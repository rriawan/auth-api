using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Models;
using AuthApi.Application.Features.Admin;
using AuthApi.Application.Features.Admin.DTOs;
using AuthApi.Application.Features.Auth;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security;
using AuthApi.WebApi.Controllers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthApi.Tests;

/// <summary>D-02 enforcement on login/refresh/verify/permissions (feature flag) end to end over the in-memory provider.</summary>
public sealed class EmploymentEnforcementTests : IDisposable
{
    private static readonly DateOnly Today = new(2026, 11, 6);
    private const string Password = "PasswordSiti!123";
    private readonly AppDbContext _db;
    private readonly RsaKeyProvider _keys;
    private readonly IAdminService _admin;
    private readonly TokenCryptoService _crypto = new();
    private readonly IOptions<JwtSettings> _jwt = Options.Create(new JwtSettings
        { Issuer = "https://auth.test.local", AccessTokenExpirationMinutes = 15, RefreshTokenExpirationDays = 14, KeyId = "kid" });

    public EmploymentEnforcementTests()
    {
        _db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase("p4-" + Guid.NewGuid().ToString("N")).Options);
        _keys = new RsaKeyProvider(_jwt, NullLogger<RsaKeyProvider>.Instance);
        _admin = new AdminService(_db, new PasswordHasherService(), _crypto, new AuditLogService(_db));
    }

    public void Dispose() { _db.Dispose(); _keys.Dispose(); }

    private sealed class Clock : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 11, 6, 4, 0, 0, TimeSpan.Zero);
    }

    private AuthService Auth(bool enforce)
    {
        var security = Options.Create(new SecuritySettings { MaxFailedLoginAttempts = 3, LockoutDurationMinutes = 15, EnforceEmploymentStatus = enforce });
        return new AuthService(_db, new PasswordHasherService(), _crypto, new JwtTokenService(_keys, _jwt), new AuditLogService(_db),
            new NoopDispatcher(), new ClientApiKeyValidator(_db, _crypto), _jwt, security,
            new EmploymentAccessGate(_db, new Clock(), security));
    }

    private sealed class NoopDispatcher : AuthApi.Application.Common.Interfaces.IAuthNotificationDispatcher
    {
        public Task SendAccountActivationTokenAsync(Guid userId, string email, string fullName, string rawActivationToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendPasswordResetTokenAsync(Guid userId, string email, string fullName, string rawResetToken, DateTime expiresAtUtc, CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private async Task<(Guid UserId, string ApiKey)> ProvisionAsync(string username)
    {
        var app = await _db.ClientApps.SingleOrDefaultAsync(x => x.ClientId == "erp-core");
        string rawKey = string.Empty;
        Guid appId;
        if (app is null)
        {
            var created = await _admin.CreateClientAppAsync(new CreateClientAppRequest("erp-core", "ERP", "ERP"), null, "127.0.0.1", "xUnit");
            appId = created.App.Id; rawKey = created.RawApiKey;
            var role = await _admin.CreateAppRoleAsync(appId, new CreateRoleRequest("Staff", "Staff", []), null, "127.0.0.1", "xUnit");
            _roleId = role.Id;
            _apiKey = rawKey;
        }
        else appId = app.Id;
        var user = await _admin.CreateUserAsync(new CreateUserRequest(username, username + "@example.test", Password, username, true, true), null, "127.0.0.1", "xUnit");
        await _admin.SetUserAppRolesAsync(user.Id, appId, new SetUserAppRolesRequest([_roleId]), null, "127.0.0.1", "xUnit");
        return (user.Id, _apiKey);
    }

    private Guid _roleId;
    private string _apiKey = string.Empty;

    private async Task LinkAsync(Guid userId, EmployeeStatusCode status, DateOnly from, DateOnly? activeUntil = null)
    {
        var employee = new Employee { EmployeeNumber = "N-" + userId.ToString("N")[..6], EmployeeFullName = "Pegawai" };
        _db.Employees.Add(employee);
        if (status == EmployeeStatusCode.Active)
            _db.EmployeeStatuses.Add(Status(employee.Id, EmployeeStatusCode.Active, from, activeUntil));
        else
        {
            _db.EmployeeStatuses.Add(Status(employee.Id, EmployeeStatusCode.Active, new DateOnly(2026, 1, 1), from));
            _db.EmployeeStatuses.Add(Status(employee.Id, status, from, null));
        }
        _db.UserEmployeeLinks.Add(new UserEmployeeLink { UserId = userId, EmployeeId = employee.Id, LinkedByUserId = userId });
        await _db.SaveChangesAsync();
    }

    private static EmployeeStatus Status(Guid employeeId, EmployeeStatusCode code, DateOnly from, DateOnly? to) => new()
    {
        EmployeeId = employeeId, StatusCode = code, EffectiveFrom = from, EffectiveToExclusive = to,
        RecordState = EmployeeRecordState.Confirmed, ChangeReason = "test"
    };

    [Fact]
    public async Task ResignedEmployee_CannotLogin_RefreshOrBeVerified_WhenEnforced()
    {
        var (userId, apiKey) = await ProvisionAsync("siti");
        var session = await Auth(false).LoginAsync(new LoginRequest("siti", Password, "erp-core"), "127.0.0.1", "xUnit");
        await LinkAsync(userId, EmployeeStatusCode.Resigned, Today);

        // Flag off: nothing changes (rollout is an operator decision).
        Assert.NotEmpty((await Auth(false).LoginAsync(new LoginRequest("siti", Password, "erp-core"), "127.0.0.1", "xUnit")).AccessToken);

        var auth = Auth(true);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => auth.LoginAsync(new LoginRequest("siti", Password, "erp-core"), "127.0.0.1", "xUnit"));

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => auth.RefreshTokenAsync(new RefreshTokenRequest(session.RefreshToken, "erp-core"), "127.0.0.1", "xUnit"));
        Assert.DoesNotContain(await _db.RefreshTokens.Where(x => x.UserId == userId).ToListAsync(), x => x.RevokedAtUtc == null);

        var verify = await auth.VerifyTokenRealtimeAsync("erp-core", apiKey, new VerifyTokenRequest(session.AccessToken, null), "127.0.0.1", "ERP");
        Assert.False(verify.IsValid);
        Assert.False(verify.IsAuthorized);
        Assert.Equal("Akun pengguna tidak aktif.", verify.Reason);
        Assert.DoesNotContain("pegawai", verify.Reason!, StringComparison.OrdinalIgnoreCase); // employment detail is audit-only

        var permissions = await auth.GetUserPermissionsAsync(userId, "erp-core");
        Assert.False(permissions.IsUserActive);
        Assert.Empty(permissions.Roles);
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => auth.GetCurrentUserProfileAsync(userId, "erp-core"));
    }

    [Fact]
    public async Task WithTheFlagOff_RefreshVerifyPermissionsAndProfileIgnoreEmploymentStatus()
    {
        var (userId, apiKey) = await ProvisionAsync("flagoff");
        var auth = Auth(false);
        var session = await auth.LoginAsync(new LoginRequest("flagoff", Password, "erp-core"), "127.0.0.1", "xUnit");
        await LinkAsync(userId, EmployeeStatusCode.Resigned, Today);

        var refreshed = await auth.RefreshTokenAsync(new RefreshTokenRequest(session.RefreshToken, "erp-core"), "127.0.0.1", "xUnit");
        Assert.NotEmpty(refreshed.AccessToken);
        var verify = await auth.VerifyTokenRealtimeAsync("erp-core", apiKey, new VerifyTokenRequest(refreshed.AccessToken, null), "127.0.0.1", "ERP");
        Assert.True(verify.IsValid);
        Assert.True((await auth.GetUserPermissionsAsync(userId, "erp-core")).IsUserActive);
        Assert.Equal("flagoff", (await auth.GetCurrentUserProfileAsync(userId, "erp-core")).Username);
    }

    [Fact]
    public async Task ActiveUnlinkedAndFutureResigningAccounts_AreNotAffected()
    {
        await ProvisionAsync("unlinked");
        var (activeId, _) = await ProvisionAsync("active");
        var (futureId, _) = await ProvisionAsync("future");
        await LinkAsync(activeId, EmployeeStatusCode.Active, new DateOnly(2026, 1, 1));
        await LinkAsync(futureId, EmployeeStatusCode.Resigned, Today.AddDays(30)); // resignation scheduled, not yet effective

        var auth = Auth(true);
        foreach (var name in new[] { "unlinked", "active", "future" })
            Assert.NotEmpty((await auth.LoginAsync(new LoginRequest(name, Password, "erp-core"), "127.0.0.1", "xUnit")).AccessToken);
    }

    [Fact]
    public async Task LinkedEmployeeWithoutAnEffectiveStatus_FailsClosed()
    {
        var (userId, _) = await ProvisionAsync("gap");
        var employee = new Employee { EmployeeNumber = "N-GAP", EmployeeFullName = "Gap" };
        _db.Employees.Add(employee);
        _db.EmployeeStatuses.Add(Status(employee.Id, EmployeeStatusCode.Active, Today.AddDays(5), null)); // starts in the future
        _db.UserEmployeeLinks.Add(new UserEmployeeLink { UserId = userId, EmployeeId = employee.Id });
        await _db.SaveChangesAsync();

        await Assert.ThrowsAsync<ForbiddenAccessException>(() => Auth(true).LoginAsync(new LoginRequest("gap", Password, "erp-core"), "127.0.0.1", "xUnit"));
    }

    [Fact]
    public async Task WrongPasswordStillLooksLikeAnOrdinaryFailure_NotAnEmploymentLeak()
    {
        var (userId, _) = await ProvisionAsync("leak");
        await LinkAsync(userId, EmployeeStatusCode.Resigned, Today);
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => Auth(true).LoginAsync(new LoginRequest("leak", "wrong-password", "erp-core"), "127.0.0.1", "xUnit"));
    }
}

