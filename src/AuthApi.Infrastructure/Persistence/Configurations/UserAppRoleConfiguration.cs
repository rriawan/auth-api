using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace AuthApi.Infrastructure.Persistence.Configurations;

public class UserAppRoleConfiguration : IEntityTypeConfiguration<UserAppRole>
{
    public void Configure(EntityTypeBuilder<UserAppRole> builder)
    {
        builder.ToTable("UserAppRoles");

        builder.HasKey(x => new { x.UserId, x.ClientAppId, x.RoleId });

        builder.HasIndex(x => new { x.UserId, x.ClientAppId })
            .HasDatabaseName("IX_UserAppRoles_UserId_ClientAppId");

        builder.HasOne(x => x.User)
            .WithMany(u => u.UserAppRoles)
            .HasForeignKey(x => x.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(x => x.ClientApp)
            .WithMany(c => c.UserAppRoles)
            .HasForeignKey(x => x.ClientAppId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.Role)
            .WithMany(r => r.UserAppRoles)
            .HasForeignKey(x => x.RoleId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

