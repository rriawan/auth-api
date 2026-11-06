using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Application.Features.Admin;
using AuthApi.Application.Features.Admin.DTOs;
using AuthApi.Application.Features.Auth;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Infrastructure.Persistence;
using AuthApi.Infrastructure.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthApi.Tests;

public class AuthAndAdminLifecycleTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly RsaKeyProvider _rsaKeyProvider;
    private readonly TestNotificationDispatcher _notificationDispatcher;
    private readonly IAuthService _authService;
    private readonly IAdminService _adminService;

    public AuthAndAdminLifecycleTests()
    {
        var dbOptions = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"AuthApiTestDb_{Guid.NewGuid():N}")
            .Options;

        _dbContext = new AppDbContext(dbOptions);

        var jwtOptions = Options.Create(new JwtSettings
        {
            Issuer = "https://auth.test.local",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 14,
            KeyId = "test-kid"
        });

        var securityOptions = Options.Create(new SecuritySettings
        {
            MaxFailedLoginAttempts = 3,
            LockoutDurationMinutes = 15,
            ActionTokenExpirationMinutes = 30
        });

        var passwordHasher = new PasswordHasherService();
        var tokenCrypto = new TokenCryptoService();
        _rsaKeyProvider = new RsaKeyProvider(jwtOptions, NullLogger<RsaKeyProvider>.Instance);
        var jwtService = new JwtTokenService(_rsaKeyProvider, jwtOptions);
        var auditLogService = new AuditLogService(_dbContext);
        _notificationDispatcher = new TestNotificationDispatcher();
        var clientApiKeyValidator = new ClientApiKeyValidator(_dbContext, tokenCrypto);

        _adminService = new AdminService(_dbContext, passwordHasher, tokenCrypto, auditLogService);
        _authService = new AuthService(
            _dbContext,
            passwordHasher,
            tokenCrypto,
            jwtService,
            auditLogService,
            _notificationDispatcher,
            clientApiKeyValidator,
            jwtOptions,
            securityOptions,
            new AuthApi.Application.Features.Employees.EmploymentAccessGate(_dbContext, TimeProvider.System, securityOptions));
    }

    [Fact]
    public async Task FullLifecycle_ProvisionClientApp_Roles_Permissions_User_Login_AndRealtimeVerify()
    {
        // 1. Daftarkan aplikasi baru 'erp-core'
        var createdApp = await _adminService.CreateClientAppAsync(
            new CreateClientAppRequest("erp-core", "ERP Core System", "Sistem ERP Perusahaan"),
            actorUserId: null,
            ipAddress: "127.0.0.1",
            userAgent: "xUnit");

        var appId = createdApp.App.Id;
        var rawApiKey = createdApp.RawApiKey;

        // 2. Buat permissions untuk 'erp-core'
        var permView = await _adminService.CreateAppPermissionAsync(
            appId,
            new CreatePermissionRequest("invoice.view", "Melihat invoice"),
            null, "127.0.0.1", "xUnit");

        var permApprove = await _adminService.CreateAppPermissionAsync(
            appId,
            new CreatePermissionRequest("invoice.approve", "Menyetujui invoice"),
            null, "127.0.0.1", "xUnit");

        // 3. Buat role 'FinanceManager' dengan kedua permission tersebut
        var role = await _adminService.CreateAppRoleAsync(
            appId,
            new CreateRoleRequest("FinanceManager", "Manajer Keuangan", [permView.Id, permApprove.Id]),
            null, "127.0.0.1", "xUnit");

        // 4. Buat user baru & tugaskan ke role 'FinanceManager' pada 'erp-core'
        var user = await _adminService.CreateUserAsync(
            new CreateUserRequest("siti", "siti@example.com", "PasswordSiti!123", "Siti Rahma", IsActive: true, IsAccountVerified: true),
            null, "127.0.0.1", "xUnit");

        await _adminService.SetUserAppRolesAsync(
            user.Id,
            appId,
            new SetUserAppRolesRequest([role.Id]),
            null, "127.0.0.1", "xUnit");

        // 5. User login ke 'erp-core'
        var loginResponse = await _authService.LoginAsync(
            new LoginRequest("siti", "PasswordSiti!123", "erp-core"),
            "127.0.0.1",
            "xUnit");

        Assert.NotEmpty(loginResponse.AccessToken);
        Assert.NotEmpty(loginResponse.RefreshToken);
        Assert.Contains("FinanceManager", loginResponse.User.Roles);
        Assert.Contains("invoice.approve", loginResponse.User.Permissions);

        // 6. Simulasi backend 'erp-core' memverifikasi token secara realtime (/api/v1/auth/verify)
        var verifyGranted = await _authService.VerifyTokenRealtimeAsync(
            "erp-core",
            rawApiKey,
            new VerifyTokenRequest(loginResponse.AccessToken, RequiredPermission: "invoice.approve"),
            "127.0.0.1",
            "ERP-Backend");

        Assert.True(verifyGranted.IsValid);
        Assert.True(verifyGranted.IsAuthorized);
        Assert.Equal(user.Id, verifyGranted.UserId);

        var verifyDenied = await _authService.VerifyTokenRealtimeAsync(
            "erp-core",
            rawApiKey,
            new VerifyTokenRequest(loginResponse.AccessToken, RequiredPermission: "payroll.delete"),
            "127.0.0.1",
            "ERP-Backend");

        Assert.True(verifyDenied.IsValid);
        Assert.False(verifyDenied.IsAuthorized);
    }

    [Fact]
    public async Task MultiAppIsolation_UserWithRoleInAppA_ShouldBeForbiddenToLoginToAppB()
    {
        var appA = await _adminService.CreateClientAppAsync(
            new CreateClientAppRequest("app-a", "App A"), null, null, null);
        var appB = await _adminService.CreateClientAppAsync(
            new CreateClientAppRequest("app-b", "App B"), null, null, null);

        var roleA = await _adminService.CreateAppRoleAsync(
            appA.App.Id, new CreateRoleRequest("AdminA"), null, null, null);

        var user = await _adminService.CreateUserAsync(
            new CreateUserRequest("doni", "doni@example.com", "PasswordDoni!123", "Doni Pratama"),
            null, null, null);

        await _adminService.SetUserAppRolesAsync(
            user.Id, appA.App.Id, new SetUserAppRolesRequest([roleA.Id]), null, null, null);

        // Login ke App A berhasil
        var loginA = await _authService.LoginAsync(
            new LoginRequest("doni", "PasswordDoni!123", "app-a"), "127.0.0.1", "xUnit");
        Assert.Equal("app-a", loginA.User.ClientId);

        // Login ke App B wajib ditolak (403 Forbidden) karena tidak punya role di App B
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("doni", "PasswordDoni!123", "app-b"), "127.0.0.1", "xUnit"));
    }

    [Fact]
    public async Task AccountLockout_ShouldLockUserAfterMaxFailedLoginAttempts_AndAllowAdminUnlock()
    {
        var app = await _adminService.CreateClientAppAsync(
            new CreateClientAppRequest("portal-app", "Portal App"), null, null, null);
        var role = await _adminService.CreateAppRoleAsync(
            app.App.Id, new CreateRoleRequest("Member"), null, null, null);
        var user = await _adminService.CreateUserAsync(
            new CreateUserRequest("rina", "rina@example.com", "PasswordBenar!123", "Rina Wijaya"),
            null, null, null);
        await _adminService.SetUserAppRolesAsync(
            user.Id, app.App.Id, new SetUserAppRolesRequest([role.Id]), null, null, null);

        // Percobaan gagal 1 & 2 -> Unauthorized (401)
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("rina", "Salah1", "portal-app"), "127.0.0.1", "xUnit"));
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("rina", "Salah2", "portal-app"), "127.0.0.1", "xUnit"));

        // Percobaan gagal ke-3 (MaxFailedLoginAttempts = 3) -> Terkunci (ForbiddenAccessException)
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("rina", "Salah3", "portal-app"), "127.0.0.1", "xUnit"));

        // Bahkan dengan password yang benar sekalipun tetap ditolak selama masa lockout
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("rina", "PasswordBenar!123", "portal-app"), "127.0.0.1", "xUnit"));

        // Admin membuka kunci (unlock) akun
        await _adminService.UnlockUserAsync(user.Id, null, "127.0.0.1", "xUnit");

        // Sekarang login dengan password benar kembali berhasil
        var loginSuccess = await _authService.LoginAsync(
            new LoginRequest("rina", "PasswordBenar!123", "portal-app"), "127.0.0.1", "xUnit");
        Assert.NotEmpty(loginSuccess.AccessToken);
    }

    [Fact]
    public async Task RefreshTokenRotation_AndReuseDetection_ShouldRevokeAllActiveSessionsWhenRotatedTokenReplayed()
    {
        var app = await _adminService.CreateClientAppAsync(
            new CreateClientAppRequest("mobile-app", "Mobile App"), null, null, null);
        var role = await _adminService.CreateAppRoleAsync(
            app.App.Id, new CreateRoleRequest("User"), null, null, null);
        var user = await _adminService.CreateUserAsync(
            new CreateUserRequest("ari", "ari@example.com", "PasswordAri!123", "Ari Wibowo"),
            null, null, null);
        await _adminService.SetUserAppRolesAsync(
            user.Id, app.App.Id, new SetUserAppRolesRequest([role.Id]), null, null, null);

        // 1. Login awal menghasilkan RefreshToken #1
        var initialLogin = await _authService.LoginAsync(
            new LoginRequest("ari", "PasswordAri!123", "mobile-app"), "127.0.0.1", "xUnit");
        var refreshToken1 = initialLogin.RefreshToken;

        // 2. Rotasi RefreshToken #1 -> menghasilkan RefreshToken #2
        var rotated = await _authService.RefreshTokenAsync(
            new RefreshTokenRequest(refreshToken1, "mobile-app"), "127.0.0.1", "xUnit");
        var refreshToken2 = rotated.RefreshToken;
        Assert.NotEqual(refreshToken1, refreshToken2);

        // 3. Simulasi penyerang mencoba memakai ulang RefreshToken #1 yang sudah dirotasi!
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest(refreshToken1, "mobile-app"), "10.0.0.99", "Attacker"));

        // 4. Pastikan RefreshToken #2 milik user ikut otomatis dicabut karena deteksi kebocoran token
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest(refreshToken2, "mobile-app"), "127.0.0.1", "xUnit"));
    }

    [Fact]
    public async Task AccountActivation_AndPasswordReset_ShouldEnforceOneTimeToken_AndRevokeRefreshTokens()
    {
        var app = await _adminService.CreateClientAppAsync(
            new CreateClientAppRequest("crm-app", "CRM App"), null, null, null);
        var role = await _adminService.CreateAppRoleAsync(
            app.App.Id, new CreateRoleRequest("Sales"), null, null, null);

        // Buat user yang belum terverifikasi (IsAccountVerified: false)
        var user = await _adminService.CreateUserAsync(
            new CreateUserRequest("maya", "maya@example.com", "PasswordLama!123", "Maya Sari", IsActive: true, IsAccountVerified: false),
            null, null, null);
        await _adminService.SetUserAppRolesAsync(
            user.Id, app.App.Id, new SetUserAppRolesRequest([role.Id]), null, null, null);

        // Login sebelum aktivasi harus ditolak
        await Assert.ThrowsAsync<ForbiddenAccessException>(() =>
            _authService.LoginAsync(new LoginRequest("maya", "PasswordLama!123", "crm-app"), "127.0.0.1", "xUnit"));

        // 1. Minta & konfirmasi token aktivasi akun
        await _authService.RequestAccountActivationAsync(
            new RequestAccountActivationRequest("maya@example.com"), "127.0.0.1", "xUnit");
        Assert.NotNull(_notificationDispatcher.LastActivationToken);

        await _authService.ConfirmAccountActivationAsync(
            new ConfirmAccountActivationRequest(_notificationDispatcher.LastActivationToken!), "127.0.0.1", "xUnit");

        // Pemakaian ulang token aktivasi yang sama harus ditolak
        await Assert.ThrowsAsync<ArgumentException>(() =>
            _authService.ConfirmAccountActivationAsync(
                new ConfirmAccountActivationRequest(_notificationDispatcher.LastActivationToken!), "127.0.0.1", "xUnit"));

        // 2. Login setelah aktivasi berhasil
        var loginBeforeReset = await _authService.LoginAsync(
            new LoginRequest("maya", "PasswordLama!123", "crm-app"), "127.0.0.1", "xUnit");

        // 3. Forgot & Reset Password
        await _authService.ForgotPasswordAsync(
            new ForgotPasswordRequest("maya@example.com"), "127.0.0.1", "xUnit");
        Assert.NotNull(_notificationDispatcher.LastResetToken);

        await _authService.ResetPasswordAsync(
            new ResetPasswordRequest(_notificationDispatcher.LastResetToken!, "PasswordBaru!456"), "127.0.0.1", "xUnit");

        // RefreshToken lama wajib sudah dicabut setelah reset password
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _authService.RefreshTokenAsync(new RefreshTokenRequest(loginBeforeReset.RefreshToken, "crm-app"), "127.0.0.1", "xUnit"));

        // Login dengan password baru berhasil
        var loginAfterReset = await _authService.LoginAsync(
            new LoginRequest("maya", "PasswordBaru!456", "crm-app"), "127.0.0.1", "xUnit");
        Assert.NotEmpty(loginAfterReset.AccessToken);
    }

    public void Dispose()
    {
        _rsaKeyProvider.Dispose();
        _dbContext.Dispose();
    }

    private sealed class TestNotificationDispatcher : IAuthNotificationDispatcher
    {
        public string? LastActivationToken { get; private set; }
        public string? LastResetToken { get; private set; }

        public Task SendAccountActivationTokenAsync(
            Guid userId,
            string email,
            string fullName,
            string rawActivationToken,
            DateTime expiresAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastActivationToken = rawActivationToken;
            return Task.CompletedTask;
        }

        public Task SendPasswordResetTokenAsync(
            Guid userId,
            string email,
            string fullName,
            string rawResetToken,
            DateTime expiresAtUtc,
            CancellationToken cancellationToken = default)
        {
            LastResetToken = rawResetToken;
            return Task.CompletedTask;
        }
    }
}

