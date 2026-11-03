using AuthApi.Infrastructure.Persistence;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace AuthApi.WebApi.Infrastructure;

public sealed class DatabaseHealthCheck(AppDbContext dbContext) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("MSSQL database connection is healthy.")
                : HealthCheckResult.Unhealthy("Cannot connect to MSSQL database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Exception while checking MSSQL database connectivity.", ex);
        }
    }
}

