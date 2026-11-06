using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace AuthApi.Tests;

public sealed class EmployeeModelTests
{
    [Fact]
    public void EmployeeModel_UsesSqlServerTypesAndTimelineConstraints()
    {
        using var context = CreateModelOnlyContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        var employee = model.FindEntityType(typeof(Employee))!;
        Assert.Equal(50, employee.FindProperty(nameof(Employee.EmployeeNumber))!.GetMaxLength());
        Assert.Equal(200, employee.FindProperty(nameof(Employee.EmployeeFullName))!.GetMaxLength());
        Assert.Contains(employee.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual([nameof(Employee.EmployeeNumber)]));
        var rowVersion = employee.FindProperty(nameof(Employee.RowVersion))!;
        Assert.True(rowVersion.IsConcurrencyToken);
        Assert.Equal("rowversion", rowVersion.GetColumnType());

        foreach (var type in new[] { typeof(EmployeeStatus), typeof(EmployeePosition), typeof(EmployeeActingAssignment) })
        {
            var entity = model.FindEntityType(type)!;
            Assert.Equal("date", entity.FindProperty("EffectiveFrom")!.GetColumnType());
            Assert.Equal("date", entity.FindProperty("EffectiveToExclusive")!.GetColumnType());
            Assert.Contains(entity.GetCheckConstraints(), x => x.Name?.EndsWith("_EffectiveRange", StringComparison.Ordinal) == true);
            Assert.Contains(entity.GetCheckConstraints(), x =>
                x.Name?.EndsWith("_SupersededMetadata", StringComparison.Ordinal) == true &&
                x.Sql.Contains("[SupersededReason] IS NOT NULL", StringComparison.Ordinal));
            Assert.Contains(entity.GetCheckConstraints(), x => x.Name?.EndsWith("_RevisionNumber", StringComparison.Ordinal) == true);
        }

        AssertEnumStoredAsCode<EmployeeStatus>(model, nameof(EmployeeStatus.StatusCode));
        AssertEnumStoredAsCode<EmployeeStatus>(model, nameof(EmployeeStatus.RecordState));
        AssertEnumStoredAsCode<EmployeePosition>(model, nameof(EmployeePosition.RecordState));
        AssertEnumStoredAsCode<EmployeeActingAssignment>(model, nameof(EmployeeActingAssignment.AssignmentType));
        AssertEnumStoredAsCode<EmployeeActingAssignment>(model, nameof(EmployeeActingAssignment.RecordState));
    }

    [Fact]
    public void EmployeeModel_UsesRevisionKeysAndAllowsMultipleIncumbentsPerPosition()
    {
        using var context = CreateModelOnlyContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        AssertUniqueIndex(model, typeof(EmployeeStatus), nameof(EmployeeStatus.EmployeeId), nameof(EmployeeStatus.RevisionNumber));
        AssertUniqueIndex(model, typeof(EmployeePosition), nameof(EmployeePosition.AssignmentKey), nameof(EmployeePosition.RevisionNumber));
        AssertUniqueIndex(model, typeof(EmployeeActingAssignment), nameof(EmployeeActingAssignment.AssignmentKey), nameof(EmployeeActingAssignment.RevisionNumber));

        var positions = model.FindEntityType(typeof(EmployeePosition))!;
        Assert.Contains(positions.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual([nameof(EmployeePosition.AssignmentKey)]) &&
            x.GetFilter() == "[SupersededAtUtc] IS NULL");
        Assert.DoesNotContain(positions.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual([nameof(EmployeePosition.PositionId)]));
        Assert.Contains(positions.GetKeys(), key => key.Properties.Select(x => x.Name)
            .SequenceEqual([nameof(EmployeePosition.EmployeeId), nameof(EmployeePosition.Id)]));
    }

    [Fact]
    public void EmployeeModel_UsesOptionalOneToOneLinkAndSameEmployeeActingSource()
    {
        using var context = CreateModelOnlyContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        var link = model.FindEntityType(typeof(UserEmployeeLink))!;
        Assert.Equal(nameof(UserEmployeeLink.UserId), Assert.Single(link.FindPrimaryKey()!.Properties).Name);
        Assert.Contains(link.GetIndexes(), x => x.IsUnique &&
            x.Properties.Select(p => p.Name).SequenceEqual([nameof(UserEmployeeLink.EmployeeId)]));
        Assert.All(link.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));

        var acting = model.FindEntityType(typeof(EmployeeActingAssignment))!;
        Assert.Contains(acting.GetForeignKeys(), fk =>
            fk.Properties.Select(p => p.Name).SequenceEqual([
                nameof(EmployeeActingAssignment.EmployeeId), nameof(EmployeeActingAssignment.SourceEmployeePositionId)]) &&
            fk.PrincipalEntityType.ClrType == typeof(EmployeePosition) &&
            fk.PrincipalKey.Properties.Select(p => p.Name).SequenceEqual([
                nameof(EmployeePosition.EmployeeId), nameof(EmployeePosition.Id)]) &&
            fk.DeleteBehavior == DeleteBehavior.Restrict);

        foreach (var entity in model.GetEntityTypes().Where(x => x.ClrType.Namespace == typeof(Employee).Namespace))
            Assert.All(entity.GetForeignKeys(), fk => Assert.Equal(DeleteBehavior.Restrict, fk.DeleteBehavior));
    }

    private static AppDbContext CreateModelOnlyContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=AuthApiEmployeeModelMetadataOnly;Integrated Security=True;TrustServerCertificate=True")
            .Options;
        return new AppDbContext(options);
    }

    private static void AssertEnumStoredAsCode<TEntity>(IModel model, string propertyName)
    {
        var property = model.FindEntityType(typeof(TEntity))!.FindProperty(propertyName)!;
        var converter = property.GetTypeMapping().Converter;
        Assert.NotNull(converter);
        Assert.Equal(typeof(string), converter.ProviderClrType);
        Assert.False(property.IsUnicode());
        var first = Enum.GetValues(property.ClrType).GetValue(0)!;
        Assert.Equal(first.ToString()!.ToUpperInvariant(), converter.ConvertToProvider(first));
    }

    private static void AssertUniqueIndex(IModel model, Type entityType, params string[] properties) =>
        Assert.Contains(model.FindEntityType(entityType)!.GetIndexes(), index => index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(properties));
}
