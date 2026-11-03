namespace AuthApi.Application.Common.Interfaces;

public interface IAuthNotificationDispatcher
{
    Task SendAccountActivationTokenAsync(
        Guid userId,
        string email,
        string fullName,
        string rawActivationToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);

    Task SendPasswordResetTokenAsync(
        Guid userId,
        string email,
        string fullName,
        string rawResetToken,
        DateTime expiresAtUtc,
        CancellationToken cancellationToken = default);
}

