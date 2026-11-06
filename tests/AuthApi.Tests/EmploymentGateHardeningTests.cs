using AuthApi.Application;
using AuthApi.Application.Common.Models;
using AuthApi.Application.Features.Auth;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthApi.Tests;

/// <summary>Regression tests for the independent review of the account lifecycle (P4).</summary>
public sealed class EmploymentGateHardeningTests
{
    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    [Fact]
    public void ProductionContainer_RegistersTheGate_AndAuthServiceRequiresIt()
    {
        var services = new ServiceCollection();
        services.AddApplication();
        Assert.Contains(services, d => d.ServiceType == typeof(IEmploymentAccessGate) && d.ImplementationType == typeof(EmploymentAccessGate));
        // Required (non-optional) constructor parameter: a missing registration fails loudly instead of silently disabling enforcement.
        var parameter = typeof(AuthService).GetConstructors().Single().GetParameters().Single(p => p.ParameterType == typeof(IEmploymentAccessGate));
        Assert.False(parameter.HasDefaultValue);
    }

    private static async Task<(AppDbContext Db, Guid UserId)> LinkedResigningOnAsync(DateOnly resignFrom)
    {
        var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("gate-" + Guid.NewGuid().ToString("N")).Options);
        var user = new User { Username = "u", Email = "u@example.test", FullName = "U", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        var employee = new Employee { EmployeeNumber = "N-1", EmployeeFullName = "E" };
        db.Users.Add(user);
        db.Employees.Add(employee);
        db.EmployeeStatuses.Add(new EmployeeStatus { EmployeeId = employee.Id, StatusCode = EmployeeStatusCode.Active, EffectiveFrom = new DateOnly(2026, 1, 1),
            EffectiveToExclusive = resignFrom, RecordState = EmployeeRecordState.Confirmed, ChangeReason = "t" });
        db.EmployeeStatuses.Add(new EmployeeStatus { EmployeeId = employee.Id, StatusCode = EmployeeStatusCode.Resigned, EffectiveFrom = resignFrom,
            RecordState = EmployeeRecordState.Confirmed, ChangeReason = "t" });
        db.UserEmployeeLinks.Add(new UserEmployeeLink { UserId = user.Id, EmployeeId = employee.Id });
        await db.SaveChangesAsync();
        return (db, user.Id);
    }

    private static EmploymentAccessGate Gate(AppDbContext db, DateTimeOffset now, bool enforce = true) =>
        new(db, new FixedClock(now), Options.Create(new SecuritySettings { EnforceEmploymentStatus = enforce }));

    [Fact]
    public async Task GateUsesTheJakartaDate_NotTheUtcDate()
    {
        var (db, userId) = await LinkedResigningOnAsync(new DateOnly(2026, 11, 6));
        using var _ = db;

        // 17:30Z on 11-05 is already 00:30 on 11-06 in Jakarta: the resignation is effective.
        Assert.False((await Gate(db, new DateTimeOffset(2026, 11, 5, 17, 30, 0, TimeSpan.Zero)).EvaluateAsync(userId)).IsAllowed);
        // 16:30Z on 11-05 is 23:30 on 11-05 in Jakarta: still the last working day.
        Assert.True((await Gate(db, new DateTimeOffset(2026, 11, 5, 16, 30, 0, TimeSpan.Zero)).EvaluateAsync(userId)).IsAllowed);
    }

    [Fact]
    public async Task GateIsInertWhenTheFlagIsOff()
    {
        var (db, userId) = await LinkedResigningOnAsync(new DateOnly(2026, 11, 6));
        using var _ = db;
        Assert.True((await Gate(db, new DateTimeOffset(2026, 11, 6, 4, 0, 0, TimeSpan.Zero), enforce: false).EvaluateAsync(userId)).IsAllowed);
    }
}
