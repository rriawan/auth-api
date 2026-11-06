using AuthApi.Domain.Entities.Organization;
using AuthApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Xunit;

namespace AuthApi.Tests;

public sealed class OrganizationModelTests
{
    [Fact]
    public void OrganizationModel_UsesSqlServerTypesAndRequiredConstraints()
    {
        using var context = CreateModelOnlyContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        var office = model.FindEntityType(typeof(Office))!;
        Assert.Equal(3, office.FindProperty(nameof(Office.OfficeCode))!.GetMaxLength());
        Assert.Equal(false, office.FindProperty(nameof(Office.OfficeCode))!.IsUnicode());
        Assert.Contains(office.GetCheckConstraints(), x =>
            x.Name == "CK_Offices_OfficeCode_ThreeDigits" && (x.Sql ?? "").Contains("DATALENGTH", StringComparison.Ordinal));
        Assert.Contains(office.GetIndexes(), x =>
            x.IsUnique && x.Properties.Select(p => p.Name).SequenceEqual([nameof(Office.OfficeCode)]));

        foreach (var versionType in new[] { typeof(OfficeVersion), typeof(OrgUnitVersion), typeof(PositionVersion) })
        {
            var effectiveFrom = model.FindEntityType(versionType)!.FindProperty("EffectiveFrom")!;
            Assert.Equal("date", effectiveFrom.GetColumnType());
            Assert.Contains(model.FindEntityType(versionType)!.GetCheckConstraints(), x =>
                x.Name is not null && x.Name.EndsWith("_EffectiveRange", StringComparison.Ordinal));
            Assert.Contains(model.FindEntityType(versionType)!.GetCheckConstraints(), x =>
                x.Name is not null && x.Name.EndsWith("_SupersededMetadata", StringComparison.Ordinal));
        }

        var positionVersion = model.FindEntityType(typeof(PositionVersion))!;
        Assert.Contains(positionVersion.GetCheckConstraints(), x =>
            x.Name == "CK_PositionVersions_MaxIncumbents" && (x.Sql ?? "").Contains("> 0", StringComparison.Ordinal));
        Assert.Contains(positionVersion.GetCheckConstraints(), x =>
            x.Name == "CK_PositionVersions_NotSelfSupervisor");
    }

    [Fact]
    public void OrganizationModel_UsesConcurrencyTokensAndStringCodedEnums()
    {
        using var context = CreateModelOnlyContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        foreach (var identityType in new[] { typeof(Office), typeof(OrgUnit), typeof(Position), typeof(StructureTemplate) })
        {
            var rowVersion = model.FindEntityType(identityType)!.FindProperty("RowVersion")!;
            Assert.True(rowVersion.IsConcurrencyToken);
            Assert.Equal(ValueGenerated.OnAddOrUpdate, rowVersion.ValueGenerated);
            Assert.Equal("rowversion", rowVersion.GetColumnType());
        }

        AssertEnumStoredAsCode<OrgUnitVersion>(model, nameof(OrgUnitVersion.UnitType));
        AssertEnumStoredAsCode<TemplateOrgUnit>(model, nameof(TemplateOrgUnit.UnitType));
        AssertEnumStoredAsCode<PositionVersion>(model, nameof(PositionVersion.Track));
        AssertEnumStoredAsCode<TemplatePosition>(model, nameof(TemplatePosition.Track));
        AssertEnumStoredAsCode<StructureTemplate>(model, nameof(StructureTemplate.State));
    }

    [Fact]
    public void OrganizationModel_UsesScopedUniqueKeysAndRestrictiveHistoryRelationships()
    {
        using var context = CreateModelOnlyContext();
        var model = context.GetService<IDesignTimeModel>().Model;

        AssertUniqueIndex(model, typeof(OrgUnit), nameof(OrgUnit.OfficeId), nameof(OrgUnit.UnitCode));
        AssertUniqueIndex(model, typeof(Position), nameof(Position.OrgUnitId), nameof(Position.PositionCode));
        AssertUniqueIndex(model, typeof(StructureTemplate), nameof(StructureTemplate.ClassificationCode), nameof(StructureTemplate.Version));
        AssertUniqueIndex(model, typeof(TemplateOrgUnit), nameof(TemplateOrgUnit.TemplateId), nameof(TemplateOrgUnit.UnitCode));
        AssertUniqueIndex(model, typeof(TemplatePosition), nameof(TemplatePosition.TemplateId), nameof(TemplatePosition.PositionCode));
        AssertUniqueIndex(model, typeof(OfficeTemplateApplication), nameof(OfficeTemplateApplication.OfficeId), nameof(OfficeTemplateApplication.IdempotencyKey));

        foreach (var entityType in model.GetEntityTypes().Where(x => x.ClrType.Namespace == typeof(Office).Namespace))
        {
            Assert.All(entityType.GetForeignKeys(), foreignKey =>
                Assert.Equal(DeleteBehavior.Restrict, foreignKey.DeleteBehavior));
        }

        var templateUnit = model.FindEntityType(typeof(TemplateOrgUnit))!;
        Assert.Contains(templateUnit.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(x => x.Name).SequenceEqual(
                [nameof(TemplateOrgUnit.TemplateId), nameof(TemplateOrgUnit.ParentTemplateOrgUnitId)]) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);

        var templatePosition = model.FindEntityType(typeof(TemplatePosition))!;
        Assert.Contains(templatePosition.GetForeignKeys(), foreignKey =>
            foreignKey.Properties.Select(x => x.Name).SequenceEqual(
                [nameof(TemplatePosition.TemplateId), nameof(TemplatePosition.ReportsToTemplatePositionId)]) &&
            foreignKey.DeleteBehavior == DeleteBehavior.Restrict);
    }

    private static AppDbContext CreateModelOnlyContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer("Server=127.0.0.1,1;Database=AuthApiModelMetadataOnly;Integrated Security=True;TrustServerCertificate=True")
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

        var firstEnumValue = Enum.GetValues(property.ClrType).GetValue(0);
        Assert.NotNull(firstEnumValue);
        Assert.Equal(firstEnumValue.ToString()!.ToUpperInvariant(), converter.ConvertToProvider(firstEnumValue));
    }

    private static void AssertUniqueIndex(IModel model, Type entityType, params string[] propertyNames)
    {
        Assert.Contains(model.FindEntityType(entityType)!.GetIndexes(), index =>
            index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual(propertyNames));
    }
}
