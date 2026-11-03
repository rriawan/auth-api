using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Common.Interfaces;

namespace AuthApi.Infrastructure.Security;

public sealed class TokenCryptoService : ITokenCryptoService
{
    public string GenerateSecureToken(int byteLength = 32, string? prefix = null)
    {
        if (byteLength < 16)
        {
            throw new ArgumentOutOfRangeException(nameof(byteLength), "Token byte length minimal 16 byte.");
        }

        var randomBytes = RandomNumberGenerator.GetBytes(byteLength);
        var base64Url = Convert.ToBase64String(randomBytes)
            .Replace('+', '-')
            .Replace('/', '_')
            .TrimEnd('=');

        return string.IsNullOrEmpty(prefix) ? base64Url : $"{prefix}{base64Url}";
    }

    public (string RawApiKey, string ApiKeyHash, string ApiKeyPrefix) GenerateClientApiKey()
    {
        var rawApiKey = GenerateSecureToken(32, "ak_live_");
        var apiKeyHash = ComputeSha256Hash(rawApiKey);
        var apiKeyPrefix = rawApiKey[..Math.Min(14, rawApiKey.Length)];
        return (rawApiKey, apiKeyHash, apiKeyPrefix);
    }

    public string ComputeSha256Hash(string rawValue)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(rawValue);
        var bytes = Encoding.UTF8.GetBytes(rawValue);
        var hashBytes = SHA256.HashData(bytes);
        return Convert.ToHexStringLower(hashBytes);
    }

    public bool FixedTimeEqualsHash(string hashA, string hashB)
    {
        if (string.IsNullOrWhiteSpace(hashA) || string.IsNullOrWhiteSpace(hashB))
        {
            return false;
        }

        var bytesA = Encoding.UTF8.GetBytes(hashA.Trim().ToLowerInvariant());
        var bytesB = Encoding.UTF8.GetBytes(hashB.Trim().ToLowerInvariant());
        return CryptographicOperations.FixedTimeEquals(bytesA, bytesB);
    }
}

