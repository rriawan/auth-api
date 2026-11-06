using System.Text.Json;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Application.Features.Organization;
using AuthApi.Domain.Constants;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Domain.Enums.Organization;
using AuthApi.WebApi.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

public sealed class SelfOrganizationProfileTests
{
    internal const string ClientId = SystemConstants.DefaultAdminClientId; // the fixture account holds a role on this app

    internal sealed class Scenario : IDisposable
    {
        public required EmployeeMutationFixture F { get; init; }
        public required OrganizationAdminService Org { get; init; }
        public required SelfOrganizationProfileService Self { get; init; }
        public Guid OfficeId, UnitId, HeadPositionId, StaffPositionId;
        public EmployeeSummary Employee = null!;
        public Guid StaffAssignmentRow;

        public Guid UserId => F.Actor.UserId;
        public void Dispose() => F.Dispose();

        /// <summary>Office "901" with a head position (vacant) and a staff position reporting to it; the account's employee holds staff.</summary>
        public static async Task<Scenario> CreateAsync(bool link = true)
        {
            var f = await EmployeeMutationFixture.CreateAsync();
            f.Db.OfficeClassifications.Add(new OfficeClassification { Code = "CAB-I", DisplayName = "Cabang I" });
            await f.Db.SaveChangesAsync();
            var s = new Scenario
            {
                F = f,
                Org = new OrganizationAdminService(f.Db, f.Runner, f.Support, f.Placements),
                Self = new SelfOrganizationProfileService(f.Db, new FixedClock())
            };
            var office = await s.Org.CreateOfficeAsync(new CreateOfficeCommand("901", "Kantor Uji", "CAB-I", f.Today, "Create"), f.Actor);
            var unit = await s.Org.CreateUnitAsync(new CreateUnitCommand(office.Id, "ROOT", null, OrgUnitType.Section, "Seksi Uji", f.Today, "Create"), f.Actor);
            var head = await s.Org.CreatePositionAsync(new CreatePositionCommand(unit.Id, "KEPALA", "Kepala Seksi", PositionTrack.Line, true, 1,
                null, null, f.Today, "Create"), f.Actor);
            var staff = await s.Org.CreatePositionAsync(new CreatePositionCommand(unit.Id, "STAFF", "Pelaksana", PositionTrack.Line, false, null,
                null, head.Id, f.Today, "Create"), f.Actor);
            s.OfficeId = office.Id; s.UnitId = unit.Id; s.HeadPositionId = head.Id; s.StaffPositionId = staff.Id;

            var employee = await f.CreateEmployeeAsync("NIP-900", "Pegawai Uji");
            var placed = await f.Placements.AssignDefinitiveAsync(employee.Id,
                new CreateDefinitiveAssignmentCommand(staff.Id, true, f.Today, null, "Place"), employee.Version, f.Actor);
            s.StaffAssignmentRow = placed.RecordId!.Value;
            s.Employee = placed.Employee;
            if (link)
                s.Employee = (await f.Profiles.LinkUserAsync(employee.Id, f.Actor.UserId, "Link", placed.Employee.Version, f.Actor)).Employee;
            return s;
        }

