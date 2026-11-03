using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AuthApi.Infrastructure.Notifications;

/// <summary>
/// Implementasi default (Development Logger / Webhook-ready stub) untuk pengiriman token aktivasi akun dan reset password.
/// Dapat diganti di Dependency Injection dengan implementasi SMTP Email, WhatsApp Gateway, atau Message Broker kapan saja.
/// </summary>
public sealed class LoggingAuthNotificationDispatcher(
    ILogger<LoggingAuthNotificationDispatcher> logger,
    IOptions<NotificationSettings> notificationOptions) : IAuthNotificationDispatcher
{
    private readonly NotificationSettings _settings = notificationOptions.Value;

    public Task SendAccountActivationTokenAsync(
        Guid userId,
        string email,
        string fullName,
        string rawActivationToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        var portalBaseUrl = string.IsNullOrWhiteSpace(_settings.PortalBaseUrl)
            ? "http://localhost:3000"
            : _settings.PortalBaseUrl.TrimEnd('/');

        var activationLink = $"{portalBaseUrl}/auth/activate-account/confirm?token={Uri.EscapeDataString(rawActivationToken)}";

        logger.LogInformation(
            "[AUTH-NOTIFICATION][ACCOUNT-ACTIVATION] Penerima: {FullName} <{Email}> (UserId: {UserId}) | Token: {ActivationToken} | Link: {ActivationLink} | Berlaku s/d: {ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC",
            fullName,
            email,
            userId,
            rawActivationToken,
            activationLink,
            expiresAtUtc);

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
        var portalBaseUrl = string.IsNullOrWhiteSpace(_settings.PortalBaseUrl)
            ? "http://localhost:3000"
            : _settings.PortalBaseUrl.TrimEnd('/');

        var resetLink = $"{portalBaseUrl}/auth/reset-password?token={Uri.EscapeDataString(rawResetToken)}";

        logger.LogInformation(
            "[AUTH-NOTIFICATION][PASSWORD-RESET] Penerima: {FullName} <{Email}> (UserId: {UserId}) | Token: {ResetToken} | Link: {ResetLink} | Berlaku s/d: {ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC",
            fullName,
            email,
            userId,
            rawResetToken,
            resetLink,
            expiresAtUtc);

        return Task.CompletedTask;
    }
}
