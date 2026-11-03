using AuthApi.Application.Common.Models;
using AuthApi.Domain.Entities;
using AuthApi.Infrastructure.Security;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace AuthApi.Tests;

public class SecurityCryptoAndJwtTests
{
    [Fact]
    public void PasswordHasherService_ShouldHashAndVerifyPassword_Correctly()
    {
        var hasher = new PasswordHasherService();
        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = "budi",
            Email = "budi@example.com"
        };

        var hash = hasher.HashPassword(user, "RahasiaKuat!2026");

        Assert.False(string.IsNullOrWhiteSpace(hash));
        Assert.NotEqual("RahasiaKuat!2026", hash);
        Assert.True(hasher.VerifyPassword(user, hash, "RahasiaKuat!2026", out var needsRehash));
        Assert.False(needsRehash);
        Assert.False(hasher.VerifyPassword(user, hash, "PasswordSalah!2026", out _));
    }

    [Fact]
    public void TokenCryptoService_ShouldGenerateUniqueTokens_AndVerifyFixedTimeSha256Hash()
    {
        var crypto = new TokenCryptoService();

        var token1 = crypto.GenerateSecureToken(32, "rt_");
        var token2 = crypto.GenerateSecureToken(32, "rt_");

        Assert.StartsWith("rt_", token1);
        Assert.NotEqual(token1, token2);

        var hash1 = crypto.ComputeSha256Hash(token1);
        var hash1Again = crypto.ComputeSha256Hash(token1);
        var hash2 = crypto.ComputeSha256Hash(token2);

        Assert.Equal(64, hash1.Length);
        Assert.True(crypto.FixedTimeEqualsHash(hash1, hash1Again));
        Assert.False(crypto.FixedTimeEqualsHash(hash1, hash2));

        var (rawApiKey, apiKeyHash, apiKeyPrefix) = crypto.GenerateClientApiKey();
        Assert.StartsWith("ak_live_", rawApiKey);
        Assert.StartsWith(apiKeyPrefix, rawApiKey);
        Assert.Equal(crypto.ComputeSha256Hash(rawApiKey), apiKeyHash);
    }

    [Fact]
    public void JwtTokenService_ShouldGenerateValidRs256Token_ExposeJwks_AndEnforceAudienceIsolation()
    {
        var jwtOptions = Options.Create(new JwtSettings
        {
            Issuer = "https://auth.unit-test.local",
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 14,
            KeyId = "test-rsa-kid-01"
        });

        using var rsaKeyProvider = new RsaKeyProvider(jwtOptions, NullLogger<RsaKeyProvider>.Instance);
        var jwtService = new JwtTokenService(rsaKeyProvider, jwtOptions);

        // 1. Verifikasi struktur JWKS
        var jwks = jwtService.GetJsonWebKeySet();
        Assert.Single(jwks.Keys);
        Assert.Equal("RSA", jwks.Keys[0].Kty);
        Assert.Equal("sig", jwks.Keys[0].Use);
        Assert.Equal("RS256", jwks.Keys[0].Alg);
        Assert.Equal("test-rsa-kid-01", jwks.Keys[0].Kid);
        Assert.False(string.IsNullOrWhiteSpace(jwks.Keys[0].N));
        Assert.False(string.IsNullOrWhiteSpace(jwks.Keys[0].E));

        // 2. Buat Access Token untuk aplikasi 'erp-core'
        var userId = Guid.NewGuid();
        var tokenResult = jwtService.GenerateAccessToken(new JwtTokenGenerationRequest(
            UserId: userId,
            Username: "andi",
            Email: "andi@example.com",
            FullName: "Andi Saputra",
            ClientId: "erp-core",
            Roles: ["FinanceStaff"],
            Permissions: ["invoice.create", "invoice.view"]));

        Assert.False(string.IsNullOrWhiteSpace(tokenResult.AccessToken));
        Assert.Equal(900, tokenResult.ExpiresInSeconds);

        // 3. Validasi token untuk audience yang benar ('erp-core')
        var validClaimResult = jwtService.ValidateAccessToken(tokenResult.AccessToken, expectedAudience: "erp-core");
        Assert.True(validClaimResult.IsValid);
        Assert.Equal(userId, validClaimResult.UserId);
        Assert.Equal("andi", validClaimResult.Username);
        Assert.Equal("erp-core", validClaimResult.AudienceClientId);
        Assert.Contains("FinanceStaff", validClaimResult.Roles);
        Assert.Contains("invoice.create", validClaimResult.Permissions);

        // 4. Pastikan ditolak jika divalidasi oleh aplikasi lain ('hris-app')
        var wrongAudienceResult = jwtService.ValidateAccessToken(tokenResult.AccessToken, expectedAudience: "hris-app");
        Assert.False(wrongAudienceResult.IsValid);
    }
}