        private sealed class FixedClock : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => new(2026, 11, 6, 4, 0, 0, TimeSpan.Zero);
        }
    }

    [Fact]
    public async Task UnlinkedAccount_GetsEmptyProfileNotAnError()
    {
        using var s = await Scenario.CreateAsync(link: false);
        var profile = await s.Self.GetAsync(s.UserId, ClientId);

        Assert.Null(profile.Employee);
        Assert.Empty(profile.Assignments);
        Assert.Null(profile.PrimaryAssignmentId);
        Assert.Equal("Asia/Jakarta", profile.BusinessTimeZone);
        Assert.Equal(s.F.Today, profile.AsOf);
    }

    [Fact]
    public async Task LinkedEmployee_SeesOwnAssignmentWithSupervisorPositionAndVacancyButNoPeopleOrPermissions()
    {
        using var s = await Scenario.CreateAsync();
        var profile = await s.Self.GetAsync(s.UserId, ClientId);

        Assert.Equal("NIP-900", profile.Employee!.EmployeeNumber);
        Assert.Equal("ACTIVE", profile.Employee.Status);
        var assignment = Assert.Single(profile.Assignments);
        Assert.Equal("DEFINITIVE", assignment.Type);
        Assert.True(assignment.IsPrimary);
        Assert.Equal(profile.PrimaryAssignmentId, assignment.Id);
        Assert.Equal("901", assignment.Office.Code);
        Assert.Equal("CAB-I", assignment.Office.Classification);
        Assert.Equal("Seksi Uji", assignment.OrgUnit.Name);
        Assert.Equal("LINE", assignment.Position.Track);
        Assert.Equal(s.HeadPositionId, assignment.FormalSupervisor!.PositionId);
        Assert.Equal("Kepala Seksi", assignment.FormalSupervisor.PositionName);
        Assert.True(assignment.FormalSupervisor.IsVacant);

        var json = JsonSerializer.Serialize(profile).ToLowerInvariant();
        Assert.DoesNotContain("permission", json);
        Assert.DoesNotContain("approval", json);
    }

    [Fact]
    public async Task AssignmentId_IsStableAcrossRevisions_AndProfileVersionOnlyChangesWithContent()
    {
        using var s = await Scenario.CreateAsync();
        var before = await s.Self.GetAsync(s.UserId, ClientId);
        var again = await s.Self.GetAsync(s.UserId, ClientId);
        Assert.Equal(before.ProfileVersion, again.ProfileVersion);

        var target = (await s.Org.CreatePositionAsync(new CreatePositionCommand(s.UnitId, "STAFF2", "Pelaksana Dua", PositionTrack.Line, false,
            null, null, s.HeadPositionId, s.F.Today, "Create"), s.F.Actor)).Id;
        await s.F.Placements.TransferAsync(s.Employee.Id,
            new TransferEmployeeCommand(s.StaffAssignmentRow, target, s.F.Today, "Move"), s.F.Version(s.Employee.Id), s.F.Actor);

        var after = await s.Self.GetAsync(s.UserId, ClientId);
        Assert.NotEqual(before.ProfileVersion, after.ProfileVersion);
        Assert.Equal("Pelaksana Dua", Assert.Single(after.Assignments).Position.Name);
    }

    [Fact]
    public async Task ActingAssignment_IsListedWithTypeAndSource()
    {
        using var s = await Scenario.CreateAsync();
        var acting = await s.F.Placements.AssignActingAsync(s.Employee.Id,
            new CreateActingAssignmentCommand(s.StaffAssignmentRow, s.HeadPositionId, ActingAssignmentType.Plh,
                s.F.Today, s.F.Today.AddDays(10), "SK-1", "Acting"), s.F.Version(s.Employee.Id), s.F.Actor);

        var profile = await s.Self.GetAsync(s.UserId, ClientId);

        Assert.Equal(2, profile.Assignments.Count);
        var plh = Assert.Single(profile.Assignments, x => x.Type == "PLH");
        Assert.False(plh.IsPrimary);
        Assert.Equal(profile.PrimaryAssignmentId, plh.SourceAssignmentId);
        Assert.Equal(s.HeadPositionId, plh.Position.Id);
        Assert.NotNull(profile.NextEffectiveChangeAtUtc);
        Assert.Equal(new DateTime(2026, 11, 15, 17, 0, 0, DateTimeKind.Utc), profile.NextEffectiveChangeAtUtc); // 2026-11-16 00:00 WIB
        _ = acting;
    }

    [Fact]
    public async Task ResignedEmployee_IsRejectedEvenWithAValidAccount()
    {
        using var s = await Scenario.CreateAsync();
        // Resign effective today: today is the first inactive day (D-03). Switch the actor so the admin check is not self-blocked.
        s.F.Db.Users.Add(new User { Username = "other-admin", Email = "o@example.test", FullName = "O", PasswordHash = "x", IsActive = true, IsAccountVerified = true });
        await s.F.Db.SaveChangesAsync();
        var employee = await s.F.Db.Employees.SingleAsync(x => x.Id == s.Employee.Id);
        s.F.Db.EmployeeStatuses.RemoveRange(s.F.Db.EmployeeStatuses.Where(x => x.EmployeeId == employee.Id));
        s.F.Db.EmployeeStatuses.AddRange(
            new EmployeeStatus { EmployeeId = employee.Id, StatusCode = EmployeeStatusCode.Resigned, EffectiveFrom = s.F.Today,
                RecordState = EmployeeRecordState.Confirmed, ChangeReason = "Resigned" });
        await s.F.Db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, ClientId));
    }

    [Fact]
    public async Task InactiveLockedOrUnknownAccountAndWrongApplication_AreRejected()
    {
        using var s = await Scenario.CreateAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(Guid.NewGuid(), ClientId));
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, "no-such-app"));

        var app = new ClientApp { ClientId = "erp", Name = "ERP", ApiKeyHash = new string('1', 64), ApiKeyPrefix = "erp", IsActive = true };
        s.F.Db.ClientApps.Add(app);
        await s.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, "erp")); // no role on that app

        app.IsActive = false;
        var user = await s.F.Db.Users.SingleAsync(x => x.Id == s.UserId);
        await s.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, "erp"));

        user.IsActive = false;
        await s.F.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => s.Self.GetAsync(s.UserId, ClientId));
    }

    [Fact]
    public async Task ReportingLine_ListsPositionsAndVacancyOnly_ForOwnAssignmentsOnly()
    {
        using var s = await Scenario.CreateAsync();
        var profile = await s.Self.GetAsync(s.UserId, ClientId);

        var line = await s.Self.GetReportingLineAsync(s.UserId, ClientId, profile.PrimaryAssignmentId!.Value);
        var node = Assert.Single(line.Positions);
        Assert.Equal(1, node.Depth);
        Assert.Equal(s.HeadPositionId, node.PositionId);
        Assert.True(node.IsVacant);
        Assert.Equal("901", node.OfficeCode);

        await Assert.ThrowsAsync<KeyNotFoundException>(() => s.Self.GetReportingLineAsync(s.UserId, ClientId, Guid.NewGuid()));
        await Assert.ThrowsAsync<KeyNotFoundException>(() => s.Self.GetReportingLineAsync(s.UserId, ClientId, s.StaffAssignmentRow)); // revision row id is not the public id
    }

    [Fact]
    public void Controller_RequiresAuthenticationAndIsRateLimited()
    {
        var type = typeof(SelfOrganizationController);
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(AuthorizeAttribute)));
        Assert.NotNull(Attribute.GetCustomAttribute(type, typeof(Microsoft.AspNetCore.RateLimiting.EnableRateLimitingAttribute)));
        Assert.DoesNotContain(type.GetMethods(), m => m.GetParameters().Any(p => p.Name is "userId" or "asOf"));
    }
}
