using AuthApi.Application.Common.Interfaces;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Notifications;

/// <summary>
/// Implementasi default (Development Logger / Webhook-ready stub) untuk pengiriman token aktivasi akun dan reset password.
/// Dapat diganti di Dependency Injection dengan implementasi SMTP Email, WhatsApp Gateway, atau Message Broker kapan saja.
/// </summary>
public sealed class LoggingAuthNotificationDispatcher(
    ILogger<LoggingAuthNotificationDispatcher> logger) : IAuthNotificationDispatcher
{
    public Task SendAccountActivationTokenAsync(
        Guid userId,
        string email,
        string fullName,
        string rawActivationToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default)
    {
        logger.LogInformation(
            "[AUTH-NOTIFICATION][ACCOUNT-ACTIVATION] Penerima: {FullName} <{Email}> (UserId: {UserId}) | Token: {ActivationToken} | Berlaku s/d: {ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC",
            fullName,
            email,
            userId,
            rawActivationToken,
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
        logger.LogInformation(
            "[AUTH-NOTIFICATION][PASSWORD-RESET] Penerima: {FullName} <{Email}> (UserId: {UserId}) | Token: {ResetToken} | Berlaku s/d: {ExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC",
            fullName,
            email,
            userId,
            rawResetToken,
            expiresAtUtc);

        return Task.CompletedTask;
    }
}

