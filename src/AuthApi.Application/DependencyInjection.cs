using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Admin;
using AuthApi.Application.Features.Auth;
using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClientApiKeyValidator, ClientApiKeyValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();

        return services;
    }
}
