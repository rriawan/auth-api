using AuthApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Common.Interfaces;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<ClientApp> ClientApps { get; }
    DbSet<Role> Roles { get; }
    DbSet<Permission> Permissions { get; }
    DbSet<RolePermission> RolePermissions { get; }
    DbSet<UserAppRole> UserAppRoles { get; }
    DbSet<RefreshToken> RefreshTokens { get; }
    DbSet<ActionToken> ActionTokens { get; }
    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

