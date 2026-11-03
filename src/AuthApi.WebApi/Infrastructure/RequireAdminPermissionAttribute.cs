using System.IdentityModel.Tokens.Jwt;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Constants;
using AuthApi.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AuthApi.WebApi.Infrastructure;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequireAdminPermissionAttribute : TypeFilterAttribute
{
    public RequireAdminPermissionAttribute(string permissionCode)
        : base(typeof(AdminPermissionAuthorizationFilter))
    {
        Arguments = [permissionCode];
    }
}

public sealed class AdminPermissionAuthorizationFilter(
    string requiredPermission,
    IClientApiKeyValidator clientApiKeyValidator) : IAsyncAuthorizationFilter
{
    public async Task OnAuthorizationAsync(AuthorizationFilterContext context)
    {
        var httpContext = context.HttpContext;

        // Jalur 1: Service API Key untuk 'auth-admin' (X-Client-Id + X-Api-Key)
        var clientIdHeader = httpContext.Request.Headers[AuthController.ClientIdHeaderName].FirstOrDefault();
        var apiKeyHeader = httpContext.Request.Headers[AuthController.ApiKeyHeaderName].FirstOrDefault();

        if (!string.IsNullOrWhiteSpace(clientIdHeader) || !string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            var clientApp = await clientApiKeyValidator.ValidateAsync(
                clientIdHeader,
                apiKeyHeader,
                httpContext.RequestAborted);

            if (!string.Equals(clientApp.ClientId, SystemConstants.DefaultAdminClientId, StringComparison.OrdinalIgnoreCase))
            {
                throw new ForbiddenAccessException(
                    $"Hanya API Key milik aplikasi '{SystemConstants.DefaultAdminClientId}' yang diizinkan mengakses endpoint administrasi.");
            }

            return;
        }

        // Jalur 2: Bearer JWT yang diterbitkan untuk aplikasi 'auth-admin'
        var user = httpContext.User;
        if (user.Identity?.IsAuthenticated != true)
        {
            throw new UnauthorizedAccessException(
                "Autentikasi diperlukan: sertakan Bearer JWT 'auth-admin' atau pasangan header 'X-Client-Id' dan 'X-Api-Key'.");
        }

        var audience = user.FindFirst(JwtRegisteredClaimNames.Aud)?.Value;
        if (!string.Equals(audience, SystemConstants.DefaultAdminClientId, StringComparison.OrdinalIgnoreCase))
        {
            throw new ForbiddenAccessException(
                $"Token ini diterbitkan untuk aplikasi '{audience}', bukan untuk '{SystemConstants.DefaultAdminClientId}'.");
        }

        var isSuperAdmin = user.FindAll("roles")
            .Any(c => string.Equals(c.Value, SystemConstants.DefaultSuperAdminRole, StringComparison.OrdinalIgnoreCase));

        if (isSuperAdmin)
        {
            return;
        }

        var hasPermission = user.FindAll("permissions")
            .Any(c => string.Equals(c.Value, requiredPermission, StringComparison.OrdinalIgnoreCase));

        if (!hasPermission)
        {
            throw new ForbiddenAccessException(
                $"Akses ditolak: membutuhkan permission '{requiredPermission}' pada aplikasi '{SystemConstants.DefaultAdminClientId}'.");
        }
    }
}

