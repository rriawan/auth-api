using System.IdentityModel.Tokens.Jwt;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Auth;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.Constants;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IAuthService authService,
    IClientApiKeyValidator clientApiKeyValidator) : ControllerBase
{
    public const string ClientIdHeaderName = "X-Client-Id";
    public const string ApiKeyHeaderName = "X-Api-Key";

    /// <summary>
    /// Autentikasi pengguna dengan username/email, password, dan clientId aplikasi tujuan.
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicAuth)]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokenResponse>> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var response = await authService.LoginAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Menukar Refresh Token yang masih aktif dengan pasangan Access Token + Refresh Token baru (Refresh Token Rotation).
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicAuth)]
    [ProducesResponseType(typeof(AuthTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<AuthTokenResponse>> Refresh(
        [FromBody] RefreshTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var response = await authService.RefreshTokenAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(response);
    }

    /// <summary>
    /// Mencabut (revoke) Refresh Token yang sedang digunakan dan mencatat ke AuditLog.
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageResponse>> Logout(
        [FromBody] LogoutRequest? request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();
        var userId = TryGetAuthenticatedUserId();
        var clientId = User.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;

        await authService.LogoutAsync(
            request ?? new LogoutRequest(),
            userId,
            clientId,
            ipAddress,
            userAgent,
            cancellationToken);

        return Ok(new MessageResponse("Sesi berhasil diakhiri (logout)."));
    }

    /// <summary>
    /// Meminta pembuatan token aktivasi akun baru (dikirim melalui abstraksi IAuthNotificationDispatcher).
    /// </summary>
    [HttpPost("activate-account/request")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicAuth)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> RequestAccountActivation(
        [FromBody] RequestAccountActivationRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await authService.RequestAccountActivationAsync(request, ipAddress, userAgent, cancellationToken);

        return Ok(new MessageResponse(
            "Jika akun terdaftar dan belum terverifikasi, instruksi aktivasi akun telah dikirimkan."));
    }

    /// <summary>
    /// Menukarkan token aktivasi sekali pakai untuk mengaktifkan status verifikasi akun pengguna.
    /// </summary>
    [HttpPost("activate-account/confirm")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicAuth)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> ConfirmAccountActivation(
        [FromBody] ConfirmAccountActivationRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await authService.ConfirmAccountActivationAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(new MessageResponse("Akun berhasil diaktivasi. Silakan login."));
    }

    /// <summary>
    /// Meminta token reset password sekali pakai (selalu mengembalikan pesan sukses generik agar tidak membocorkan enumerasi akun).
    /// </summary>
    [HttpPost("forgot-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicAuth)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await authService.ForgotPasswordAsync(request, ipAddress, userAgent, cancellationToken);

        return Ok(new MessageResponse(
            "Jika email atau akun terdaftar, instruksi untuk mereset password telah dikirimkan."));
    }

    /// <summary>
    /// Memvalidasi token reset password sekali pakai, memperbarui password, dan mencabut seluruh sesi refresh token lama milik pengguna.
    /// </summary>
    [HttpPost("reset-password")]
    [AllowAnonymous]
    [EnableRateLimiting(RateLimitPolicies.PublicAuth)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status429TooManyRequests)]
    public async Task<ActionResult<MessageResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await authService.ResetPasswordAsync(request, ipAddress, userAgent, cancellationToken);
        return Ok(new MessageResponse("Password berhasil direset dan seluruh sesi lama telah dicabut. Silakan login kembali."));
    }

    /// <summary>
    /// Mengambil informasi profil pengguna yang sedang login beserta daftar role dan permission pada aplikasi terkait.
    /// </summary>
    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(UserProfileSummaryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<UserProfileSummaryDto>> Me(CancellationToken cancellationToken)
    {
        var userId = GetRequiredAuthenticatedUserId();
        var clientId = User.FindFirst(JwtRegisteredClaimNames.Aud)?.Value
            ?? throw new UnauthorizedAccessException("Klaim audience (clientId) tidak ditemukan pada token.");

        var profile = await authService.GetCurrentUserProfileAsync(userId, clientId, cancellationToken);
        return Ok(profile);
    }

    /// <summary>
    /// Mengganti password pengguna yang sedang login, memverifikasi password lama, dan mencabut seluruh sesi refresh token aktif.
    /// </summary>
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<MessageResponse>> ChangePassword(
        [FromBody] ChangePasswordRequest request,
        CancellationToken cancellationToken)
    {
        var userId = GetRequiredAuthenticatedUserId();
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        await authService.ChangePasswordAsync(userId, request, ipAddress, userAgent, cancellationToken);
        return Ok(new MessageResponse("Password berhasil diperbarui dan seluruh sesi lama telah dicabut."));
    }

    /// <summary>
    /// Validasi realtime sebuah Access Token oleh backend aplikasi konsumen menggunakan header X-Client-Id dan X-Api-Key,
    /// sekaligus mengecek apakah user masih aktif dan memiliki requiredPermission/requiredRole tertentu.
    /// </summary>
    [HttpPost("verify")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(VerifyTokenResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    public async Task<ActionResult<VerifyTokenResponse>> VerifyToken(
        [FromHeader(Name = ClientIdHeaderName)] string? clientIdHeader,
        [FromHeader(Name = ApiKeyHeaderName)] string? apiKeyHeader,
        [FromBody] VerifyTokenRequest request,
        CancellationToken cancellationToken)
    {
        var ipAddress = GetClientIpAddress();
        var userAgent = Request.Headers.UserAgent.ToString();

        var response = await authService.VerifyTokenRealtimeAsync(
            clientIdHeader ?? string.Empty,
            apiKeyHeader ?? string.Empty,
            request,
            ipAddress,
            userAgent,
            cancellationToken);

        return Ok(response);
    }

    /// <summary>
    /// Mengambil daftar terbaru roles dan permissions seorang user untuk aplikasi tertentu.
    /// Mendukung autentikasi via Bearer JWT maupun via header X-Client-Id + X-Api-Key.
    /// </summary>
    [HttpGet("permissions")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(UserPermissionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserPermissionsResponse>> GetPermissions(
        [FromHeader(Name = ClientIdHeaderName)] string? clientIdHeader,
        [FromHeader(Name = ApiKeyHeaderName)] string? apiKeyHeader,
        [FromQuery] Guid? userId,
        [FromQuery] string? clientId,
        CancellationToken cancellationToken)
    {
        // Mode 1: Autentikasi menggunakan X-Client-Id + X-Api-Key (komunikasi antar-backend)
        if (!string.IsNullOrWhiteSpace(clientIdHeader) || !string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            var authenticatedApp = await clientApiKeyValidator.ValidateAsync(
                clientIdHeader,
                apiKeyHeader,
                cancellationToken);

            var targetUserId = userId ?? TryGetAuthenticatedUserId()
                ?? throw new ArgumentException(
                    "Parameter query 'userId' wajib disertakan saat memanggil endpoint permissions menggunakan Client API Key.");

            // Isolasi aplikasi: ClientApp biasa hanya dapat mengecek permission miliknya sendiri,
            // kecuali 'auth-admin' yang diizinkan menentukan parameter query clientId.
            var targetClientId = string.Equals(
                    authenticatedApp.ClientId,
                    SystemConstants.DefaultAdminClientId,
                    StringComparison.OrdinalIgnoreCase) && !string.IsNullOrWhiteSpace(clientId)
                ? clientId.Trim()
                : authenticatedApp.ClientId;

            var result = await authService.GetUserPermissionsAsync(targetUserId, targetClientId, cancellationToken);
            return Ok(result);
        }

        // Mode 2: Autentikasi menggunakan Bearer JWT
        var jwtUserId = TryGetAuthenticatedUserId();
        if (jwtUserId.HasValue)
        {
            var jwtClientId = User.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
            var targetClientId = !string.IsNullOrWhiteSpace(clientId)
                ? clientId.Trim()
                : jwtClientId
                  ?? throw new UnauthorizedAccessException("Klaim audience (clientId) tidak ditemukan pada token.");

            // Hanya pengguna 'auth-admin' dengan permission 'users.read' yang boleh memeriksa userId milik pengguna lain via JWT
            var targetUserId = jwtUserId.Value;
            if (userId.HasValue && userId.Value != jwtUserId.Value)
            {
                var isAdminReader = string.Equals(jwtClientId, SystemConstants.DefaultAdminClientId, StringComparison.OrdinalIgnoreCase)
                    && User.FindAll("permissions").Any(c => string.Equals(c.Value, SystemConstants.Permissions.UsersRead, StringComparison.OrdinalIgnoreCase));

                if (!isAdminReader)
                {
                    throw new UnauthorizedAccessException("Anda hanya dapat melihat daftar permission milik akun Anda sendiri.");
                }

                targetUserId = userId.Value;
            }

            var result = await authService.GetUserPermissionsAsync(targetUserId, targetClientId, cancellationToken);
            return Ok(result);
        }

        throw new UnauthorizedAccessException(
            "Autentikasi diperlukan: sertakan Bearer JWT atau pasangan header 'X-Client-Id' dan 'X-Api-Key'.");
    }

    private Guid? TryGetAuthenticatedUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private Guid GetRequiredAuthenticatedUserId() =>
        TryGetAuthenticatedUserId()
        ?? throw new UnauthorizedAccessException("Identitas pengguna (sub) pada token tidak valid.");

    private string? GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}
