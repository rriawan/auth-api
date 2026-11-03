using AuthApi.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;

namespace AuthApi.WebApi.Infrastructure;

public sealed class JwtBearerOptionsSetup(RsaKeyProvider rsaKeyProvider)
    : IConfigureNamedOptions<JwtBearerOptions>
{
    public void Configure(string? name, JwtBearerOptions options)
    {
        if (name is null || name == JwtBearerDefaults.AuthenticationScheme)
        {
            Configure(options);
        }
    }

    public void Configure(JwtBearerOptions options)
    {
        options.MapInboundClaims = false;
        options.RequireHttpsMetadata = false;
        options.SaveToken = false;
        options.TokenValidationParameters = rsaKeyProvider.GetTokenValidationParameters(validAudience: null);
    }
}

