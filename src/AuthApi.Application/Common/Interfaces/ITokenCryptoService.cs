namespace AuthApi.Application.Common.Interfaces;

public interface ITokenCryptoService
{
    string GenerateSecureToken(int byteLength = 32, string? prefix = null);
    (string RawApiKey, string ApiKeyHash, string ApiKeyPrefix) GenerateClientApiKey();
    string ComputeSha256Hash(string rawValue);
    bool FixedTimeEqualsHash(string hashA, string hashB);
}

