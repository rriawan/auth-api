using AuthApi.WebApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace AuthApi.Tests;

[CollectionDefinition("Design-time configuration", DisableParallelization = true)]
public sealed class DesignTimeConfigurationCollection;

[Collection("Design-time configuration")]
public sealed class DesignTimeConfigurationTests
{
    [Fact]
    public void Factory_LoadsEnvironmentSettingsWithoutStartingApplicationOrConnecting()
    {
        WithSettings(null, directory =>
        {
            using var context = new AppDbContextDesignTimeFactory()
                .CreateDbContext(["--content-root", directory, "--environment", "ConfigurationTest"]);

            Assert.Equal("EnvironmentSettingsMetadataOnly", context.Database.GetDbConnection().Database);
            Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
        });
    }

    [Fact]
    public void Factory_EnvironmentVariableOverridesJsonSettings()
    {
        WithSettings("Server=127.0.0.1,1;Database=EnvironmentVariableMetadataOnly;Integrated Security=True", directory =>
        {
            using var context = new AppDbContextDesignTimeFactory()
                .CreateDbContext(["--content-root=" + directory, "--environment=ConfigurationTest"]);

            Assert.Equal("EnvironmentVariableMetadataOnly", context.Database.GetDbConnection().Database);
            Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
        });
    }

    [Fact]
    public void Factory_OfflineModeSkipsConfigurationEvenWithNonexistentContentRoot()
    {
        using var context = new AppDbContextDesignTimeFactory().CreateDbContext(
            ["--offline-migrations", "--content-root", Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"))]);

        Assert.Equal("AuthApiDesignTime", context.Database.GetDbConnection().Database);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
    }

    [Fact]
    public void Factory_RejectsMissingEnvironmentArgument()
    {
        Assert.Throws<ArgumentException>(() => new AppDbContextDesignTimeFactory()
            .CreateDbContext(["--environment"]));
    }

    private static void WithSettings(string? connectionOverride, Action<string> verify)
    {
        var directory = Path.Combine(Path.GetTempPath(), "auth-api-config-tests-" + Guid.NewGuid().ToString("N"));
        var previousConnection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        Directory.CreateDirectory(directory);
        try
        {
            File.WriteAllText(Path.Combine(directory, "appsettings.json"),
                """{"ConnectionStrings":{"DefaultConnection":"Server=127.0.0.1,1;Database=BaseSettingsMetadataOnly;Integrated Security=True"}}""");
            File.WriteAllText(Path.Combine(directory, "appsettings.ConfigurationTest.json"),
                """{"ConnectionStrings":{"DefaultConnection":"Server=127.0.0.1,1;Database=EnvironmentSettingsMetadataOnly;Integrated Security=True"}}""");
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", connectionOverride);
            verify(directory);
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__DefaultConnection", previousConnection);
            // Remove only the two fixture files and empty directory created by this test.
            File.Delete(Path.Combine(directory, "appsettings.ConfigurationTest.json"));
            File.Delete(Path.Combine(directory, "appsettings.json"));
            Directory.Delete(directory);
        }
    }
}
