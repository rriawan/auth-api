using Microsoft.Extensions.DependencyInjection;

namespace AuthApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        return services;
    }
}

