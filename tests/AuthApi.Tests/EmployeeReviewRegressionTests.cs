using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Domain.Enums.Organization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

/// <summary>Regression tests for the independent P2B security/correctness review (2026-11-06).</summary>
public sealed class EmployeeReviewRegressionTests
{
    [Fact]
    public async Task Transfer_OnSourceStartDate_CancelsSourceInsteadOfDuplicatingOrRejecting()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var sourcePosition = await f.AddPositionAsync("SOURCE", null);
        var targetPosition = await f.AddPositionAsync("TARGET", null);
        var future = f.Today.AddDays(5);
        var source = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(sourcePosition, true, future, null, "Future appointment"), employee.Version, f.Actor);

        var transfer = await f.Placements.TransferAsync(employee.Id,
            new TransferEmployeeCommand(source.RecordId!.Value, targetPosition, future, "Transfer on start date"),
            source.Employee.Version, f.Actor);

        var current = await f.Db.EmployeePositions.Where(x => x.EmployeeId == employee.Id && x.SupersededAtUtc == null).ToListAsync();
        var confirmed = Assert.Single(current, x => x.RecordState == EmployeeRecordState.Confirmed);
        Assert.Equal(targetPosition, confirmed.PositionId);
        Assert.Equal(transfer.SecondaryRecordId, confirmed.Id);
        var cancelled = Assert.Single(current, x => x.RecordState == EmployeeRecordState.Cancelled);
        Assert.Equal(sourcePosition, cancelled.PositionId);
    }

    [Fact]
    public async Task Transfer_OnSourceStartDate_ForNonPrimaryPlacementDoesNotLeaveBothActive()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var primary = await f.AddPositionAsync("PRIMARY", null);
        var extra = await f.AddPositionAsync("EXTRA", null);
        var target = await f.AddPositionAsync("TARGET", null);
        var first = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(primary, true, f.Today, null, "Primary"), employee.Version, f.Actor);
        var future = f.Today.AddDays(3);
        var second = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(extra, false, future, null, "Additional"), first.Employee.Version, f.Actor);

        await f.Placements.TransferAsync(employee.Id,
            new TransferEmployeeCommand(second.RecordId!.Value, target, future, "Move additional"),
            second.Employee.Version, f.Actor);

        var confirmed = await f.Db.EmployeePositions.Where(x => x.EmployeeId == employee.Id && x.SupersededAtUtc == null
            && x.RecordState == EmployeeRecordState.Confirmed).ToListAsync();
        Assert.DoesNotContain(confirmed, x => x.PositionId == extra);
        Assert.Contains(confirmed, x => x.PositionId == target);
    }

    [Fact]
    public async Task ReportingLineValidation_IgnoresNonCoexistingReportToVersions()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var p = await f.AddPositionAsync("P", null);
        var b = await f.AddPositionAsync("B", null);
        var x = await f.AddPositionAsync("X", null);
        var y = await f.AddPositionAsync("Y", null);
        var split = f.Today.AddDays(30);
        await SetReportsToAsync(f, p, b);
        // B reports to X first and to Y later; the two lines never coexist.
        var bVersion = await f.Db.PositionVersions.SingleAsync(v => v.PositionId == b);
        bVersion.ReportsToPositionId = x;
        bVersion.EffectiveToExclusive = split;
        f.Db.PositionVersions.Add(CopyVersion(bVersion, split, null, y));
        await f.Db.SaveChangesAsync();

        var result = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(p, true, f.Today, null, "Placement under re-organised line"),
            employee.Version, f.Actor);

        Assert.NotNull(result.RecordId);
    }

    [Fact]
    public async Task ReportingLineValidation_StillRejectsCycleOverTheSamePeriod()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var p = await f.AddPositionAsync("P", null);
        var b = await f.AddPositionAsync("B", null);
        await SetReportsToAsync(f, p, b);
        await SetReportsToAsync(f, b, p);

        var failure = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(p, true, f.Today, null, "Cyclic line"), employee.Version, f.Actor));

        Assert.Contains("cycle", failure.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task EndDefinitive_AndEndActing_PreserveHistoryWithSingleCurrentRevision()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var primary = await f.AddPositionAsync("PRIMARY", null);
        var actingPosition = await f.AddPositionAsync("ACTING", null);
        var source = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(primary, true, f.Today, null, "Primary"), employee.Version, f.Actor);
        var acting = await f.Placements.AssignActingAsync(employee.Id,
            new CreateActingAssignmentCommand(source.RecordId!.Value, actingPosition, ActingAssignmentType.Plh,
                f.Today, null, "SK-010", "Acting"), source.Employee.Version, f.Actor);

        var endedActing = await f.Placements.EndActingAsync(employee.Id,
            new EndActingAssignmentCommand(acting.RecordId!.Value, f.Today.AddDays(5), "Acting finished"),
            acting.Employee.Version, f.Actor);
        var endedSource = await f.Placements.EndDefinitiveAsync(employee.Id,
            new EndDefinitiveAssignmentCommand(source.RecordId.Value, f.Today.AddDays(10), "Placement finished"),
            endedActing.Employee.Version, f.Actor);

        var actingRows = await f.Db.EmployeeActingAssignments.Where(x => x.EmployeeId == employee.Id).ToListAsync();
        Assert.Single(actingRows, x => x.SupersededAtUtc == null);
        Assert.Contains(actingRows, x => x.Id == acting.RecordId && x.SupersededAtUtc != null);
        var placementRows = await f.Db.EmployeePositions.Where(x => x.EmployeeId == employee.Id).ToListAsync();
        var currentPlacement = Assert.Single(placementRows, x => x.SupersededAtUtc == null);
        Assert.Equal(f.Today.AddDays(10), currentPlacement.EffectiveToExclusive);
        Assert.Equal(endedSource.RecordId, currentPlacement.Id);
    }

    [Fact]
    public async Task ChangeStatus_AllowsManualReactivationAfterResignation()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var resign = f.Today.AddDays(10);
        var resigned = await f.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Resigned, resign, "Resignation"), employee.Version, f.Actor);

        var reactivated = await f.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Active, resign.AddDays(1), "Manual reactivation", "SK-REACT"),
            resigned.Employee.Version, f.Actor);

        var current = await f.Db.EmployeeStatuses.Where(s => s.EmployeeId == employee.Id && s.SupersededAtUtc == null).OrderBy(s => s.EffectiveFrom).ToListAsync();
        Assert.Equal([EmployeeStatusCode.Active, EmployeeStatusCode.Resigned, EmployeeStatusCode.Active], current.Select(s => s.StatusCode));
        Assert.Null(current[^1].EffectiveToExclusive);
        Assert.Equal(EmployeeStatusCode.Active, reactivated.Employee.CurrentStatus);
    }

    private static async Task SetReportsToAsync(EmployeeMutationFixture f, Guid positionId, Guid reportsTo)
    {
        var version = await f.Db.PositionVersions.SingleAsync(v => v.PositionId == positionId);
        version.ReportsToPositionId = reportsTo;
        await f.Db.SaveChangesAsync();
    }

    private static PositionVersion CopyVersion(PositionVersion source, DateOnly from, DateOnly? to, Guid? reportsTo) => new()
    {
        PositionId = source.PositionId, Name = source.Name, Track = source.Track, IsUnitHead = source.IsUnitHead,
        MaxIncumbents = source.MaxIncumbents, EffectiveFrom = from, EffectiveToExclusive = to, ReportsToPositionId = reportsTo,
        ChangeReason = "Re-organisation", CreatedByUserId = source.CreatedByUserId
    };
}

public sealed class AuditDetailTruncationTests
{
    [Fact]
    public async Task OversizedStructuredDetail_IsStoredAsValidBoundedJson()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var audit = new AuthApi.Infrastructure.Security.AuditLogService(f.Db);
        await audit.LogAsync(AuthApi.Domain.Enums.AuditEventType.EmployeeCreated, true,
            detail: new { reason = new string('x', 6000), note = "quote\" and \\ backslash" });

        var stored = await f.Db.AuditLogs.AsNoTracking().OrderByDescending(x => x.TimestampUtc).FirstAsync();
        Assert.NotNull(stored.DetailJson);
        Assert.True(stored.DetailJson!.Length <= 4000);
        using var document = System.Text.Json.JsonDocument.Parse(stored.DetailJson);
        Assert.True(document.RootElement.GetProperty("truncated").GetBoolean());
        Assert.True(document.RootElement.GetProperty("originalLength").GetInt32() > 6000);
    }
}
