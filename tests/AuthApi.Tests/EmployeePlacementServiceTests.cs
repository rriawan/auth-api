using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

public sealed class EmployeePlacementServiceTests
{
    [Fact]
    public async Task AssignDefinitive_RejectsFuturePrimaryOverlap_AndKeepsHistoryAtomic()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var p1 = await f.AddPositionAsync("HEAD-A", 1);
        var p2 = await f.AddPositionAsync("HEAD-B", 1);
        await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(p1, true, f.Today, null, "Initial appointment"),
            employee.Version, f.Actor);
        var current = f.Version(employee.Id);
        var failed = await Assert.ThrowsAsync<InvalidOperationException>(() => f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(p2, true, f.Today.AddDays(10), null, "Future overlap"), current, f.Actor));
        Assert.Contains("Primary placement", failed.Message);
        Assert.Single(await f.Db.EmployeePositions.Where(x => x.EmployeeId == employee.Id && x.SupersededAtUtc == null && x.RecordState == EmployeeRecordState.Confirmed).ToListAsync());
        Assert.Equal(1, f.Runner.RollbackCount);
    }

    [Fact]
    public async Task AssignDefinitive_AllowsMultipleEmployeesAtUncappedStaffPosition()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var p = await f.AddPositionAsync("STAFF", null);
        var first = await f.CreateEmployeeAsync("NIP-001");
        var second = await f.CreateEmployeeAsync("NIP-002");
        await f.Placements.AssignDefinitiveAsync(first.Id,
            new CreateDefinitiveAssignmentCommand(p, true, f.Today, null, "Staff one"), first.Version, f.Actor);
        await f.Placements.AssignDefinitiveAsync(second.Id,
            new CreateDefinitiveAssignmentCommand(p, true, f.Today, null, "Staff two"), f.Version(second.Id), f.Actor);
        Assert.Equal(2, await f.Db.EmployeePositions.CountAsync(x => x.PositionId == p && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null));
    }

    [Fact]
    public async Task Transfer_RebasesAllEndedActingHistoryAndPreservesOldRows()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var sourcePosition = await f.AddPositionAsync("SOURCE", null);
        var targetPosition = await f.AddPositionAsync("TARGET", null);
        var actingPosition = await f.AddPositionAsync("ACTING", null);
        var sourceResult = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(sourcePosition, true, f.Today, null, "Initial"), employee.Version, f.Actor);
        var actingResult = await f.Placements.AssignActingAsync(employee.Id,
            new CreateActingAssignmentCommand(sourceResult.RecordId!.Value, actingPosition, ActingAssignmentType.Plh,
                f.Today, f.Today.AddDays(4), "SK-001", "Acting"), sourceResult.Employee.Version, f.Actor);
        var transferDate = f.Today.AddDays(10);
        var transfer = await f.Placements.TransferAsync(employee.Id,
            new TransferEmployeeCommand(sourceResult.RecordId.Value, targetPosition, transferDate, "Transfer"),
            actingResult.Employee.Version, f.Actor);
        var oldActing = await f.Db.EmployeeActingAssignments.SingleAsync(x => x.Id == actingResult.RecordId);
        var newActing = await f.Db.EmployeeActingAssignments.SingleAsync(x => x.AssignmentKey == oldActing.AssignmentKey && x.SupersededAtUtc == null);
        Assert.NotNull(oldActing.SupersededAtUtc);
        Assert.Equal(transfer.RecordId, newActing.SourceEmployeePositionId);
        Assert.Equal(oldActing.EffectiveFrom, newActing.EffectiveFrom);
        Assert.Equal(oldActing.EffectiveToExclusive, newActing.EffectiveToExclusive);
        Assert.NotEqual(transfer.RecordId, sourceResult.RecordId);
    }

    [Fact]
    public async Task Transfer_RejectsActingThatWouldOutliveShortenedSource()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var sourcePosition = await f.AddPositionAsync("SOURCE", null);
        var targetPosition = await f.AddPositionAsync("TARGET", null);
        var actingPosition = await f.AddPositionAsync("ACTING", null);
        var source = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(sourcePosition, true, f.Today, null, "Initial"), employee.Version, f.Actor);
        var acting = await f.Placements.AssignActingAsync(employee.Id,
            new CreateActingAssignmentCommand(source.RecordId!.Value, actingPosition, ActingAssignmentType.Plt,
                f.Today.AddDays(2), null, "SK-002", "Acting"), source.Employee.Version, f.Actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Placements.TransferAsync(employee.Id,
            new TransferEmployeeCommand(source.RecordId.Value, targetPosition, f.Today.AddDays(10), "Transfer"),
            acting.Employee.Version, f.Actor));
        Assert.Single(await f.Db.EmployeePositions.Where(x => x.EmployeeId == employee.Id && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync());
        Assert.Single(await f.Db.EmployeeActingAssignments.Where(x => x.EmployeeId == employee.Id && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync());
    }

    [Fact]
    public async Task AssignmentAuditFailureRollsBackPlacementAndRootVersion()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var position = await f.AddPositionAsync("AUDIT", null);
        var beforeVersion = f.Version(employee.Id);
        f.FailAudit = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(position, true, f.Today, null, "Audit failure"), beforeVersion, f.Actor));
        Assert.Equal(beforeVersion, f.Version(employee.Id));
        Assert.Empty(await f.Db.EmployeePositions.Where(x => x.EmployeeId == employee.Id).ToListAsync());
        Assert.Equal(1, f.Runner.RollbackCount);
    }

    [Fact]
    public async Task EndDefinitive_RefusesToDropCurrentActingSource()
    {
        using var f = await EmployeeMutationFixture.CreateAsync();
        var employee = await f.CreateEmployeeAsync();
        var primary = await f.AddPositionAsync("PRIMARY", null);
        var actingPosition = await f.AddPositionAsync("ACTING", null);
        var source = await f.Placements.AssignDefinitiveAsync(employee.Id,
            new CreateDefinitiveAssignmentCommand(primary, true, f.Today, null, "Primary"), employee.Version, f.Actor);
        var acting = await f.Placements.AssignActingAsync(employee.Id,
            new CreateActingAssignmentCommand(source.RecordId!.Value, actingPosition, ActingAssignmentType.Plh,
                f.Today.AddDays(2), null, "SK-003", "Acting"), source.Employee.Version, f.Actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => f.Placements.EndDefinitiveAsync(employee.Id,
            new EndDefinitiveAssignmentCommand(source.RecordId.Value, f.Today.AddDays(4), "End source"), acting.Employee.Version, f.Actor));
    }
}
