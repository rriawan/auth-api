using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class RoleConfiguration : IEntityTypeConfiguration<Role>
{
    public void Configure(EntityTypeBuilder<Role> builder)
    {
        builder.ToTable("Roles");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.HasIndex(x => new { x.ClientAppId, x.Name })
            .IsUnique()
            .HasDatabaseName("IX_Roles_ClientAppId_Name");

        builder.HasOne(x => x.ClientApp)
            .WithMany(c => c.Roles)
            .HasForeignKey(x => x.ClientAppId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

