using System.ComponentModel.DataAnnotations;

namespace AuthApi.Application.Features.Auth.DTOs;

public sealed record LoginRequest(
    [property: Required, MaxLength(256)] string UsernameOrEmail,
    [property: Required, MaxLength(256)] string Password,
    [property: Required, MaxLength(100)] string ClientId);

public sealed record RefreshTokenRequest(
    [property: Required, MaxLength(512)] string RefreshToken,
    [property: MaxLength(100)] string? ClientId = null);

public sealed record LogoutRequest(
    [property: MaxLength(512)] string? RefreshToken = null);

public sealed record ChangePasswordRequest(
    [property: Required, MaxLength(256)] string CurrentPassword,
    [property: Required, MinLength(8), MaxLength(256)] string NewPassword);

public sealed record RequestAccountActivationRequest(
    [property: Required, MaxLength(256)] string EmailOrUsername);

public sealed record ConfirmAccountActivationRequest(
    [property: Required, MaxLength(512)] string Token);

public sealed record ForgotPasswordRequest(
    [property: Required, MaxLength(256)] string EmailOrUsername);

public sealed record ResetPasswordRequest(
    [property: Required, MaxLength(512)] string Token,
    [property: Required, MinLength(8), MaxLength(256)] string NewPassword);

public sealed record VerifyTokenRequest(
    [property: Required, MaxLength(4096)] string AccessToken,
    [property: MaxLength(150)] string? RequiredPermission = null,
    [property: MaxLength(100)] string? RequiredRole = null);

public sealed record VerifyTokenResponse(
    bool IsValid,
    bool IsAuthorized,
    Guid? UserId,
    string? Username,
    string? Email,
    string? FullName,
    string ClientId,
    DateTime? ExpiresAtUtc,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? Reason);

public sealed record UserPermissionsResponse(
    Guid UserId,
    string Username,
    string Email,
    string ClientId,
    bool IsUserActive,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    DateTime FetchedAtUtc);

public sealed record UserProfileSummaryDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    string ClientId,
    bool IsAccountVerified,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions);

public sealed record AuthTokenResponse(
    string AccessToken,
    string TokenType,
    int ExpiresInSeconds,
    DateTime ExpiresAtUtc,
    string RefreshToken,
    DateTime RefreshTokenExpiresAtUtc,
    UserProfileSummaryDto User);

public sealed record MessageResponse(string Message);
