using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AuthApi.Infrastructure.Security;

public sealed class JwtTokenService(
    RsaKeyProvider rsaKeyProvider,
    IOptions<JwtSettings> jwtOptions) : IJwtTokenService
{
    private readonly JwtSettings _jwtSettings = jwtOptions.Value;

    public JwtTokenResult GenerateAccessToken(JwtTokenGenerationRequest request)
    {
        var nowUtc = DateTime.UtcNow;
        var expirationMinutes = _jwtSettings.AccessTokenExpirationMinutes > 0
            ? _jwtSettings.AccessTokenExpirationMinutes
            : 15;
        var expiresAtUtc = nowUtc.AddMinutes(expirationMinutes);
        var jwtId = Guid.NewGuid().ToString("N");

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new(JwtRegisteredClaimNames.UniqueName, request.Username),
            new(JwtRegisteredClaimNames.Email, request.Email),
            new(JwtRegisteredClaimNames.Jti, jwtId),
            new(
                JwtRegisteredClaimNames.Iat,
                EpochTime.GetIntDate(nowUtc).ToString(),
                ClaimValueTypes.Integer64)
        };

        foreach (var role in request.Roles.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("roles", role));
        }

        foreach (var permission in request.Permissions.Where(p => !string.IsNullOrWhiteSpace(p)).Distinct(StringComparer.OrdinalIgnoreCase))
        {
            claims.Add(new Claim("permissions", permission));
        }

        var token = new JwtSecurityToken(
            issuer: rsaKeyProvider.Issuer,
            audience: request.ClientId,
            claims: claims,
            notBefore: nowUtc,
            expires: expiresAtUtc,
            signingCredentials: rsaKeyProvider.GetSigningCredentials());

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.WriteToken(token);

        return new JwtTokenResult(
            AccessToken: accessToken,
            ExpiresAtUtc: expiresAtUtc,
            ExpiresInSeconds: (int)TimeSpan.FromMinutes(expirationMinutes).TotalSeconds,
            JwtId: jwtId);
    }

    public JsonWebKeySetResponse GetJsonWebKeySet() =>
        rsaKeyProvider.GetJsonWebKeySet();

    public TokenClaimsPrincipalResult ValidateAccessToken(string accessToken, string? expectedAudience = null)
    {
        if (string.IsNullOrWhiteSpace(accessToken))
        {
            return new TokenClaimsPrincipalResult(
                IsValid: false,
                UserId: null,
                Username: null,
                Email: null,
                AudienceClientId: null,
                Roles: [],
                Permissions: [],
                JwtId: null,
                ExpiresAtUtc: null,
                ErrorReason: "Access token kosong.");
        }

        var handler = new JwtSecurityTokenHandler
        {
            MapInboundClaims = false
        };

        try
        {
            var validationParameters = rsaKeyProvider.GetTokenValidationParameters(expectedAudience);
            var principal = handler.ValidateToken(accessToken, validationParameters, out var validatedToken);

            var sub = principal.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
            Guid? userId = Guid.TryParse(sub, out var parsedUserId) ? parsedUserId : null;
            var username = principal.FindFirst(JwtRegisteredClaimNames.UniqueName)?.Value;
            var email = principal.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
            var aud = principal.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
            var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
            var roles = principal.FindAll("roles").Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
            var permissions = principal.FindAll("permissions").Select(c => c.Value).Distinct(StringComparer.OrdinalIgnoreCase).ToList();

            return new TokenClaimsPrincipalResult(
                IsValid: userId.HasValue,
                UserId: userId,
                Username: username,
                Email: email,
                AudienceClientId: aud,
                Roles: roles,
                Permissions: permissions,
                JwtId: jti,
                ExpiresAtUtc: validatedToken.ValidTo,
                ErrorReason: userId.HasValue ? null : "Klaim 'sub' pada token tidak valid.");
        }
        catch (SecurityTokenExpiredException)
        {
            return new TokenClaimsPrincipalResult(
                false, null, null, null, null, [], [], null, null, "Access token telah kedaluwarsa.");
        }
        catch (SecurityTokenInvalidAudienceException)
        {
            return new TokenClaimsPrincipalResult(
                false, null, null, null, null, [], [], null, null, "Audience (clientId) pada token tidak sesuai.");
        }
        catch (Exception ex)
        {
            return new TokenClaimsPrincipalResult(
                false, null, null, null, null, [], [], null, null, $"Token tidak valid: {ex.Message}");
        }
    }
}

