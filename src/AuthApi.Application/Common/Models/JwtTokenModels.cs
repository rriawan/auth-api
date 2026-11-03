namespace AuthApi.Application.Common.Models;

public sealed record JwtTokenGenerationRequest(
    Guid UserId,
    string Username,
    string Email,
    string FullName,
    string ClientId,
    IReadOnlyCollection<string> Roles,
    IReadOnlyCollection<string> Permissions);

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc,
    int ExpiresInSeconds,
    string JwtId);

public sealed record TokenClaimsPrincipalResult(
    bool IsValid,
    Guid? UserId,
    string? Username,
    string? Email,
    string? AudienceClientId,
    IReadOnlyList<string> Roles,
    IReadOnlyList<string> Permissions,
    string? JwtId,
    DateTime? ExpiresAtUtc,
    string? ErrorReason);

