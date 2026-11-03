using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("RefreshTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.Property(x => x.CreatedByIp)
            .HasMaxLength(64);

        builder.Property(x => x.UserAgent)
            .HasMaxLength(512);

        builder.Property(x => x.ReplacedByTokenHash)
            .HasMaxLength(128);

        builder.Property(x => x.RevokeReason)
            .HasMaxLength(250);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_RefreshTokens_TokenHash");

        builder.HasIndex(x => new { x.UserId, x.ClientAppId })
            .HasDatabaseName("IX_RefreshTokens_UserId_ClientAppId");

        builder.HasOne(x => x.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ClientApp)
            .WithMany(c => c.RefreshTokens)
            .HasForeignKey(x => x.ClientAppId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