public sealed class EmployeeLifecycleServiceTests
{
    private static async Task<(EmployeeMutationFixture F, EmployeeLifecycleService Lifecycle, User Target, EmployeeSummary Employee)> SetupAsync()
    {
        var f = await EmployeeMutationFixture.CreateAsync();
        var target = new User { Username = "target", Email = "t@example.test", FullName = "Target", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        var app = await f.Db.ClientApps.SingleAsync();
        f.Db.Users.Add(target);
        f.Db.RefreshTokens.Add(new RefreshToken { UserId = target.Id, ClientAppId = app.Id, TokenHash = "h1", ExpiresAtUtc = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        f.Db.RefreshTokens.Add(new RefreshToken { UserId = target.Id, ClientAppId = app.Id, TokenHash = "h2", ExpiresAtUtc = new DateTime(2027, 1, 1, 0, 0, 0, DateTimeKind.Utc) });
        await f.Db.SaveChangesAsync();
        var employee = await f.CreateEmployeeAsync("NIP-LC", "Pegawai LC");
        var linked = await f.Profiles.LinkUserAsync(employee.Id, target.Id, "Link", employee.Version, f.Actor);
        return (f, new EmployeeLifecycleService(f.Db, f.Runner, f.Support), target, linked.Employee);
    }

    [Fact]
    public async Task NothingHappensBeforeTheResignationTakesEffect()
    {
        var (f, lifecycle, target, employee) = await SetupAsync();
        using var _ = f;
        await f.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Resigned, f.Today.AddDays(10), "Scheduled"), employee.Version, f.Actor);

        var result = await lifecycle.ProcessResignationsAsync(false, f.Actor);

        Assert.Empty(result.Processed);
        Assert.True((await f.Db.Users.AsNoTracking().SingleAsync(x => x.Id == target.Id)).IsActive);
    }

    [Fact]
    public async Task DryRunListsCandidatesWithoutWriting_ThenRunDeactivatesRevokesAndIsIdempotent()
    {
        var (f, lifecycle, target, employee) = await SetupAsync();
        using var _ = f;
        await f.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Resigned, f.Today, "Resigned today"), employee.Version, f.Actor);

