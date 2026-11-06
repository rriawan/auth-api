using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations.Employees;

public sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("Employees", table =>
        {
            table.HasCheckConstraint("CK_Employees_EmployeeNumber_NotBlank", "LEN(LTRIM(RTRIM([EmployeeNumber]))) > 0");
            table.HasCheckConstraint("CK_Employees_EmployeeFullName_NotBlank", "LEN(LTRIM(RTRIM([EmployeeFullName]))) > 0");
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.EmployeeNumber).HasMaxLength(50).IsRequired();
        builder.Property(x => x.EmployeeFullName).HasMaxLength(200).IsRequired();
        builder.Property(x => x.RowVersion).IsRowVersion();
        builder.Property(x => x.CreatedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.Property(x => x.UpdatedAtUtc).HasColumnType("datetime2");
        builder.HasIndex(x => x.EmployeeNumber).IsUnique().HasDatabaseName("UX_Employees_EmployeeNumber");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UpdatedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmployeeStatusConfiguration : IEntityTypeConfiguration<EmployeeStatus>
{
    public void Configure(EntityTypeBuilder<EmployeeStatus> builder)
    {
        builder.ToTable("EmployeeStatuses", table =>
        {
            table.HasCheckConstraint("CK_EmployeeStatuses_EffectiveRange",
                "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_EmployeeStatuses_RevisionNumber", "[RevisionNumber] > 0");
            table.HasCheckConstraint("CK_EmployeeStatuses_ChangeReason_NotBlank", "LEN(LTRIM(RTRIM([ChangeReason]))) > 0");
            table.HasCheckConstraint("CK_EmployeeStatuses_StatusCode", "[StatusCode] IN ('ACTIVE', 'RESIGNED')");
            table.HasCheckConstraint("CK_EmployeeStatuses_RecordState", "[RecordState] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
            table.HasCheckConstraint("CK_EmployeeStatuses_SupersededMetadata", SupersededMetadataCheck());
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StatusCode).HasConversion(v => ToCode(v), v => FromCode<EmployeeStatusCode>(v))
            .HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.RecordState).HasConversion(v => ToCode(v), v => FromCode<EmployeeRecordState>(v))
            .HasMaxLength(20).IsUnicode(false).IsRequired();
        ConfigureTimelineFields(builder.Property(x => x.EffectiveFrom), builder.Property(x => x.EffectiveToExclusive),
            builder.Property(x => x.ChangeReason), builder.Property(x => x.DecisionReference!),
            builder.Property(x => x.CreatedAtUtc), builder.Property(x => x.SupersededAtUtc), builder.Property(x => x.SupersededReason));
        builder.HasIndex(x => new { x.EmployeeId, x.RevisionNumber }).IsUnique()
            .HasDatabaseName("UX_EmployeeStatuses_Employee_Revision");
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_EmployeeStatuses_Employee_EffectiveRange");
        builder.HasOne(x => x.Employee).WithMany(x => x.Statuses).HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.SupersededByUserId).OnDelete(DeleteBehavior.Restrict);
    }

    internal static string ToCode<TEnum>(TEnum value) where TEnum : struct, Enum => value.ToString().ToUpperInvariant();
    internal static TEnum FromCode<TEnum>(string value) where TEnum : struct, Enum => Enum.Parse<TEnum>(value, true);

    internal static void ConfigureTimelineFields(
        PropertyBuilder<DateOnly> from,
        PropertyBuilder<DateOnly?> to,
        PropertyBuilder<string> reason,
        PropertyBuilder<string> decisionReference,
        PropertyBuilder<DateTime> createdAt,
        PropertyBuilder<DateTime?> supersededAt,
        PropertyBuilder<string?> supersededReason)
    {
        from.HasColumnType("date");
        to.HasColumnType("date");
        reason.HasMaxLength(500).IsRequired();
        decisionReference.HasMaxLength(200);
        createdAt.HasColumnType("datetime2").IsRequired();
        supersededAt.HasColumnType("datetime2");
        supersededReason.HasMaxLength(500);
    }

    internal static string SupersededMetadataCheck() =>
        "([SupersededAtUtc] IS NULL AND [SupersededByUserId] IS NULL AND [SupersededReason] IS NULL) OR " +
        "([SupersededAtUtc] IS NOT NULL AND [SupersededByUserId] IS NOT NULL AND [SupersededReason] IS NOT NULL AND LEN(LTRIM(RTRIM([SupersededReason]))) > 0)";

}

public sealed class EmployeePositionConfiguration : IEntityTypeConfiguration<EmployeePosition>
{
    public void Configure(EntityTypeBuilder<EmployeePosition> builder)
    {
        builder.ToTable("EmployeePositions", table =>
        {
            table.HasCheckConstraint("CK_EmployeePositions_EffectiveRange",
                "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_EmployeePositions_RevisionNumber", "[RevisionNumber] > 0");
            table.HasCheckConstraint("CK_EmployeePositions_ChangeReason_NotBlank", "LEN(LTRIM(RTRIM([ChangeReason]))) > 0");
            table.HasCheckConstraint("CK_EmployeePositions_RecordState", "[RecordState] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
            table.HasCheckConstraint("CK_EmployeePositions_SupersededMetadata", EmployeeStatusConfiguration.SupersededMetadataCheck());
        });
        builder.HasKey(x => x.Id);
        builder.HasAlternateKey(x => new { x.EmployeeId, x.Id }).HasName("AK_EmployeePositions_Employee_Id");
        builder.Property(x => x.RecordState).HasConversion(v => EmployeeStatusConfiguration.ToCode(v),
            v => EmployeeStatusConfiguration.FromCode<EmployeeRecordState>(v)).HasMaxLength(20).IsUnicode(false).IsRequired();
        EmployeeStatusConfiguration.ConfigureTimelineFields(builder.Property(x => x.EffectiveFrom), builder.Property(x => x.EffectiveToExclusive),
            builder.Property(x => x.ChangeReason), builder.Property(x => x.DecisionReference!), builder.Property(x => x.CreatedAtUtc),
            builder.Property(x => x.SupersededAtUtc), builder.Property(x => x.SupersededReason));
        builder.HasIndex(x => new { x.AssignmentKey, x.RevisionNumber }).IsUnique()
            .HasDatabaseName("UX_EmployeePositions_Assignment_Revision");
        builder.HasIndex(x => x.AssignmentKey).IsUnique().HasFilter("[SupersededAtUtc] IS NULL")
            .HasDatabaseName("UX_EmployeePositions_Assignment_CurrentRevision");
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_EmployeePositions_Employee_EffectiveRange");
        builder.HasIndex(x => new { x.PositionId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_EmployeePositions_Position_EffectiveRange");
        builder.HasOne(x => x.Employee).WithMany(x => x.Positions).HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Position).WithMany().HasForeignKey(x => x.PositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.SupersededByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class EmployeeActingAssignmentConfiguration : IEntityTypeConfiguration<EmployeeActingAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeActingAssignment> builder)
    {
        builder.ToTable("EmployeeActingAssignments", table =>
        {
            table.HasCheckConstraint("CK_EmployeeActingAssignments_EffectiveRange",
                "[EffectiveToExclusive] IS NULL OR [EffectiveToExclusive] > [EffectiveFrom]");
            table.HasCheckConstraint("CK_EmployeeActingAssignments_RevisionNumber", "[RevisionNumber] > 0");
            table.HasCheckConstraint("CK_EmployeeActingAssignments_ChangeReason_NotBlank", "LEN(LTRIM(RTRIM([ChangeReason]))) > 0");
            table.HasCheckConstraint("CK_EmployeeActingAssignments_DecisionReference_NotBlank", "LEN(LTRIM(RTRIM([DecisionReference]))) > 0");
            table.HasCheckConstraint("CK_EmployeeActingAssignments_AssignmentType", "[AssignmentType] IN ('PLH', 'PLT')");
            table.HasCheckConstraint("CK_EmployeeActingAssignments_RecordState", "[RecordState] IN ('DRAFT', 'CONFIRMED', 'CANCELLED')");
            table.HasCheckConstraint("CK_EmployeeActingAssignments_SupersededMetadata", EmployeeStatusConfiguration.SupersededMetadataCheck());
        });
        builder.HasKey(x => x.Id);
        builder.Property(x => x.AssignmentType).HasConversion(v => EmployeeStatusConfiguration.ToCode(v),
            v => EmployeeStatusConfiguration.FromCode<ActingAssignmentType>(v)).HasMaxLength(20).IsUnicode(false).IsRequired();
        builder.Property(x => x.RecordState).HasConversion(v => EmployeeStatusConfiguration.ToCode(v),
            v => EmployeeStatusConfiguration.FromCode<EmployeeRecordState>(v)).HasMaxLength(20).IsUnicode(false).IsRequired();
        EmployeeStatusConfiguration.ConfigureTimelineFields(builder.Property(x => x.EffectiveFrom), builder.Property(x => x.EffectiveToExclusive),
            builder.Property(x => x.ChangeReason), builder.Property(x => x.DecisionReference), builder.Property(x => x.CreatedAtUtc),
            builder.Property(x => x.SupersededAtUtc), builder.Property(x => x.SupersededReason));
        builder.HasIndex(x => new { x.AssignmentKey, x.RevisionNumber }).IsUnique()
            .HasDatabaseName("UX_EmployeeActingAssignments_Assignment_Revision");
        builder.HasIndex(x => x.AssignmentKey).IsUnique().HasFilter("[SupersededAtUtc] IS NULL")
            .HasDatabaseName("UX_EmployeeActingAssignments_Assignment_CurrentRevision");
        builder.HasIndex(x => new { x.EmployeeId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_EmployeeActingAssignments_Employee_EffectiveRange");
        builder.HasIndex(x => new { x.TargetPositionId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_EmployeeActingAssignments_TargetPosition_EffectiveRange");
        builder.HasIndex(x => new { x.SourceEmployeePositionId, x.EffectiveFrom, x.EffectiveToExclusive })
            .HasDatabaseName("IX_EmployeeActingAssignments_SourcePosition_EffectiveRange");
        builder.HasOne(x => x.Employee).WithMany(x => x.ActingAssignments).HasForeignKey(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.SourceEmployeePosition).WithMany(x => x.ActingAssignments)
            .HasForeignKey(x => new { x.EmployeeId, x.SourceEmployeePositionId })
            .HasPrincipalKey(x => new { x.EmployeeId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.TargetPosition).WithMany().HasForeignKey(x => x.TargetPositionId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.CreatedByUserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.SupersededByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UserEmployeeLinkConfiguration : IEntityTypeConfiguration<UserEmployeeLink>
{
    public void Configure(EntityTypeBuilder<UserEmployeeLink> builder)
    {
        builder.ToTable("UserEmployeeLinks");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.LinkedAtUtc).HasColumnType("datetime2").IsRequired();
        builder.HasIndex(x => x.EmployeeId).IsUnique().HasDatabaseName("UX_UserEmployeeLinks_EmployeeId");
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.UserId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Employee>().WithOne().HasForeignKey<UserEmployeeLink>(x => x.EmployeeId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<User>().WithMany().HasForeignKey(x => x.LinkedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
