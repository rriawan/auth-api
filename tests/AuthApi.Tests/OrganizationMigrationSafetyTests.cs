using AuthApi.Infrastructure.Persistence;
using AuthApi.WebApi.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Xunit;

namespace AuthApi.Tests;

public sealed class OrganizationMigrationSafetyTests
{
    [Fact]
    public void OfflineFactory_OnlyConstructsMetadataAndKeepsConnectionClosed()
    {
        using var context = new AppDbContextDesignTimeFactory()
            .CreateDbContext(["--offline-migrations"]);

        Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", context.Database.ProviderName);
        Assert.Equal(System.Data.ConnectionState.Closed, context.Database.GetDbConnection().State);
        Assert.Equal("AuthApiDesignTime", context.Database.GetDbConnection().Database);
        Assert.NotNull(context.Model.FindEntityType("AuthApi.Domain.Entities.User"));
    }

    [Fact]
    public void OrganizationMigration_IsAdditiveAndDoesNotModifyAuthenticationTables()
    {
        using var context = new AppDbContextDesignTimeFactory()
            .CreateDbContext(["--offline-migrations"]);
        var assembly = context.GetService<IMigrationsAssembly>();
        var migrationEntry = Assert.Single(assembly.Migrations,
            x => x.Key.EndsWith("_AddOrganizationMaster", StringComparison.Ordinal));
        var migration = assembly.CreateMigration(migrationEntry.Value, context.Database.ProviderName!);

        var expectedTables = new HashSet<string>(StringComparer.Ordinal)
        {
            "OfficeClassifications", "Offices", "OfficeVersions", "OrgUnits", "OrgUnitVersions",
            "PositionLevels", "Positions", "PositionVersions", "StructureTemplates",
            "TemplateOrgUnits", "TemplatePositions", "OfficeTemplateApplications"
        };

        var createdTables = migration.UpOperations.OfType<CreateTableOperation>().ToArray();
        Assert.Equal(expectedTables.Count, createdTables.Length);
        Assert.True(expectedTables.SetEquals(createdTables.Select(x => x.Name)));
        Assert.All(migration.UpOperations, operation =>
        {
            Assert.True(operation is CreateTableOperation or CreateIndexOperation,
                $"Unexpected migration operation: {operation.GetType().Name}");
            if (operation is CreateIndexOperation index)
                Assert.Contains(index.Table, expectedTables);
        });

        Assert.All(migration.DownOperations, operation =>
        {
            var drop = Assert.IsType<DropTableOperation>(operation);
            Assert.Contains(drop.Name, expectedTables);
        });
    }

    [Fact]
    public void EmployeeFoundationMigration_OnlyAddsEmployeeTablesAndIndexes()
    {
        using var context = new AppDbContextDesignTimeFactory().CreateDbContext(["--offline-migrations"]);
        var assembly = context.GetService<IMigrationsAssembly>();
        var entry = Assert.Single(assembly.Migrations,
            x => x.Key.EndsWith("_AddEmployeeProfileFoundation", StringComparison.Ordinal));
        var migration = assembly.CreateMigration(entry.Value, context.Database.ProviderName!);
        var tables = new HashSet<string>(StringComparer.Ordinal)
        {
            "Employees", "EmployeeStatuses", "EmployeePositions", "EmployeeActingAssignments", "UserEmployeeLinks"
        };

        Assert.True(tables.SetEquals(migration.UpOperations.OfType<CreateTableOperation>().Select(x => x.Name)));
        Assert.All(migration.UpOperations, operation =>
        {
            Assert.True(operation is CreateTableOperation or CreateIndexOperation,
                $"Unexpected employee migration operation: {operation.GetType().Name}");
            if (operation is CreateIndexOperation index) Assert.Contains(index.Table, tables);
        });
        Assert.All(migration.DownOperations, operation => Assert.Contains(Assert.IsType<DropTableOperation>(operation).Name, tables));
    }
}
