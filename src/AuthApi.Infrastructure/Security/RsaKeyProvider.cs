using System.Security.Cryptography;
using AuthApi.Application.Common.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security;

public sealed class RsaKeyProvider : IDisposable
{
    private readonly RSA _rsa;
    private readonly RSAParameters _publicParameters;
    private readonly JwtSettings _jwtSettings;

    public RsaKeyProvider(IOptions<JwtSettings> jwtOptions, ILogger<RsaKeyProvider> logger)
    {
        _jwtSettings = jwtOptions.Value;
        _rsa = RSA.Create(2048);

        if (!string.IsNullOrWhiteSpace(_jwtSettings.RsaPrivateKeyPem))
        {
            var normalizedPem = _jwtSettings.RsaPrivateKeyPem.Replace("\\n", "\n");
            _rsa.ImportFromPem(normalizedPem);
            logger.LogInformation("RSA Signing Key berhasil dimuat dari konfigurasi (KeyId: {KeyId}).", KeyId);
        }
        else
        {
            var devKeyFilePath = Path.Combine(AppContext.BaseDirectory, "dev-rsa-private.key");
            if (File.Exists(devKeyFilePath))
            {
                var pem = File.ReadAllText(devKeyFilePath);
                _rsa.ImportFromPem(pem);
                logger.LogInformation(
                    "RSA Signing Key Development dimuat dari file lokal '{KeyFile}' (KeyId: {KeyId}).",
                    devKeyFilePath,
                    KeyId);
            }
            else
            {
                var pem = _rsa.ExportRSAPrivateKeyPem();
                try
                {
                    File.WriteAllText(devKeyFilePath, pem);
                    logger.LogWarning(
                        "JwtSettings:RsaPrivateKeyPem belum diatur. Kunci RSA 2048-bit baru telah dibuat untuk Development di '{KeyFile}' (KeyId: {KeyId}). Pastikan menggunakan Environment Variable / User Secrets di server Production.",
                        devKeyFilePath,
                        KeyId);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(
                        ex,
                        "Kunci RSA 2048-bit ephemeral in-memory digunakan karena gagal menyimpan file lokal (KeyId: {KeyId}).",
                        KeyId);
                }
            }
        }

        _publicParameters = _rsa.ExportParameters(includePrivateParameters: false);
    }

    public string KeyId => string.IsNullOrWhiteSpace(_jwtSettings.KeyId)
        ? "auth-api-rsa-key-1"
        : _jwtSettings.KeyId;

    public string Issuer => string.IsNullOrWhiteSpace(_jwtSettings.Issuer)
        ? "https://auth.internal.local"
        : _jwtSettings.Issuer;

    public SigningCredentials GetSigningCredentials()
    {
        var securityKey = new RsaSecurityKey(_rsa)
        {
            KeyId = KeyId,
            CryptoProviderFactory = new CryptoProviderFactory
            {
                CacheSignatureProviders = false
            }
        };

        return new SigningCredentials(securityKey, SecurityAlgorithms.RsaSha256);
    }

    public RsaSecurityKey GetPublicValidationKey() =>
        new(_publicParameters)
        {
            KeyId = KeyId
        };

    public TokenValidationParameters GetTokenValidationParameters(string? validAudience = null)
    {
        var validateAudience = !string.IsNullOrWhiteSpace(validAudience);

        return new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = GetPublicValidationKey(),
            ValidateIssuer = true,
            ValidIssuer = Issuer,
            ValidateAudience = validateAudience,
            ValidAudience = validAudience,
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromSeconds(30),
            NameClaimType = "unique_name",
            RoleClaimType = "roles"
        };
    }

    public JsonWebKeySetResponse GetJsonWebKeySet()
    {
        var n = Base64UrlEncoder.Encode(_publicParameters.Modulus!);
        var e = Base64UrlEncoder.Encode(_publicParameters.Exponent!);

        var jwk = new JsonWebKeyDto(
            Kty: "RSA",
            Use: "sig",
            Alg: SecurityAlgorithms.RsaSha256,
            Kid: KeyId,
            N: n,
            E: e);

        return new JsonWebKeySetResponse([jwk]);
    }

    public void Dispose()
    {
        _rsa.Dispose();
    }
}

