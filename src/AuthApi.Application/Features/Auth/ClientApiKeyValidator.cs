using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Auth;

public sealed class ClientApiKeyValidator(
    IAppDbContext dbContext,
    ITokenCryptoService tokenCryptoService) : IClientApiKeyValidator
{
    public async Task<ClientApp> ValidateAsync(
        string? clientId,
        string? rawApiKey,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(rawApiKey))
        {
            throw new UnauthorizedAccessException(
                "Header 'X-Client-Id' dan 'X-Api-Key' wajib disertakan untuk autentikasi layanan aplikasi.");
        }

        var normalizedClientId = clientId.Trim();
        var clientApp = await dbContext.ClientApps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.ClientId == normalizedClientId, cancellationToken);

        if (clientApp is null)
        {
            throw new UnauthorizedAccessException("Client ID atau API Key tidak valid.");
        }

        var computedHash = tokenCryptoService.ComputeSha256Hash(rawApiKey.Trim());
        var isHashValid = tokenCryptoService.FixedTimeEqualsHash(computedHash, clientApp.ApiKeyHash);

        if (!isHashValid)
        {
            throw new UnauthorizedAccessException("Client ID atau API Key tidak valid.");
        }

        if (!clientApp.IsActive)
        {
            throw new ForbiddenAccessException($"Aplikasi klien '{clientApp.ClientId}' sedang dinonaktifkan.");
        }

        return clientApp;
    }
}

