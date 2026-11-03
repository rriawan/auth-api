using System.Text.Json;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Constants;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace AuthApi.Infrastructure.Persistence;

public class DatabaseSeeder(
    AppDbContext dbContext,
    IPasswordHasherService passwordHasher,
    ITokenCryptoService tokenCryptoService,
    IConfiguration configuration,
    ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var anyChanges = false;

        // 1. Seed aplikasi internal 'auth-admin' di ClientApps
        var adminApp = await dbContext.ClientApps
            .FirstOrDefaultAsync(x => x.ClientId == SystemConstants.DefaultAdminClientId, cancellationToken);

        if (adminApp is null)
        {
            var rawApiKey = tokenCryptoService.GenerateSecureToken(24, "ak_admin_");
            var apiKeyHash = tokenCryptoService.ComputeSha256Hash(rawApiKey);
            var apiKeyPrefix = rawApiKey[..Math.Min(14, rawApiKey.Length)];

            adminApp = new ClientApp
            {
                Id = Guid.NewGuid(),
                ClientId = SystemConstants.DefaultAdminClientId,
                Name = SystemConstants.DefaultAdminClientName,
                Description = "Aplikasi internal sistem untuk administrasi dan provisioning Auth API.",
                ApiKeyHash = apiKeyHash,
                ApiKeyPrefix = apiKeyPrefix,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            dbContext.ClientApps.Add(adminApp);
            anyChanges = true;

            logger.LogWarning(
                "Bootstrap ClientApp '{ClientId}' dibuat. Simpan API Key awal ini (hanya ditampilkan 1 kali): {RawApiKey}",
                adminApp.ClientId,
                rawApiKey);
        }

        // 2. Seed katalog Permissions bawaan untuk 'auth-admin'
        var existingPermissions = await dbContext.Permissions
            .Where(x => x.ClientAppId == adminApp.Id)
            .ToListAsync(cancellationToken);

        var permissionByCode = existingPermissions.ToDictionary(x => x.Code, StringComparer.OrdinalIgnoreCase);

        foreach (var (code, description) in SystemConstants.Permissions.AllAdminPermissions)
        {
            if (!permissionByCode.TryGetValue(code, out var permission))
            {
                permission = new Permission
                {
                    Id = Guid.NewGuid(),
                    ClientAppId = adminApp.Id,
                    Code = code,
                    Description = description,
                    CreatedAtUtc = DateTime.UtcNow
                };
                dbContext.Permissions.Add(permission);
                permissionByCode[code] = permission;
                anyChanges = true;
            }
        }

        // 3. Seed Role 'SuperAdmin' untuk 'auth-admin' beserta seluruh RolePermissions
        var superAdminRole = await dbContext.Roles
            .FirstOrDefaultAsync(
                x => x.ClientAppId == adminApp.Id && x.Name == SystemConstants.DefaultSuperAdminRole,
                cancellationToken);

        if (superAdminRole is null)
        {
            superAdminRole = new Role
            {
                Id = Guid.NewGuid(),
                ClientAppId = adminApp.Id,
                Name = SystemConstants.DefaultSuperAdminRole,
                Description = "Administrator penuh atas seluruh fungsi sistem Auth API.",
                IsSystemRole = true,
                CreatedAtUtc = DateTime.UtcNow
            };
            dbContext.Roles.Add(superAdminRole);
            anyChanges = true;
        }

        var existingRolePermissionIds = await dbContext.RolePermissions
            .Where(x => x.RoleId == superAdminRole.Id)
            .Select(x => x.PermissionId)
            .ToHashSetAsync(cancellationToken);

        foreach (var permission in permissionByCode.Values)
        {
            if (!existingRolePermissionIds.Contains(permission.Id))
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = superAdminRole.Id,
                    PermissionId = permission.Id,
                    AssignedAtUtc = DateTime.UtcNow
                });
                anyChanges = true;
            }
        }

        // 4. Seed akun 'SuperAdmin' awal
        var adminUsername = configuration["BootstrapAdmin:Username"]?.Trim();
        if (string.IsNullOrWhiteSpace(adminUsername))
        {
            adminUsername = "superadmin";
        }

        var adminEmail = configuration["BootstrapAdmin:Email"]?.Trim();
        if (string.IsNullOrWhiteSpace(adminEmail))
        {
            adminEmail = "superadmin@local.dev";
        }

        var adminFullName = configuration["BootstrapAdmin:FullName"]?.Trim();
        if (string.IsNullOrWhiteSpace(adminFullName))
        {
            adminFullName = "System Super Administrator";
        }

        var superAdminUser = await dbContext.Users
            .FirstOrDefaultAsync(
                x => x.Username == adminUsername || x.Email == adminEmail,
                cancellationToken);

        if (superAdminUser is null)
        {
            var initialPassword = configuration["BootstrapAdmin:InitialPassword"];
            var isGeneratedPassword = false;

            if (string.IsNullOrWhiteSpace(initialPassword))
            {
                initialPassword = tokenCryptoService.GenerateSecureToken(18, "Admin!");
                isGeneratedPassword = true;
            }

            superAdminUser = new User
            {
                Id = Guid.NewGuid(),
                Username = adminUsername,
                Email = adminEmail,
                FullName = adminFullName,
                IsActive = true,
                IsAccountVerified = true,
                FailedLoginCount = 0,
                CreatedAtUtc = DateTime.UtcNow
            };

            superAdminUser.PasswordHash = passwordHasher.HashPassword(superAdminUser, initialPassword);

            dbContext.Users.Add(superAdminUser);
            anyChanges = true;

            if (isGeneratedPassword)
            {
                logger.LogWarning(
                    "Akun SuperAdmin '{Username}' dibuat dengan password acak sementara (wajib segera diganti setelah bootstrap): {InitialPassword}",
                    superAdminUser.Username,
                    initialPassword);
            }
            else
            {
                logger.LogInformation(
                    "Akun SuperAdmin '{Username}' dibuat menggunakan InitialPassword dari konfigurasi. Segera ganti password setelah login pertama.",
                    superAdminUser.Username);
            }
        }

        // 5. Pastikan penugasan UserAppRole (SuperAdmin -> auth-admin -> SuperAdmin Role)
        var hasUserAppRole = await dbContext.UserAppRoles
            .AnyAsync(
                x => x.UserId == superAdminUser.Id
                     && x.ClientAppId == adminApp.Id
                     && x.RoleId == superAdminRole.Id,
                cancellationToken);

        if (!hasUserAppRole)
        {
            dbContext.UserAppRoles.Add(new UserAppRole
            {
                UserId = superAdminUser.Id,
                ClientAppId = adminApp.Id,
                RoleId = superAdminRole.Id,
                AssignedAtUtc = DateTime.UtcNow
            });
            anyChanges = true;
        }

        if (anyChanges)
        {
            dbContext.AuditLogs.Add(new AuditLog
            {
                Id = Guid.NewGuid(),
                TimestampUtc = DateTime.UtcNow,
                UserId = superAdminUser.Id,
                ClientAppId = adminApp.Id,
                EventType = AuditEventType.SystemBootstrapSeeded,
                IsSuccess = true,
                DetailJson = JsonSerializer.Serialize(new
                {
                    clientId = adminApp.ClientId,
                    role = superAdminRole.Name,
                    username = superAdminUser.Username,
                    permissionsCount = permissionByCode.Count
                })
            });

            await dbContext.SaveChangesAsync(cancellationToken);
            logger.LogInformation("Database seeding awal selesai disimpan secara idempoten.");
        }
        else
        {
            logger.LogInformation("Database seeding dilewati: seluruh data awal sudah tersedia.");
        }
    }
}
