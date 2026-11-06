using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Organization;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations.Organization;

public sealed class OfficeClassificationConfiguration : IEntityTypeConfiguration<OfficeClassification>
{
    public void Configure(EntityTypeBuilder<OfficeClassification> builder)
    {
        builder.ToTable("OfficeClassifications");
        builder.HasKey(x => x.Code);
        builder.Property(x => x.Code).HasMaxLength(20).IsRequired();
        builder.Property(x => x.DisplayName).HasMaxLength(100).IsRequired();
    }
}

public sealed class OfficeConfiguration : IEntityTypeConfiguration<Office>
{
    public void Configure(EntityTypeBuilder<Office> builder)
    {
        builder.ToTable("Offices", table => table.HasCheckConstraint(
            "CK_Offices_OfficeCode_ThreeDigits",
            "DATALENGTH([OfficeCode]) = 3 AND [OfficeCode] COLLATE Latin1_General_100_BIN2 NOT LIKE '%[^0-9]%'"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.OfficeCode).HasMaxLength(3).IsUnicode(false).IsRequired();
        builder.HasIndex(x => x.OfficeCode).IsUnique().HasDatabaseName("UX_Offices_OfficeCode");
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Versions)
            .WithOne(x => x.Office)
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.OrgUnits)
            .WithOne(x => x.Office)
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OfficeVersionConfiguration : IEntityTypeConfiguration<OfficeVersion>
{
    public void Configure(EntityTypeBuilder<OfficeVersion> builder)
    {
        builder.ToTable("OfficeVersions", table =>
        {
            table.HasCheckConstraint("CK_OfficeVersions_EffectiveRange",
                "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_OfficeVersions_RevisionNumber", "[RevisionNumber] > 0");
            table.HasCheckConstraint("CK_OfficeVersions_SupersededMetadata",
                SupersededMetadataCheck("SupersededAtUtc", "SupersededByUserId", "SupersededReason"));
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.EffectiveToExclusive).HasColumnType("date");
        builder.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DecisionReference).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.SupersededAtUtc).HasColumnType("datetime2");
        builder.Property(x => x.SupersededReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.OfficeId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_OfficeVersions_Office_EffectiveRange");
        builder.HasIndex(x => new { x.OfficeId, x.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("UX_OfficeVersions_Office_Revision");
        builder.HasOne(x => x.Office)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Classification)
            .WithMany(x => x.OfficeVersions)
            .HasForeignKey(x => x.ClassificationCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SupersededByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    internal static string SupersededMetadataCheck(string at, string by, string reason) =>
        $"(([{at}] IS NULL AND [{by}] IS NULL AND [{reason}] IS NULL) OR " +
        $"([{at}] IS NOT NULL AND [{by}] IS NOT NULL AND [{reason}] IS NOT NULL AND LEN(LTRIM(RTRIM([{reason}]))) > 0))";
}

public sealed class OrgUnitConfiguration : IEntityTypeConfiguration<OrgUnit>
{
    public void Configure(EntityTypeBuilder<OrgUnit> builder)
    {
        builder.ToTable("OrgUnits");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(x => new { x.OfficeId, x.UnitCode })
            .IsUnique()
            .HasDatabaseName("UX_OrgUnits_Office_UnitCode");
        builder.HasOne(x => x.Office)
            .WithMany(x => x.OrgUnits)
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Versions)
            .WithOne(x => x.OrgUnit)
            .HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Positions)
            .WithOne(x => x.OrgUnit)
            .HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OrgUnitVersionConfiguration : IEntityTypeConfiguration<OrgUnitVersion>
{
    public void Configure(EntityTypeBuilder<OrgUnitVersion> builder)
    {
        builder.ToTable("OrgUnitVersions", table =>
        {
            table.HasCheckConstraint("CK_OrgUnitVersions_NotSelfParent",
                "[ParentOrgUnitId] IS NULL OR [ParentOrgUnitId] <> [OrgUnitId]");
            table.HasCheckConstraint("CK_OrgUnitVersions_UnitType",
                "[UnitType] IN ('DIRECTORATE', 'DIVISION', 'DEPARTMENT', 'SECTION', 'WORKGROUP', 'OTHER')");
            table.HasCheckConstraint("CK_OrgUnitVersions_EffectiveRange",
                "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_OrgUnitVersions_RevisionNumber", "[RevisionNumber] > 0");
            table.HasCheckConstraint("CK_OrgUnitVersions_SupersededMetadata",
                OfficeVersionConfiguration.SupersededMetadataCheck(
                    "SupersededAtUtc", "SupersededByUserId", "SupersededReason"));
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.UnitType)
            .HasConversion(v => ToDatabaseCode(v), v => ParseDatabaseCode<OrgUnitType>(v))
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.EffectiveToExclusive).HasColumnType("date");
        builder.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DecisionReference).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.SupersededAtUtc).HasColumnType("datetime2");
        builder.Property(x => x.SupersededReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.OrgUnitId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_OrgUnitVersions_Unit_EffectiveRange");
        builder.HasIndex(x => new { x.OrgUnitId, x.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("UX_OrgUnitVersions_Unit_Revision");
        builder.HasOne(x => x.OrgUnit)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ParentOrgUnit)
            .WithMany()
            .HasForeignKey(x => x.ParentOrgUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SupersededByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }

    internal static string ToDatabaseCode<TEnum>(TEnum value)
        where TEnum : struct, Enum => value.ToString().ToUpperInvariant();

    internal static TEnum ParseDatabaseCode<TEnum>(string value)
        where TEnum : struct, Enum => Enum.Parse<TEnum>(value, ignoreCase: true);
}

public sealed class PositionLevelConfiguration : IEntityTypeConfiguration<PositionLevel>
{
    public void Configure(EntityTypeBuilder<PositionLevel> builder)
    {
        builder.ToTable("PositionLevels", table => table.HasCheckConstraint(
            "CK_PositionLevels_DisplayOrder", "[DisplayOrder] >= 0"));
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Code).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(150).IsRequired();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(x => x.Code).IsUnique().HasDatabaseName("UX_PositionLevels_Code");
        builder.HasIndex(x => x.DisplayOrder).HasDatabaseName("IX_PositionLevels_DisplayOrder");
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PositionConfiguration : IEntityTypeConfiguration<Position>
{
    public void Configure(EntityTypeBuilder<Position> builder)
    {
        builder.ToTable("Positions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.PositionCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(x => new { x.OrgUnitId, x.PositionCode })
            .IsUnique()
            .HasDatabaseName("UX_Positions_Unit_PositionCode");
        builder.HasOne(x => x.OrgUnit)
            .WithMany(x => x.Positions)
            .HasForeignKey(x => x.OrgUnitId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Versions)
            .WithOne(x => x.Position)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class PositionVersionConfiguration : IEntityTypeConfiguration<PositionVersion>
{
    public void Configure(EntityTypeBuilder<PositionVersion> builder)
    {
        builder.ToTable("PositionVersions", table =>
        {
            table.HasCheckConstraint("CK_PositionVersions_NotSelfSupervisor",
                "[ReportsToPositionId] IS NULL OR [ReportsToPositionId] <> [PositionId]");
            table.HasCheckConstraint("CK_PositionVersions_Track", "[Track] IN ('LINE', 'FUNCTIONAL')");
            table.HasCheckConstraint("CK_PositionVersions_MaxIncumbents",
                "[MaxIncumbents] IS NULL OR [MaxIncumbents] > 0");
            table.HasCheckConstraint("CK_PositionVersions_EffectiveRange",
                "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_PositionVersions_RevisionNumber", "[RevisionNumber] > 0");
            table.HasCheckConstraint("CK_PositionVersions_SupersededMetadata",
                OfficeVersionConfiguration.SupersededMetadataCheck(
                    "SupersededAtUtc", "SupersededByUserId", "SupersededReason"));
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Track)
            .HasConversion(v => OrgUnitVersionConfiguration.ToDatabaseCode(v),
                v => OrgUnitVersionConfiguration.ParseDatabaseCode<PositionTrack>(v))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.EffectiveToExclusive).HasColumnType("date");
        builder.Property(x => x.ChangeReason).HasMaxLength(500).IsRequired();
        builder.Property(x => x.DecisionReference).HasMaxLength(200);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.SupersededAtUtc).HasColumnType("datetime2");
        builder.Property(x => x.SupersededReason).HasMaxLength(500);
        builder.HasIndex(x => new { x.PositionId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_PositionVersions_Position_EffectiveRange");
        builder.HasIndex(x => new { x.PositionId, x.RevisionNumber })
            .IsUnique()
            .HasDatabaseName("UX_PositionVersions_Position_Revision");
        builder.HasIndex(x => new { x.ReportsToPositionId, x.EffectiveFrom })
            .HasDatabaseName("IX_PositionVersions_ReportsTo_EffectiveFrom");
        builder.HasOne(x => x.Position)
            .WithMany(x => x.Versions)
            .HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Level)
            .WithMany(x => x.PositionVersions)
            .HasForeignKey(x => x.LevelId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.ReportsToPosition)
            .WithMany()
            .HasForeignKey(x => x.ReportsToPositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.SupersededByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class StructureTemplateConfiguration : IEntityTypeConfiguration<StructureTemplate>
{
    public void Configure(EntityTypeBuilder<StructureTemplate> builder)
    {
        builder.ToTable("StructureTemplates", table =>
        {
            table.HasCheckConstraint("CK_StructureTemplates_Version", "[Version] > 0");
            table.HasCheckConstraint("CK_StructureTemplates_PublicationMetadata",
                "([State] = 'DRAFT' AND [PublishedAtUtc] IS NULL AND [PublishedByUserId] IS NULL) OR " +
                "([State] = 'PUBLISHED' AND [PublishedAtUtc] IS NOT NULL AND [PublishedByUserId] IS NOT NULL)");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.ClassificationCode).HasMaxLength(20).IsRequired();
        builder.Property(x => x.State)
            .HasConversion(v => OrgUnitVersionConfiguration.ToDatabaseCode(v),
                v => OrgUnitVersionConfiguration.ParseDatabaseCode<StructureTemplateState>(v))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(1000);
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.PublishedAtUtc).HasColumnType("datetime2");
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.HasIndex(x => new { x.ClassificationCode, x.Version })
            .IsUnique()
            .HasDatabaseName("UX_StructureTemplates_Classification_Version");
        builder.HasOne(x => x.Classification)
            .WithMany(x => x.StructureTemplates)
            .HasForeignKey(x => x.ClassificationCode)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.CreatedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PublishedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.OrgUnits)
            .WithOne(x => x.Template)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Positions)
            .WithOne(x => x.Template)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplateOrgUnitConfiguration : IEntityTypeConfiguration<TemplateOrgUnit>
{
    public void Configure(EntityTypeBuilder<TemplateOrgUnit> builder)
    {
        builder.ToTable("TemplateOrgUnits", table =>
        {
            table.HasCheckConstraint("CK_TemplateOrgUnits_NotSelfParent",
                "[ParentTemplateOrgUnitId] IS NULL OR [ParentTemplateOrgUnitId] <> [Id]");
            table.HasCheckConstraint("CK_TemplateOrgUnits_UnitType",
                "[UnitType] IN ('DIRECTORATE', 'DIVISION', 'DEPARTMENT', 'SECTION', 'WORKGROUP', 'OTHER')");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.TemplateId, x.Id })
            .HasName("AK_TemplateOrgUnits_Template_Id");
        builder.Property(x => x.UnitCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.UnitType)
            .HasConversion(v => OrgUnitVersionConfiguration.ToDatabaseCode(v),
                v => OrgUnitVersionConfiguration.ParseDatabaseCode<OrgUnitType>(v))
            .HasMaxLength(30)
            .IsUnicode(false)
            .IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.UnitCode })
            .IsUnique()
            .HasDatabaseName("UX_TemplateOrgUnits_Template_UnitCode");
        builder.HasOne(x => x.Template)
            .WithMany(x => x.OrgUnits)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Parent)
            .WithMany(x => x.Children)
            .HasForeignKey(x => new { x.TemplateId, x.ParentTemplateOrgUnitId })
            .HasPrincipalKey(x => new { x.TemplateId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Positions)
            .WithOne(x => x.OrgUnit)
            .HasForeignKey(x => new { x.TemplateId, x.TemplateOrgUnitId })
            .HasPrincipalKey(x => new { x.TemplateId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class TemplatePositionConfiguration : IEntityTypeConfiguration<TemplatePosition>
{
    public void Configure(EntityTypeBuilder<TemplatePosition> builder)
    {
        builder.ToTable("TemplatePositions", table =>
        {
            table.HasCheckConstraint("CK_TemplatePositions_NotSelfSupervisor",
                "[ReportsToTemplatePositionId] IS NULL OR [ReportsToTemplatePositionId] <> [Id]");
            table.HasCheckConstraint("CK_TemplatePositions_MaxIncumbents",
                "[MaxIncumbents] IS NULL OR [MaxIncumbents] > 0");
            table.HasCheckConstraint("CK_TemplatePositions_Track", "[Track] IN ('LINE', 'FUNCTIONAL')");
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.TemplateId, x.Id })
            .HasName("AK_TemplatePositions_Template_Id");
        builder.Property(x => x.PositionCode).HasMaxLength(50).IsRequired();
        builder.Property(x => x.Name).HasMaxLength(200).IsRequired();
        builder.Property(x => x.Track)
            .HasConversion(v => OrgUnitVersionConfiguration.ToDatabaseCode(v),
                v => OrgUnitVersionConfiguration.ParseDatabaseCode<PositionTrack>(v))
            .HasMaxLength(20)
            .IsUnicode(false)
            .IsRequired();
        builder.HasIndex(x => new { x.TemplateId, x.PositionCode })
            .IsUnique()
            .HasDatabaseName("UX_TemplatePositions_Template_PositionCode");
        builder.HasOne(x => x.ReportsToPosition)
            .WithMany()
            .HasForeignKey(x => new { x.TemplateId, x.ReportsToTemplatePositionId })
            .HasPrincipalKey(x => new { x.TemplateId, x.Id })
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<PositionLevel>()
            .WithMany()
            .HasForeignKey(x => x.LevelId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class OfficeTemplateApplicationConfiguration : IEntityTypeConfiguration<OfficeTemplateApplication>
{
    public void Configure(EntityTypeBuilder<OfficeTemplateApplication> builder)
    {
        builder.ToTable("OfficeTemplateApplications");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EffectiveFrom).HasColumnType("date");
        builder.Property(x => x.IdempotencyKey).HasMaxLength(128).IsRequired();
        builder.Property(x => x.RequestHash).HasMaxLength(64).IsUnicode(false).IsRequired();
        builder.Property(x => x.AppliedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(x => new { x.OfficeId, x.IdempotencyKey })
            .IsUnique()
            .HasDatabaseName("UX_OfficeTemplateApplications_Office_IdempotencyKey");
        builder.HasIndex(x => new { x.OfficeId, x.EffectiveFrom })
            .HasDatabaseName("IX_OfficeTemplateApplications_Office_EffectiveFrom");
        builder.HasOne(x => x.Office)
            .WithMany(x => x.TemplateApplications)
            .HasForeignKey(x => x.OfficeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Template)
            .WithMany(x => x.OfficeApplications)
            .HasForeignKey(x => x.TemplateId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.AppliedByUserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
