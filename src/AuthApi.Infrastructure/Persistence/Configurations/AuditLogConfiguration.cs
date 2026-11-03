using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.IpAddress)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        builder.Property(x => x.DetailJson)
            .HasMaxLength(4000);

        builder.HasIndex(x => x.TimestampUtc)
            .HasDatabaseName("IX_AuditLogs_TimestampUtc");

        builder.HasIndex(x => new { x.UserId, x.TimestampUtc })
            .HasDatabaseName("IX_AuditLogs_UserId_TimestampUtc");

        builder.HasIndex(x => new { x.ClientAppId, x.TimestampUtc })
            .HasDatabaseName("IX_AuditLogs_ClientAppId_TimestampUtc");

        builder.HasIndex(x => x.EventType)
            .HasDatabaseName("IX_AuditLogs_EventType");

        builder.HasOne(x => x.User)
            .WithMany(u => u.AuditLogs)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(x => x.ClientApp)
            .WithMany(c => c.AuditLogs)
            .HasForeignKey(x => x.ClientAppId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

