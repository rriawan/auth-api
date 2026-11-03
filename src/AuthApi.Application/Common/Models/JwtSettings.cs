namespace AuthApi.Application.Common.Models;

public sealed class JwtSettings
{
    public const string SectionName = "JwtSettings";

    public string Issuer { get; set; } = "https://auth.internal.local";
    public int AccessTokenExpirationMinutes { get; set; } = 15;
    public int RefreshTokenExpirationDays { get; set; } = 14;
    public string KeyId { get; set; } = "auth-api-rsa-key-1";
    public string? RsaPrivateKeyPem { get; set; }
}

