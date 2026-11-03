using AuthApi.Application.Features.Auth.DTOs;

namespace AuthApi.Application.Features.Auth;

public interface IAuthService
{
    Task<AuthTokenResponse> LoginAsync(
        LoginRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<AuthTokenResponse> RefreshTokenAsync(
        RefreshTokenRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task LogoutAsync(
        LogoutRequest request,
        Guid? authenticatedUserId,
        string? authenticatedClientId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<UserProfileSummaryDto> GetCurrentUserProfileAsync(
        Guid userId,
        string clientId,
        CancellationToken cancellationToken = default);

    Task ChangePasswordAsync(
        Guid userId,
        ChangePasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task RequestAccountActivationAsync(
        RequestAccountActivationRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task ConfirmAccountActivationAsync(
        ConfirmAccountActivationRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task ForgotPasswordAsync(
        ForgotPasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task ResetPasswordAsync(
        ResetPasswordRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<VerifyTokenResponse> VerifyTokenRealtimeAsync(
        string clientIdHeader,
        string apiKeyHeader,
        VerifyTokenRequest request,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<UserPermissionsResponse> GetUserPermissionsAsync(
        Guid userId,
        string targetClientId,
        CancellationToken cancellationToken = default);
}
