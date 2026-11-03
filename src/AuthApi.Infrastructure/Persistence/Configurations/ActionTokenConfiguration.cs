using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class ActionTokenConfiguration : IEntityTypeConfiguration<ActionToken>
{
    public void Configure(EntityTypeBuilder<ActionToken> builder)
    {
        builder.ToTable("ActionTokens");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Purpose)
            .HasConversion<string>()
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(x => x.TokenHash)
            .IsRequired()
            .HasMaxLength(128);

        builder.HasIndex(x => x.TokenHash)
            .IsUnique()
            .HasDatabaseName("IX_ActionTokens_TokenHash");

        builder.HasIndex(x => new { x.UserId, x.Purpose })
            .HasDatabaseName("IX_ActionTokens_UserId_Purpose");

        builder.HasOne(x => x.User)
            .WithMany(u => u.ActionTokens)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

