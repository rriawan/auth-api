using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

public sealed class EmployeeProfileServiceTests
{
    [Fact]
    public async Task CreateEmployee_CreatesSeparateEmployeeAndDoesNotCreateOrChangeUser()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var accountName = fixture.Db.Users.Single(x => x.Id == fixture.Actor.UserId).FullName;

        var created = await fixture.Profiles.CreateAsync(
            new CreateEmployeeCommand("  000123  ", "  Official Employee  ", fixture.Today, "Created from HR"), fixture.Actor);

        var employee = await fixture.Db.Employees.SingleAsync(x => x.Id == created.Employee.Id);
        Assert.Equal("000123", employee.EmployeeNumber);
        Assert.Equal("Official Employee", employee.EmployeeFullName);
        Assert.Equal(accountName, fixture.Db.Users.Single(x => x.Id == fixture.Actor.UserId).FullName);
        Assert.Empty(await fixture.Db.UserEmployeeLinks.ToListAsync());
        Assert.Equal(EmployeeStatusCode.Active, created.Employee.CurrentStatus);
        Assert.Equal(1, fixture.Runner.CommitCount);
    }

    [Fact]
    public async Task CreateEmployee_RequiresFreshInternalSuperAdminMembership()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var outsider = new EmployeeActor(Guid.NewGuid());
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.Profiles.CreateAsync(
            new CreateEmployeeCommand("NIP-X", "Employee X", fixture.Today, "Reason"), outsider));
        Assert.Empty(await fixture.Db.Employees.ToListAsync());

        var nonAdmin = new User { Username = "ordinary", Email = "ordinary@example.test", FullName = "Ordinary", PasswordHash = "unused", IsAccountVerified = true };
        fixture.Db.Users.Add(nonAdmin);
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => fixture.Profiles.CreateAsync(
            new CreateEmployeeCommand("NIP-Y", "Employee Y", fixture.Today, "Reason"), new EmployeeActor(nonAdmin.Id)));
        Assert.Empty(await fixture.Db.Employees.ToListAsync());
    }

    [Fact]
    public async Task ExistingEmployeeWrites_RequireExactVersionAndRotateItEvenAtSameClockInstant()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var employee = await fixture.CreateEmployeeAsync();
        await Assert.ThrowsAsync<EmployeePreconditionException>(() => fixture.Profiles.UpdateIdentityAsync(
            employee.Id, new UpdateEmployeeIdentityCommand("NIP-001", "New Name", "Reason"), null, fixture.Actor));
        await Assert.ThrowsAsync<EmployeePreconditionException>(() => fixture.Profiles.UpdateIdentityAsync(
            employee.Id, new UpdateEmployeeIdentityCommand("NIP-001", "New Name", "Reason"), "\"AAAAAAAAAAA=\"", fixture.Actor));

        var first = fixture.Version(employee.Id);
        var updated = await fixture.Profiles.UpdateIdentityAsync(employee.Id,
            new UpdateEmployeeIdentityCommand(" NIP-001 ", " First Update ", "Reason one"), first, fixture.Actor);
        var second = updated.Employee.Version;
        Assert.NotEqual(first, second);
        var updatedAgain = await fixture.Profiles.UpdateIdentityAsync(employee.Id,
            new UpdateEmployeeIdentityCommand("NIP-001", "Second Update", "Reason two"), second, fixture.Actor);
        Assert.NotEqual(second, updatedAgain.Employee.Version);
        Assert.Equal("Second Update", updatedAgain.Employee.EmployeeFullName);
    }

    [Fact]
    public async Task LinkAndUnlink_AreExplicitOneToOneAndLeaveAccountAndRolesUntouched()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var employee = await fixture.CreateEmployeeAsync();
        var before = await fixture.Db.Users.AsNoTracking().SingleAsync(x => x.Id == fixture.Actor.UserId);
        var rolesBefore = await fixture.Db.UserAppRoles.AsNoTracking().ToListAsync();
        var linked = await fixture.Profiles.LinkUserAsync(employee.Id, fixture.Actor.UserId, "Verified identity", fixture.Version(employee.Id), fixture.Actor);

        Assert.Equal(fixture.Actor.UserId, linked.Employee.LinkedUserId);
        var afterLink = await fixture.Db.Users.AsNoTracking().SingleAsync(x => x.Id == fixture.Actor.UserId);
        Assert.Equal(before.FullName, afterLink.FullName);
        Assert.Equal(before.IsActive, afterLink.IsActive);
        Assert.Equal(before.IsAccountVerified, afterLink.IsAccountVerified);
        Assert.Equal(rolesBefore.Select(x => (x.UserId, x.ClientAppId, x.RoleId)),
            (await fixture.Db.UserAppRoles.AsNoTracking().ToListAsync()).Select(x => (x.UserId, x.ClientAppId, x.RoleId)));

        var unlinked = await fixture.Profiles.UnlinkUserAsync(employee.Id, fixture.Actor.UserId, "Correction", linked.Employee.Version, fixture.Actor);
        Assert.Null(unlinked.Employee.LinkedUserId);
        Assert.Equal(1, await fixture.Db.Users.CountAsync());
        Assert.Equal(rolesBefore.Count, await fixture.Db.UserAppRoles.CountAsync());
    }

    [Fact]
    public async Task LinkRejectsRelinkingAnAlreadyLinkedUserAndAnEmployeeWithAnotherUser()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var first = await fixture.CreateEmployeeAsync();
        await fixture.Profiles.LinkUserAsync(first.Id, fixture.Actor.UserId, "Link", fixture.Version(first.Id), fixture.Actor);
        var second = await fixture.CreateEmployeeAsync("NIP-002", "Other employee");
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Profiles.LinkUserAsync(
            second.Id, fixture.Actor.UserId, "Implicit relink", fixture.Version(second.Id), fixture.Actor));

        var another = new User { Username = "another", Email = "another@example.test", FullName = "Another", PasswordHash = "unused", IsAccountVerified = true };
        fixture.Db.Users.Add(another);
        await fixture.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Profiles.LinkUserAsync(
            first.Id, another.Id, "Replace link", fixture.Version(first.Id), fixture.Actor));
        Assert.Equal(fixture.Actor.UserId, (await fixture.Db.UserEmployeeLinks.SingleAsync(x => x.EmployeeId == first.Id)).UserId);
    }

    [Fact]
    public async Task ScheduledResignationBlocksUnlinkAndRehireNeverReactivatesAccount()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var employee = await fixture.CreateEmployeeAsync();
        var targetAccount = new User { Username = "employee-account", Email = "employee@example.test", FullName = "Account Name", PasswordHash = "unused", IsActive = false, IsAccountVerified = true };
        fixture.Db.Users.Add(targetAccount);
        await fixture.Db.SaveChangesAsync();
        await fixture.Profiles.LinkUserAsync(employee.Id, targetAccount.Id, "Link", fixture.Version(employee.Id), fixture.Actor);
        var resignationDate = fixture.Today.AddDays(10);
        var resigned = await fixture.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Resigned, resignationDate, "Resignation", "HR-42"),
            fixture.Version(employee.Id), fixture.Actor);
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Profiles.UnlinkUserAsync(
            employee.Id, targetAccount.Id, "Try unlink", resigned.Employee.Version, fixture.Actor));
        var rehireDate = resignationDate.AddDays(1);
        await fixture.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Active, rehireDate, "Manual rehire", "HR-43"),
            fixture.Version(employee.Id), fixture.Actor);
        Assert.False((await fixture.Db.Users.AsNoTracking().SingleAsync(x => x.Id == targetAccount.Id)).IsActive);
        Assert.Single(await fixture.Db.UserAppRoles.ToListAsync());
    }

    [Fact]
    public async Task StatusRevisionPreservesPreviousHistoryAndAuditFailureRollsBack()
    {
        using var fixture = await EmployeeMutationFixture.CreateAsync();
        var employee = await fixture.CreateEmployeeAsync();
        var statusStart = fixture.Today.AddDays(7);
        await fixture.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Resigned, statusStart, "Scheduled resignation"),
            fixture.Version(employee.Id), fixture.Actor);
        var rows = await fixture.Db.EmployeeStatuses.AsNoTracking().Where(x => x.EmployeeId == employee.Id).ToListAsync();
        Assert.Equal(3, rows.Count);
        Assert.Single(rows, x => x.SupersededAtUtc is null && x.StatusCode == EmployeeStatusCode.Active && x.EffectiveFrom == fixture.Today && x.EffectiveToExclusive == statusStart);
        Assert.Single(rows, x => x.SupersededAtUtc is null && x.StatusCode == EmployeeStatusCode.Resigned && x.EffectiveFrom == statusStart && x.EffectiveToExclusive is null);

        var currentVersion = fixture.Version(employee.Id);
        fixture.FailAudit = true;
        await Assert.ThrowsAsync<InvalidOperationException>(() => fixture.Profiles.UpdateIdentityAsync(employee.Id,
            new UpdateEmployeeIdentityCommand("NIP-CHANGED", "Changed", "Audit must be atomic"), currentVersion, fixture.Actor));
        Assert.Equal("NIP-001", (await fixture.Db.Employees.AsNoTracking().SingleAsync(x => x.Id == employee.Id)).EmployeeNumber);
        Assert.Equal(1, fixture.Runner.RollbackCount);
    }
}
