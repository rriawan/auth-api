using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
{
    public void Configure(EntityTypeBuilder<Permission> builder)
    {
        builder.ToTable("Permissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Code)
            .IsRequired()
            .HasMaxLength(150);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.ClientAppId, x.Code })
            .IsUnique()
            .HasDatabaseName("IX_Permissions_ClientAppId_Code");

        builder.HasOne(x => x.ClientApp)
            .WithMany(c => c.Permissions)
            .HasForeignKey(x => x.ClientAppId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

