using System.IdentityModel.Tokens.Jwt;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Domain.Constants;
using AuthApi.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthApi.WebApi.Infrastructure;

/// <summary>
/// Employee/organization administration accepts only an authenticated Bearer JWT issued for 'auth-admin'.
/// The service-API-key path used by older admin endpoints is deliberately not accepted (planning §8.3).
/// This filter checks the token's audience/subject only; administrator rights are re-verified against the
/// database by the application services on every call, never taken from JWT claims.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
public sealed class RequireAdminBearerAttribute : TypeFilterAttribute
{
    public RequireAdminBearerAttribute() : base(typeof(AdminBearerOnlyFilter)) { }
}

public sealed class AdminBearerOnlyFilter : IAuthorizationFilter
{
    public void OnAuthorization(AuthorizationFilterContext context)
    {
        var request = context.HttpContext.Request;
        if (request.Headers.ContainsKey(AuthController.ApiKeyHeaderName))
            throw new ForbiddenAccessException("Endpoint ini hanya menerima Bearer JWT administrator, bukan API Key.");

        var user = context.HttpContext.User;
        if (user.Identity?.IsAuthenticated != true)
            throw new UnauthorizedAccessException("Autentikasi diperlukan: sertakan Bearer JWT 'auth-admin'.");

        var audience = user.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
        if (!string.Equals(audience, SystemConstants.DefaultAdminClientId, StringComparison.OrdinalIgnoreCase))
            throw new ForbiddenAccessException($"Token ini bukan untuk aplikasi '{SystemConstants.DefaultAdminClientId}'.");

        if (!Guid.TryParse(user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value, out _))
            throw new UnauthorizedAccessException("Token tidak memuat subject pengguna yang valid.");
    }
}
