using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthApi.Application.Features.Auth;

public sealed class AuthService(
    IAppDbContext dbContext,
    IPasswordHasherService passwordHasher,
    ITokenCryptoService tokenCryptoService,
    IJwtTokenService jwtTokenService,
    IAuditLogService auditLogService,
    IAuthNotificationDispatcher notificationDispatcher,
    IClientApiKeyValidator clientApiKeyValidator,
    IOptions<JwtSettings> jwtOptions,
    IOptions<SecuritySettings> securityOptions) : IAuthService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;
    private readonly SecuritySettings _securitySettings = securityOptions.Value;

    public async Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var normalizedIdentifier = request.UsernameOrEmail.Trim();
        var normalizedClientId = request.ClientId.Trim();

        // 1. Validasi ClientApp
        var clientApp = await dbContext.ClientApps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == normalizedClientId, cancellationToken);

        if (clientApp is null || !clientApp.IsActive)
        {
            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: null,
                clientAppId: clientApp?.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Invalid or inactive ClientApp", clientId = normalizedClientId, identifier = normalizedIdentifier },
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Aplikasi klien (clientId) tidak ditemukan atau sedang dinonaktifkan.");
        }

        // 2. Cari User berdasarkan Username atau Email
        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Username == normalizedIdentifier || x.Email == normalizedIdentifier,
                cancellationToken);

        if (user is null)
        {
            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: null,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "User not found", identifier = normalizedIdentifier },
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Username/email atau password salah.");
        }

        // 3. Cek Lockout
        if (user.IsLockedOut(nowUtc))
        {
            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: user.Id,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Account currently locked out", lockoutEndUtc = user.LockoutEndUtc },
                cancellationToken: cancellationToken);

            throw new ForbiddenAccessException(
                $"Akun sedang terkunci sementara karena terlalu banyak percobaan gagal. Silakan coba lagi setelah {user.LockoutEndUtc:yyyy-MM-dd HH:mm:ss} UTC.");
        }

        // 4. Verifikasi Password
        var isPasswordValid = passwordHasher.VerifyPassword(user, user.PasswordHash, request.Password, out var needsRehash);
        if (!isPasswordValid)
        {
            user.FailedLoginCount++;
            var maxAttempts = _securitySettings.MaxFailedLoginAttempts > 0 ? _securitySettings.MaxFailedLoginAttempts : 5;
            var lockoutMinutes = _securitySettings.LockoutDurationMinutes > 0 ? _securitySettings.LockoutDurationMinutes : 15;

            if (user.FailedLoginCount >= maxAttempts)
            {
                user.LockoutEndUtc = nowUtc.AddMinutes(lockoutMinutes);
                user.FailedLoginCount = 0;
                user.UpdatedAtUtc = nowUtc;
                await dbContext.SaveChangesAsync(cancellationToken);

                await auditLogService.LogAsync(
                    AuditEventType.AccountLockedOut,
                    isSuccess: false,
                    userId: user.Id,
                    clientAppId: clientApp.Id,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    detail: new { maxAttempts, lockoutEndUtc = user.LockoutEndUtc },
                    cancellationToken: cancellationToken);

                throw new ForbiddenAccessException(
                    $"Akun telah dikunci sementara selama {lockoutMinutes} menit karena gagal login {maxAttempts} kali berturut-turut.");
            }

            user.UpdatedAtUtc = nowUtc;
            await dbContext.SaveChangesAsync(cancellationToken);

            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: user.Id,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Invalid password", failedLoginCount = user.FailedLoginCount },
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Username/email atau password salah.");
        }

        // 5. Cek Status Aktif & Verifikasi Akun
        if (!user.IsActive)
        {
            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: user.Id,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "User account is inactive" },
                cancellationToken: cancellationToken);

            throw new ForbiddenAccessException("Akun pengguna sedang dinonaktifkan.");
        }

        if (!user.IsAccountVerified)
        {
            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: user.Id,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Account is not verified" },
                cancellationToken: cancellationToken);

            throw new ForbiddenAccessException("Akun pengguna belum diaktivasi/diverifikasi.");
        }

        // 6. Muat Roles & Permissions milik User khusus untuk ClientApp tujuan
        var (roles, permissions) = await LoadUserAppAccessAsync(user.Id, clientApp.Id, cancellationToken);
        if (roles.Count == 0)
        {
            await auditLogService.LogAsync(
                AuditEventType.LoginFailed,
                isSuccess: false,
                userId: user.Id,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "User has no role assigned in target ClientApp", clientId = clientApp.ClientId },
                cancellationToken: cancellationToken);

            throw new ForbiddenAccessException(
                $"Akun Anda tidak memiliki hak akses (Role) pada aplikasi '{clientApp.ClientId}'.");
        }

        // 7. Reset FailedLoginCount / Lockout & Rehash jika diperlukan
        if (user.FailedLoginCount > 0 || user.LockoutEndUtc.HasValue || needsRehash)
        {
            user.FailedLoginCount = 0;
            user.LockoutEndUtc = null;
            if (needsRehash)
            {
                user.PasswordHash = passwordHasher.HashPassword(user, request.Password);
            }
            user.UpdatedAtUtc = nowUtc;
        }

        // 8. Terbitkan Access Token (JWT RS256) & Refresh Token (SHA-256 Hashed)
        var jwtResult = jwtTokenService.GenerateAccessToken(new JwtTokenGenerationRequest(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ClientId: clientApp.ClientId,
            Roles: roles,
            Permissions: permissions));

        var rawRefreshToken = tokenCryptoService.GenerateSecureToken(32, "rt_");
        var refreshTokenHash = tokenCryptoService.ComputeSha256Hash(rawRefreshToken);
        var refreshDays = _jwtSettings.RefreshTokenExpirationDays > 0 ? _jwtSettings.RefreshTokenExpirationDays : 14;
        var refreshExpiresAtUtc = nowUtc.AddDays(refreshDays);

        var refreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClientAppId = clientApp.Id,
            TokenHash = refreshTokenHash,
            ExpiresAtUtc = refreshExpiresAtUtc,
            CreatedAtUtc = nowUtc,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        dbContext.RefreshTokens.Add(refreshTokenEntity);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.LoginSuccess,
            isSuccess: true,
            userId: user.Id,
            clientAppId: clientApp.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { clientId = clientApp.ClientId, roles, jwtId = jwtResult.JwtId },
            cancellationToken: cancellationToken);

        return new AuthTokenResponse(
            AccessToken: jwtResult.AccessToken,
            TokenType: "Bearer",
            ExpiresInSeconds: jwtResult.ExpiresInSeconds,
            ExpiresAtUtc: jwtResult.ExpiresAtUtc,
            RefreshToken: rawRefreshToken,
            RefreshTokenExpiresAtUtc: refreshExpiresAtUtc,
            User: new UserProfileSummaryDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                FullName: user.FullName,
                ClientId: clientApp.ClientId,
                IsAccountVerified: user.IsAccountVerified,
                Roles: roles,
                Permissions: permissions));
    }

    public async Task<AuthTokenResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var tokenHash = tokenCryptoService.ComputeSha256Hash(request.RefreshToken.Trim());

        var storedToken = await dbContext.RefreshTokens
            .Include(x => x.User)
            .Include(x => x.ClientApp)
            .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

        if (storedToken is null)
        {
            await auditLogService.LogAsync(
                AuditEventType.TokenRefreshed,
                isSuccess: false,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Refresh token not found" },
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Refresh token tidak valid.");
        }

        // Deteksi Pemakaian Ulang Token yang Sudah Dirotasi (Refresh Token Reuse Detection)
        if (storedToken.IsRevoked)
        {
            if (!string.IsNullOrEmpty(storedToken.ReplacedByTokenHash))
            {
                var activeUserTokens = await dbContext.RefreshTokens
                    .Where(x => x.UserId == storedToken.UserId && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
                    .ToListAsync(cancellationToken);

                foreach (var session in activeUserTokens)
                {
                    session.RevokedAtUtc = nowUtc;
                    session.RevokeReason = "Compromised refresh token reuse detected";
                }

                await dbContext.SaveChangesAsync(cancellationToken);

                await auditLogService.LogAsync(
                    AuditEventType.RefreshTokenReuseDetected,
                    isSuccess: false,
                    userId: storedToken.UserId,
                    clientAppId: storedToken.ClientAppId,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    detail: new
                    {
                        reusedTokenId = storedToken.Id,
                        revokedActiveSessionsCount = activeUserTokens.Count
                    },
                    cancellationToken: cancellationToken);
            }

            throw new UnauthorizedAccessException("Refresh token sudah tidak berlaku atau telah dicabut.");
        }

        if (storedToken.IsExpired(nowUtc))
        {
            await auditLogService.LogAsync(
                AuditEventType.TokenRefreshed,
                isSuccess: false,
                userId: storedToken.UserId,
                clientAppId: storedToken.ClientAppId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Refresh token expired", expiresAtUtc = storedToken.ExpiresAtUtc },
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Refresh token telah kedaluwarsa. Silakan login kembali.");
        }

        if (!string.IsNullOrWhiteSpace(request.ClientId) &&
            !string.Equals(storedToken.ClientApp.ClientId, request.ClientId.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            throw new UnauthorizedAccessException("Refresh token tidak sesuai dengan clientId yang diminta.");
        }

        var user = storedToken.User;
        var clientApp = storedToken.ClientApp;

        if (!clientApp.IsActive)
        {
            throw new ForbiddenAccessException("Aplikasi klien sedang dinonaktifkan.");
        }

        if (!user.IsActive || !user.IsAccountVerified || user.IsLockedOut(nowUtc))
        {
            throw new ForbiddenAccessException("Akun pengguna sedang tidak aktif atau terkunci.");
        }

        var (roles, permissions) = await LoadUserAppAccessAsync(user.Id, clientApp.Id, cancellationToken);
        if (roles.Count == 0)
        {
            throw new ForbiddenAccessException(
                $"Hak akses pengguna pada aplikasi '{clientApp.ClientId}' telah dicabut.");
        }

        // Rotasi Refresh Token (Revoke token lama + buat token baru)
        var newRawRefreshToken = tokenCryptoService.GenerateSecureToken(32, "rt_");
        var newRefreshTokenHash = tokenCryptoService.ComputeSha256Hash(newRawRefreshToken);
        var refreshDays = _jwtSettings.RefreshTokenExpirationDays > 0 ? _jwtSettings.RefreshTokenExpirationDays : 14;
        var newRefreshExpiresAtUtc = nowUtc.AddDays(refreshDays);

        storedToken.RevokedAtUtc = nowUtc;
        storedToken.ReplacedByTokenHash = newRefreshTokenHash;
        storedToken.RevokeReason = "Rotated";

        var newRefreshTokenEntity = new RefreshToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            ClientAppId = clientApp.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAtUtc = newRefreshExpiresAtUtc,
            CreatedAtUtc = nowUtc,
            CreatedByIp = ipAddress,
            UserAgent = userAgent
        };

        dbContext.RefreshTokens.Add(newRefreshTokenEntity);

        var jwtResult = jwtTokenService.GenerateAccessToken(new JwtTokenGenerationRequest(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ClientId: clientApp.ClientId,
            Roles: roles,
            Permissions: permissions));

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.TokenRefreshed,
            isSuccess: true,
            userId: user.Id,
            clientAppId: clientApp.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { previousTokenId = storedToken.Id, newTokenId = newRefreshTokenEntity.Id, jwtId = jwtResult.JwtId },
            cancellationToken: cancellationToken);

        return new AuthTokenResponse(
            AccessToken: jwtResult.AccessToken,
            TokenType: "Bearer",
            ExpiresInSeconds: jwtResult.ExpiresInSeconds,
            ExpiresAtUtc: jwtResult.ExpiresAtUtc,
            RefreshToken: newRawRefreshToken,
            RefreshTokenExpiresAtUtc: newRefreshExpiresAtUtc,
            User: new UserProfileSummaryDto(
                Id: user.Id,
                Username: user.Username,
                Email: user.Email,
                FullName: user.FullName,
                ClientId: clientApp.ClientId,
                IsAccountVerified: user.IsAccountVerified,
                Roles: roles,
                Permissions: permissions));
    }

    public async Task LogoutAsync(
        LogoutRequest request,
        Guid? authenticatedUserId,
        string? authenticatedClientId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;

        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            var tokenHash = tokenCryptoService.ComputeSha256Hash(request.RefreshToken.Trim());
            var storedToken = await dbContext.RefreshTokens
                .FirstOrDefaultAsync(x => x.TokenHash == tokenHash, cancellationToken);

            if (storedToken is not null && !storedToken.IsRevoked)
            {
                if (authenticatedUserId.HasValue && storedToken.UserId != authenticatedUserId.Value)
                {
                    throw new ForbiddenAccessException("Refresh token bukan milik pengguna yang sedang login.");
                }

                storedToken.RevokedAtUtc = nowUtc;
                storedToken.RevokeReason = "User logout";
                await dbContext.SaveChangesAsync(cancellationToken);

                await auditLogService.LogAsync(
                    AuditEventType.Logout,
                    isSuccess: true,
                    userId: storedToken.UserId,
                    clientAppId: storedToken.ClientAppId,
                    ipAddress: ipAddress,
                    userAgent: userAgent,
                    detail: new { refreshTokenId = storedToken.Id },
                    cancellationToken: cancellationToken);
                return;
            }
        }

        if (authenticatedUserId.HasValue)
        {
            var query = dbContext.RefreshTokens
                .Where(x => x.UserId == authenticatedUserId.Value && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc);

            Guid? clientAppId = null;
            if (!string.IsNullOrWhiteSpace(authenticatedClientId))
            {
                var app = await dbContext.ClientApps
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.ClientId == authenticatedClientId, cancellationToken);
                if (app is not null)
                {
                    clientAppId = app.Id;
                    query = query.Where(x => x.ClientAppId == app.Id);
                }
            }

            var activeTokens = await query.ToListAsync(cancellationToken);
            foreach (var token in activeTokens)
            {
                token.RevokedAtUtc = nowUtc;
                token.RevokeReason = "User logout";
            }

            await dbContext.SaveChangesAsync(cancellationToken);

            await auditLogService.LogAsync(
                AuditEventType.Logout,
                isSuccess: true,
                userId: authenticatedUserId.Value,
                clientAppId: clientAppId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { revokedSessionsCount = activeTokens.Count },
                cancellationToken: cancellationToken);
            return;
        }

        throw new UnauthorizedAccessException("Sertakan refreshToken yang valid atau Bearer token untuk logout.");
    }

    public async Task<UserProfileSummaryDto> GetCurrentUserProfileAsync(
        Guid userId,
        string clientId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        if (!user.IsActive)
        {
            throw new ForbiddenAccessException("Akun pengguna sedang tidak aktif.");
        }

        var clientApp = await dbContext.ClientApps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == clientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aplikasi klien '{clientId}' tidak ditemukan.");

        var (roles, permissions) = await LoadUserAppAccessAsync(user.Id, clientApp.Id, cancellationToken);

        return new UserProfileSummaryDto(
            Id: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ClientId: clientApp.ClientId,
            IsAccountVerified: user.IsAccountVerified,
            Roles: roles,
            Permissions: permissions);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        if (string.Equals(request.CurrentPassword, request.NewPassword, StringComparison.Ordinal))
        {
            throw new ArgumentException("Password baru tidak boleh sama dengan password saat ini.");
        }

        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        if (!user.IsActive)
        {
            throw new ForbiddenAccessException("Akun pengguna sedang tidak aktif.");
        }

        var isCurrentValid = passwordHasher.VerifyPassword(user, user.PasswordHash, request.CurrentPassword, out _);
        if (!isCurrentValid)
        {
            await auditLogService.LogAsync(
                AuditEventType.PasswordChanged,
                isSuccess: false,
                userId: user.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = "Invalid current password" },
                cancellationToken: cancellationToken);

            throw new UnauthorizedAccessException("Password saat ini tidak sesuai.");
        }

        var nowUtc = DateTime.UtcNow;
        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.UpdatedAtUtc = nowUtc;

        var activeSessions = await dbContext.RefreshTokens
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = nowUtc;
            session.RevokeReason = "Password changed";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.PasswordChanged,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { revokedSessionsCount = activeSessions.Count },
            cancellationToken: cancellationToken);
    }

    public async Task RequestAccountActivationAsync(
        RequestAccountActivationRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var identifier = request.EmailOrUsername.Trim();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Email == identifier || x.Username == identifier,
                cancellationToken);

        if (user is null || !user.IsActive || user.IsAccountVerified)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var expirationMinutes = _securitySettings.ActionTokenExpirationMinutes > 0
            ? _securitySettings.ActionTokenExpirationMinutes
            : 30;
        var expiresAtUtc = nowUtc.AddMinutes(expirationMinutes);

        var pendingTokens = await dbContext.ActionTokens
            .Where(x => x.UserId == user.Id
                        && x.Purpose == ActionTokenPurpose.AccountActivation
                        && x.UsedAtUtc == null
                        && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var pending in pendingTokens)
        {
            pending.UsedAtUtc = nowUtc;
        }

        var rawToken = tokenCryptoService.GenerateSecureToken(32, "act_");
        var tokenHash = tokenCryptoService.ComputeSha256Hash(rawToken);

        var actionToken = new ActionToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = ActionTokenPurpose.AccountActivation,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = nowUtc
        };

        dbContext.ActionTokens.Add(actionToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationDispatcher.SendAccountActivationTokenAsync(
            user.Id,
            user.Email,
            user.FullName,
            rawToken,
            expiresAtUtc,
            cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.AccountActivationRequested,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actionTokenId = actionToken.Id, expiresAtUtc },
            cancellationToken: cancellationToken);
    }

    public async Task ConfirmAccountActivationAsync(
        ConfirmAccountActivationRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var tokenHash = tokenCryptoService.ComputeSha256Hash(request.Token.Trim());

        var actionToken = await dbContext.ActionTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash && x.Purpose == ActionTokenPurpose.AccountActivation,
                cancellationToken);

        if (actionToken is null || !actionToken.IsValid(nowUtc))
        {
            await auditLogService.LogAsync(
                AuditEventType.AccountActivated,
                isSuccess: false,
                userId: actionToken?.UserId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new
                {
                    reason = actionToken is null
                        ? "Token not found"
                        : actionToken.IsUsed
                            ? "Token already used"
                            : "Token expired"
                },
                cancellationToken: cancellationToken);

            throw new ArgumentException("Token aktivasi akun tidak valid, sudah pernah digunakan, atau telah kedaluwarsa.");
        }

        var user = actionToken.User;
        if (!user.IsActive)
        {
            throw new ForbiddenAccessException("Akun pengguna sedang dinonaktifkan.");
        }

        actionToken.UsedAtUtc = nowUtc;
        user.IsAccountVerified = true;
        user.UpdatedAtUtc = nowUtc;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.AccountActivated,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actionTokenId = actionToken.Id },
            cancellationToken: cancellationToken);
    }

    public async Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var identifier = request.EmailOrUsername.Trim();
        var user = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Email == identifier || x.Username == identifier,
                cancellationToken);

        if (user is null || !user.IsActive)
        {
            return;
        }

        var nowUtc = DateTime.UtcNow;
        var expirationMinutes = _securitySettings.ActionTokenExpirationMinutes > 0
            ? _securitySettings.ActionTokenExpirationMinutes
            : 30;
        var expiresAtUtc = nowUtc.AddMinutes(expirationMinutes);

        var pendingTokens = await dbContext.ActionTokens
            .Where(x => x.UserId == user.Id
                        && x.Purpose == ActionTokenPurpose.PasswordReset
                        && x.UsedAtUtc == null
                        && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var pending in pendingTokens)
        {
            pending.UsedAtUtc = nowUtc;
        }

        var rawToken = tokenCryptoService.GenerateSecureToken(32, "rst_");
        var tokenHash = tokenCryptoService.ComputeSha256Hash(rawToken);

        var actionToken = new ActionToken
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Purpose = ActionTokenPurpose.PasswordReset,
            TokenHash = tokenHash,
            ExpiresAtUtc = expiresAtUtc,
            CreatedAtUtc = nowUtc
        };

        dbContext.ActionTokens.Add(actionToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        await notificationDispatcher.SendPasswordResetTokenAsync(
            user.Id,
            user.Email,
            user.FullName,
            rawToken,
            expiresAtUtc,
            cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.PasswordResetRequested,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actionTokenId = actionToken.Id, expiresAtUtc },
            cancellationToken: cancellationToken);
    }

    public async Task ResetPasswordAsync(
        ResetPasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.UtcNow;
        var tokenHash = tokenCryptoService.ComputeSha256Hash(request.Token.Trim());

        var actionToken = await dbContext.ActionTokens
            .Include(x => x.User)
            .FirstOrDefaultAsync(
                x => x.TokenHash == tokenHash && x.Purpose == ActionTokenPurpose.PasswordReset,
                cancellationToken);

        if (actionToken is null || !actionToken.IsValid(nowUtc))
        {
            await auditLogService.LogAsync(
                AuditEventType.PasswordResetCompleted,
                isSuccess: false,
                userId: actionToken?.UserId,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new
                {
                    reason = actionToken is null
                        ? "Token not found"
                        : actionToken.IsUsed
                            ? "Token already used"
                            : "Token expired"
                },
                cancellationToken: cancellationToken);

            throw new ArgumentException("Token reset password tidak valid, sudah pernah digunakan, atau telah kedaluwarsa.");
        }

        var user = actionToken.User;
        if (!user.IsActive)
        {
            throw new ForbiddenAccessException("Akun pengguna sedang dinonaktifkan.");
        }

        actionToken.UsedAtUtc = nowUtc;
        user.PasswordHash = passwordHasher.HashPassword(user, request.NewPassword);
        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        user.UpdatedAtUtc = nowUtc;

        var activeSessions = await dbContext.RefreshTokens
            .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = nowUtc;
            session.RevokeReason = "Password reset completed";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.PasswordResetCompleted,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actionTokenId = actionToken.Id, revokedSessionsCount = activeSessions.Count },
            cancellationToken: cancellationToken);
    }

    public async Task<VerifyTokenResponse> VerifyTokenRealtimeAsync(
        string clientIdHeader,
        string apiKeyHeader,
        VerifyTokenRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        // 1. Autentikasi ClientApp pemanggil melalui X-Client-Id & X-Api-Key
        var clientApp = await clientApiKeyValidator.ValidateAsync(
            clientIdHeader,
            apiKeyHeader,
            cancellationToken);

        // 2. Validasi signature & masa berlaku JWT RS256 beserta kecocokan audience (clientId)
        var rawToken = request.AccessToken.Trim();
        if (rawToken.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            rawToken = rawToken["Bearer ".Length..].Trim();
        }

        var tokenResult = jwtTokenService.ValidateAccessToken(rawToken, expectedAudience: clientApp.ClientId);
        if (!tokenResult.IsValid || !tokenResult.UserId.HasValue)
        {
            await auditLogService.LogAsync(
                AuditEventType.TokenVerified,
                isSuccess: false,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = tokenResult.ErrorReason },
                cancellationToken: cancellationToken);

            return new VerifyTokenResponse(
                IsValid: false,
                IsAuthorized: false,
                UserId: null,
                Username: null,
                Email: null,
                FullName: null,
                ClientId: clientApp.ClientId,
                ExpiresAtUtc: null,
                Roles: [],
                Permissions: [],
                Reason: tokenResult.ErrorReason);
        }

        // 3. Cek status realtime User di database
        var nowUtc = DateTime.UtcNow;
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == tokenResult.UserId.Value, cancellationToken);

        if (user is null || !user.IsActive || !user.IsAccountVerified || user.IsLockedOut(nowUtc))
        {
            var reason = user is null
                ? "Akun pengguna tidak ditemukan."
                : !user.IsActive
                    ? "Akun pengguna telah dinonaktifkan."
                    : !user.IsAccountVerified
                        ? "Akun pengguna belum terverifikasi."
                        : "Akun pengguna sedang terkunci (locked out).";

            await auditLogService.LogAsync(
                AuditEventType.TokenVerified,
                isSuccess: false,
                userId: tokenResult.UserId,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason },
                cancellationToken: cancellationToken);

            return new VerifyTokenResponse(
                IsValid: false,
                IsAuthorized: false,
                UserId: tokenResult.UserId,
                Username: user?.Username,
                Email: user?.Email,
                FullName: user?.FullName,
                ClientId: clientApp.ClientId,
                ExpiresAtUtc: tokenResult.ExpiresAtUtc,
                Roles: [],
                Permissions: [],
                Reason: reason);
        }

        // 4. Muat Roles & Permissions realtime milik User pada ClientApp pemanggil
        var (roles, permissions) = await LoadUserAppAccessAsync(user.Id, clientApp.Id, cancellationToken);
        if (roles.Count == 0)
        {
            const string noRoleReason = "Pengguna sudah tidak memiliki hak akses (Role) pada aplikasi ini.";
            await auditLogService.LogAsync(
                AuditEventType.TokenVerified,
                isSuccess: false,
                userId: user.Id,
                clientAppId: clientApp.Id,
                ipAddress: ipAddress,
                userAgent: userAgent,
                detail: new { reason = noRoleReason },
                cancellationToken: cancellationToken);

            return new VerifyTokenResponse(
                IsValid: false,
                IsAuthorized: false,
                UserId: user.Id,
                Username: user.Username,
                Email: user.Email,
                FullName: user.FullName,
                ClientId: clientApp.ClientId,
                ExpiresAtUtc: tokenResult.ExpiresAtUtc,
                Roles: [],
                Permissions: [],
                Reason: noRoleReason);
        }

        // 5. Cek kecocokan RequiredRole & RequiredPermission bila diminta oleh aplikasi konsumen
        var hasRequiredRole = string.IsNullOrWhiteSpace(request.RequiredRole) ||
            roles.Contains(request.RequiredRole.Trim(), StringComparer.OrdinalIgnoreCase);

        var hasRequiredPermission = string.IsNullOrWhiteSpace(request.RequiredPermission) ||
            permissions.Contains(request.RequiredPermission.Trim(), StringComparer.OrdinalIgnoreCase);

        var isAuthorized = hasRequiredRole && hasRequiredPermission;
        string? authReason = null;

        if (!isAuthorized)
        {
            authReason = !hasRequiredRole
                ? $"Pengguna tidak memiliki role wajib '{request.RequiredRole}'."
                : $"Pengguna tidak memiliki permission wajib '{request.RequiredPermission}'.";
        }

        await auditLogService.LogAsync(
            AuditEventType.TokenVerified,
            isSuccess: isAuthorized,
            userId: user.Id,
            clientAppId: clientApp.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new
            {
                requiredRole = request.RequiredRole,
                requiredPermission = request.RequiredPermission,
                isAuthorized
            },
            cancellationToken: cancellationToken);

        return new VerifyTokenResponse(
            IsValid: true,
            IsAuthorized: isAuthorized,
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            FullName: user.FullName,
            ClientId: clientApp.ClientId,
            ExpiresAtUtc: tokenResult.ExpiresAtUtc,
            Roles: roles,
            Permissions: permissions,
            Reason: authReason);
    }

    public async Task<UserPermissionsResponse> GetUserPermissionsAsync(
        Guid userId,
        string targetClientId,
        CancellationToken cancellationToken = default)
    {
        var normalizedClientId = targetClientId.Trim();

        var clientApp = await dbContext.ClientApps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == normalizedClientId, cancellationToken)
            ?? throw new KeyNotFoundException($"Aplikasi klien '{normalizedClientId}' tidak ditemukan.");

        if (!clientApp.IsActive)
        {
            throw new ForbiddenAccessException($"Aplikasi klien '{clientApp.ClientId}' sedang dinonaktifkan.");
        }

        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        var nowUtc = DateTime.UtcNow;
        var isUserActive = user.IsActive && user.IsAccountVerified && !user.IsLockedOut(nowUtc);

        var (roles, permissions) = isUserActive
            ? await LoadUserAppAccessAsync(user.Id, clientApp.Id, cancellationToken)
            : ([], []);

        return new UserPermissionsResponse(
            UserId: user.Id,
            Username: user.Username,
            Email: user.Email,
            ClientId: clientApp.ClientId,
            IsUserActive: isUserActive,
            Roles: roles,
            Permissions: permissions,
            FetchedAtUtc: nowUtc);
    }

    private async Task<(IReadOnlyList<string> Roles, IReadOnlyList<string> Permissions)> LoadUserAppAccessAsync(
        Guid userId,
        Guid clientAppId,
        CancellationToken cancellationToken)
    {
        var assignedRoles = await dbContext.UserAppRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ClientAppId == clientAppId)
            .Select(x => new { x.RoleId, x.Role.Name })
            .ToListAsync(cancellationToken);

        if (assignedRoles.Count == 0)
        {
            return ([], []);
        }

        var roleNames = assignedRoles
            .Select(x => x.Name)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var roleIds = assignedRoles.Select(x => x.RoleId).Distinct().ToList();

        var permissionCodes = await dbContext.RolePermissions
            .AsNoTracking()
            .Where(x => roleIds.Contains(x.RoleId) && x.Permission.ClientAppId == clientAppId)
            .Select(x => x.Permission.Code)
            .Distinct()
            .OrderBy(x => x)
            .ToListAsync(cancellationToken);

        return (roleNames, permissionCodes);
    }
}