        var dry = await lifecycle.ProcessResignationsAsync(true, f.Actor);
        Assert.Equal(target.Id, Assert.Single(dry.Processed).UserId);
        Assert.Equal(0, dry.RevokedSessions);
        Assert.True((await f.Db.Users.AsNoTracking().SingleAsync(x => x.Id == target.Id)).IsActive);

        var run = await lifecycle.ProcessResignationsAsync(false, f.Actor);
        Assert.Equal(target.Id, Assert.Single(run.Processed).UserId);
        Assert.Equal(2, run.RevokedSessions);
        Assert.False((await f.Db.Users.AsNoTracking().SingleAsync(x => x.Id == target.Id)).IsActive);
        Assert.All(await f.Db.RefreshTokens.AsNoTracking().Where(x => x.UserId == target.Id).ToListAsync(), x => Assert.NotNull(x.RevokedAtUtc));
        Assert.Single(await f.Db.AuditLogs.AsNoTracking().Where(x => x.EventType == AuditEventType.EmployeeAccountDeactivated).ToListAsync());

        var again = await lifecycle.ProcessResignationsAsync(false, f.Actor);
        Assert.Empty(again.Processed);
        Assert.Single(await f.Db.AuditLogs.AsNoTracking().Where(x => x.EventType == AuditEventType.EmployeeAccountDeactivated).ToListAsync());
    }

    [Fact]
    public async Task ReactivatingTheEmployeeNeverReactivatesTheAccountOrTheSessions()
    {
        var (f, lifecycle, target, employee) = await SetupAsync();
        using var _ = f;
        await f.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Resigned, f.Today, "Resigned today"), employee.Version, f.Actor);
        await lifecycle.ProcessResignationsAsync(false, f.Actor);

        await f.Profiles.ChangeStatusAsync(employee.Id,
            new ChangeEmployeeStatusCommand(EmployeeStatusCode.Active, f.Today.AddDays(1), "Rehired"), f.Version(employee.Id), f.Actor);

        Assert.False((await f.Db.Users.AsNoTracking().SingleAsync(x => x.Id == target.Id)).IsActive);
        Assert.All(await f.Db.RefreshTokens.AsNoTracking().Where(x => x.UserId == target.Id).ToListAsync(), x => Assert.NotNull(x.RevokedAtUtc));

    }

    [Fact]
    public async Task OnlyAFreshSuperAdminMayRunIt_AndTheEndpointDefaultsToDryRun()
    {
        var (f, lifecycle, _, _) = await SetupAsync();
        using var _ = f;
        var plain = new User { Username = "plain", Email = "p@example.test", FullName = "P", PasswordHash = "x", IsActive = true, IsAccountVerified = true };
        f.Db.Users.Add(plain);
        await f.Db.SaveChangesAsync();
        await Assert.ThrowsAsync<ForbiddenAccessException>(() => lifecycle.ProcessResignationsAsync(true, new EmployeeActor(plain.Id)));

        var method = typeof(EmployeesAdminController).GetMethod(nameof(EmployeesAdminController.ProcessResignations))!;
        Assert.True((bool)method.GetParameters().Single(p => p.Name == "dryRun").DefaultValue!);
    }
}
