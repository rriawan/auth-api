using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Admin;
using AuthApi.Application.Features.Auth;
using AuthApi.Application.Features.Employees;
using AuthApi.Application.Features.Organization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AuthApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IClientApiKeyValidator, ClientApiKeyValidator>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IAdminService, AdminService>();
        services.TryAddSingleton<TimeProvider>(TimeProvider.System);
        services.AddScoped<EmployeeMutationSupport>();
        services.AddScoped<EmployeePlacementService>();
        services.AddScoped<IEmployeePlacementService>(provider => provider.GetRequiredService<EmployeePlacementService>());
        services.AddScoped<IEmployeeScheduleWriter>(provider => provider.GetRequiredService<EmployeePlacementService>());
        services.AddScoped<IEmployeeProfileService, EmployeeProfileService>();
        services.AddScoped<IEmployeeQueryService, EmployeeQueryService>();
        services.AddScoped<IOrganizationAdminService, OrganizationAdminService>();
        services.AddScoped<IOrganizationQueryService, OrganizationQueryService>();

        return services;
    }
}
