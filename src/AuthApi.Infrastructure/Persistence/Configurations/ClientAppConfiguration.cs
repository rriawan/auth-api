using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class ClientAppConfiguration : IEntityTypeConfiguration<ClientApp>
{
    public void Configure(EntityTypeBuilder<ClientApp> builder)
    {
        builder.ToTable("ClientApps");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ClientId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(x => x.Description)
            .HasMaxLength(500);

        builder.Property(x => x.ApiKeyHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.ApiKeyPrefix)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(x => x.AllowedOrigins)
            .HasMaxLength(2000);

        builder.HasIndex(x => x.ClientId)
            .IsUnique()
            .HasDatabaseName("IX_ClientApps_ClientId");

        builder.HasIndex(x => x.ApiKeyHash)
            .HasDatabaseName("IX_ClientApps_ApiKeyHash");
    }
}

