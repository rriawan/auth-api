using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace AuthApi.WebApi.Infrastructure;

/// <summary>
/// Constructs a context without running the Web API startup, RSA provider,
/// bootstrap seeder, or health checks. Offline mode also skips configuration.
/// Context construction itself never opens a database connection.
/// </summary>
public sealed class AppDbContextDesignTimeFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var offline = args.Contains("--offline-migrations", StringComparer.Ordinal);
        var connectionString = offline
            ? "Server=127.0.0.1;Database=AuthApiDesignTime;Integrated Security=True;Encrypt=True"
            : ReadConnectionString(args);

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. Configure it in the Web API " +
                "settings, Development user-secrets, or ConnectionStrings__DefaultConnection. " +
                "Use --offline-migrations only when generating or inspecting migration files.");
        }

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                connectionString,
                sql => sql.MigrationsAssembly(typeof(AppDbContext).Assembly.FullName))
            .Options;

        return new AppDbContext(options);
    }

    private static string? ReadConnectionString(string[] args)
    {
        var environment = GetOption(args, "--environment")
            ?? Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT")
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Development";

        if (string.IsNullOrWhiteSpace(environment) || environment.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            throw new ArgumentException("Provide a valid environment name.");

        var contentRoot = GetOption(args, "--content-root") ?? FindWebApiDirectory();
        var builder = new ConfigurationBuilder()
            .SetBasePath(Path.GetFullPath(contentRoot))
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false);

        if (string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase))
            builder.AddUserSecrets(typeof(Program).Assembly, optional: true, reloadOnChange: false);

        // Match normal configuration priority without building or starting a host.
        var configuration = builder.AddEnvironmentVariables().Build();
        try
        {
            return configuration.GetConnectionString("DefaultConnection");
        }
        finally
        {
            (configuration as IDisposable)?.Dispose();
        }
    }

    private static string? GetOption(string[] args, string name)
    {
        for (var index = 0; index < args.Length; index++)
        {
            if (args[index].StartsWith(name + "=", StringComparison.Ordinal))
            {
                var value = args[index][(name.Length + 1)..];
                if (!string.IsNullOrWhiteSpace(value)) return value;
                throw new ArgumentException($"Provide a value for {name}.");
            }

            if (args[index] != name) continue;
            if (index + 1 < args.Length && !args[index + 1].StartsWith("--", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(args[index + 1]))
                return args[index + 1];
            throw new ArgumentException($"Provide a value for {name}.");
        }

        return null;
    }

    private static string FindWebApiDirectory()
    {
        foreach (var startPath in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var directory = new DirectoryInfo(startPath); directory is not null; directory = directory.Parent)
            {
                if (File.Exists(Path.Combine(directory.FullName, "AuthApi.WebApi.csproj")))
                    return directory.FullName;

                var candidate = Path.Combine(directory.FullName, "src", "AuthApi.WebApi");
                if (File.Exists(Path.Combine(candidate, "AuthApi.WebApi.csproj")))
                    return candidate;
            }
        }

        throw new InvalidOperationException(
            "Cannot locate AuthApi.WebApi configuration. Pass --content-root with its directory.");
    }
}
