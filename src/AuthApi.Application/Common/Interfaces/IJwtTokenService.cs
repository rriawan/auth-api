using AuthApi.Application.Common.Models;

namespace AuthApi.Application.Common.Interfaces;

public interface IJwtTokenService
{
    JwtTokenResult GenerateAccessToken(JwtTokenGenerationRequest request);
    JsonWebKeySetResponse GetJsonWebKeySet();
    TokenClaimsPrincipalResult ValidateAccessToken(string accessToken, string? expectedAudience = null);
}

