using AuthApi.Domain.Entities;

namespace AuthApi.Application.Common.Interfaces;

public interface IClientApiKeyValidator
{
    Task<ClientApp> ValidateAsync(
        string? clientId,
        string? rawApiKey,
        CancellationToken cancellationToken = default);
}

