using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Admin.DTOs;
using AuthApi.Domain.Constants;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Admin;

public sealed class AdminService(
    IAppDbContext dbContext,
    IPasswordHasherService passwordHasher,
    ITokenCryptoService tokenCryptoService,
    IAuditLogService auditLogService) : IAdminService
{
    private const string OneTimeApiKeyNotice =
        "PENTING: Simpan RawApiKey ini sekarang. Demi keamanan, nilai asli API Key hanya ditampilkan 1 kali pada respons ini dan disimpan dalam bentuk hash SHA-256.";

    // =========================================================================
    // 1. Client Apps & API Keys
    // =========================================================================

    public async Task<IReadOnlyList<ClientAppDto>> GetClientAppsAsync(CancellationToken cancellationToken = default)
    {
        return await dbContext.ClientApps
            .AsNoTracking()
            .OrderBy(x => x.ClientId)
            .Select(x => new ClientAppDto(
                x.Id,
                x.ClientId,
                x.Name,
                x.Description,
                x.ApiKeyPrefix,
                x.AllowedOrigins,
                x.IsActive,
                x.Roles.Count,
                x.Permissions.Count,
                x.UserAppRoles.Select(u => u.UserId).Distinct().Count(),
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<ClientAppDto> GetClientAppByIdAsync(Guid appId, CancellationToken cancellationToken = default)
    {
        return await dbContext.ClientApps
            .AsNoTracking()
            .Where(x => x.Id == appId)
            .Select(x => new ClientAppDto(
                x.Id,
                x.ClientId,
                x.Name,
                x.Description,
                x.ApiKeyPrefix,
                x.AllowedOrigins,
                x.IsActive,
                x.Roles.Count,
                x.Permissions.Count,
                x.UserAppRoles.Select(u => u.UserId).Distinct().Count(),
                x.CreatedAtUtc,
                x.UpdatedAtUtc))
            .FirstOrDefaultAsync(cancellationToken)
            ?? throw new KeyNotFoundException("Aplikasi klien (ClientApp) tidak ditemukan.");
    }

    public async Task<ClientAppWithRawKeyResponse> CreateClientAppAsync(
        CreateClientAppRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var normalizedClientId = request.ClientId.Trim().ToLowerInvariant();
        var exists = await dbContext.ClientApps
            .AnyAsync(x => x.ClientId == normalizedClientId, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"ClientId '{normalizedClientId}' sudah terdaftar.");
        }

        var (rawApiKey, apiKeyHash, apiKeyPrefix) = tokenCryptoService.GenerateClientApiKey();
        var nowUtc = DateTime.UtcNow;

        var clientApp = new ClientApp
        {
            Id = Guid.NewGuid(),
            ClientId = normalizedClientId,
            Name = request.Name.Trim(),
            Description = request.Description?.Trim(),
            ApiKeyHash = apiKeyHash,
            ApiKeyPrefix = apiKeyPrefix,
            AllowedOrigins = request.AllowedOrigins?.Trim(),
            IsActive = request.IsActive,
            CreatedAtUtc = nowUtc
        };

        dbContext.ClientApps.Add(clientApp);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.ClientAppCreated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: clientApp.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { clientApp.ClientId, clientApp.Name, clientApp.IsActive },
            cancellationToken: cancellationToken);

        var dto = new ClientAppDto(
            clientApp.Id,
            clientApp.ClientId,
            clientApp.Name,
            clientApp.Description,
            clientApp.ApiKeyPrefix,
            clientApp.AllowedOrigins,
            clientApp.IsActive,
            RolesCount: 0,
            PermissionsCount: 0,
            AssignedUsersCount: 0,
            clientApp.CreatedAtUtc,
            clientApp.UpdatedAtUtc);

        return new ClientAppWithRawKeyResponse(dto, rawApiKey, OneTimeApiKeyNotice);
    }

    public async Task<ClientAppDto> UpdateClientAppAsync(
        Guid appId,
        UpdateClientAppRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var clientApp = await dbContext.ClientApps
            .FirstOrDefaultAsync(x => x.Id == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Aplikasi klien (ClientApp) tidak ditemukan.");

        if (string.Equals(clientApp.ClientId, SystemConstants.DefaultAdminClientId, StringComparison.OrdinalIgnoreCase)
            && !request.IsActive)
        {
            throw new InvalidOperationException(
                $"Aplikasi sistem '{SystemConstants.DefaultAdminClientId}' tidak boleh dinonaktifkan.");
        }

        var nowUtc = DateTime.UtcNow;
        var wasActive = clientApp.IsActive;

        clientApp.Name = request.Name.Trim();
        clientApp.Description = request.Description?.Trim();
        clientApp.AllowedOrigins = request.AllowedOrigins?.Trim();
        clientApp.IsActive = request.IsActive;
        clientApp.UpdatedAtUtc = nowUtc;

        // Jika aplikasi dinonaktifkan, cabut seluruh sesi refresh token aktif pada aplikasi tersebut
        if (wasActive && !request.IsActive)
        {
            var activeSessions = await dbContext.RefreshTokens
                .Where(x => x.ClientAppId == clientApp.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.RevokedAtUtc = nowUtc;
                session.RevokeReason = "ClientApp deactivated by administrator";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.ClientAppUpdated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: clientApp.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { clientApp.ClientId, clientApp.Name, clientApp.IsActive },
            cancellationToken: cancellationToken);

        return await GetClientAppByIdAsync(clientApp.Id, cancellationToken);
    }

    public async Task<ClientAppWithRawKeyResponse> RotateClientAppApiKeyAsync(
        Guid appId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var clientApp = await dbContext.ClientApps
            .FirstOrDefaultAsync(x => x.Id == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Aplikasi klien (ClientApp) tidak ditemukan.");

        var (rawApiKey, apiKeyHash, apiKeyPrefix) = tokenCryptoService.GenerateClientApiKey();
        clientApp.ApiKeyHash = apiKeyHash;
        clientApp.ApiKeyPrefix = apiKeyPrefix;
        clientApp.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.ClientApiKeyRotated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: clientApp.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { clientApp.ClientId, newApiKeyPrefix = apiKeyPrefix },
            cancellationToken: cancellationToken);

        var dto = await GetClientAppByIdAsync(clientApp.Id, cancellationToken);
        return new ClientAppWithRawKeyResponse(dto, rawApiKey, OneTimeApiKeyNotice);
    }

    // =========================================================================
    // 2. Users Management
    // =========================================================================

    public async Task<PagedResponse<AdminUserDto>> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var query = dbContext.Users.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            query = query.Where(x =>
                x.Username.Contains(term) ||
                x.Email.Contains(term) ||
                x.FullName.Contains(term));
        }

        var totalCount = await query.CountAsync(cancellationToken);
        var users = await query
            .OrderByDescending(x => x.CreatedAtUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(x => x.UserAppRoles)
                .ThenInclude(uar => uar.ClientApp)
            .Include(x => x.UserAppRoles)
                .ThenInclude(uar => uar.Role)
            .ToListAsync(cancellationToken);

        var items = users.Select(MapToAdminUserDto).ToList();
        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

        return new PagedResponse<AdminUserDto>(items, page, pageSize, totalCount, totalPages);
    }

    public async Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Include(x => x.UserAppRoles)
                .ThenInclude(uar => uar.ClientApp)
            .Include(x => x.UserAppRoles)
                .ThenInclude(uar => uar.Role)
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        return MapToAdminUserDto(user);
    }

    public async Task<AdminUserDto> CreateUserAsync(
        CreateUserRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var username = request.Username.Trim();
        var email = request.Email.Trim();

        var conflict = await dbContext.Users
            .AnyAsync(x => x.Username == username || x.Email == email, cancellationToken);

        if (conflict)
        {
            throw new InvalidOperationException("Username atau Email sudah digunakan oleh pengguna lain.");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Username = username,
            Email = email,
            FullName = request.FullName.Trim(),
            IsActive = request.IsActive,
            IsAccountVerified = request.IsAccountVerified,
            FailedLoginCount = 0,
            CreatedAtUtc = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(user, request.Password);

        dbContext.Users.Add(user);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.UserCreated,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actorUserId, user.Username, user.Email, user.IsActive, user.IsAccountVerified },
            cancellationToken: cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<AdminUserDto> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        var email = request.Email.Trim();
        var emailConflict = await dbContext.Users
            .AnyAsync(x => x.Id != user.Id && x.Email == email, cancellationToken);

        if (emailConflict)
        {
            throw new InvalidOperationException("Email sudah digunakan oleh pengguna lain.");
        }

        var nowUtc = DateTime.UtcNow;
        var wasActive = user.IsActive;

        user.Email = email;
        user.FullName = request.FullName.Trim();
        user.IsActive = request.IsActive;
        user.IsAccountVerified = request.IsAccountVerified;
        user.UpdatedAtUtc = nowUtc;

        // Jika akun dinonaktifkan, cabut seluruh sesi refresh token aktif milik user
        if (wasActive && !request.IsActive)
        {
            var activeSessions = await dbContext.RefreshTokens
                .Where(x => x.UserId == user.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
                .ToListAsync(cancellationToken);

            foreach (var session in activeSessions)
            {
                session.RevokedAtUtc = nowUtc;
                session.RevokeReason = "User deactivated by administrator";
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.UserUpdated,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actorUserId, user.Username, user.Email, user.IsActive, user.IsAccountVerified },
            cancellationToken: cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<AdminUserDto> UnlockUserAsync(
        Guid userId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        user.FailedLoginCount = 0;
        user.LockoutEndUtc = null;
        user.UpdatedAtUtc = DateTime.UtcNow;

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.UserUnlocked,
            isSuccess: true,
            userId: user.Id,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actorUserId, user.Username },
            cancellationToken: cancellationToken);

        return await GetUserByIdAsync(user.Id, cancellationToken);
    }

    public async Task<RevokeSessionsResponse> RevokeUserSessionsAsync(
        Guid userId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users
            .AnyAsync(x => x.Id == userId, cancellationToken);

        if (!userExists)
        {
            throw new KeyNotFoundException("Pengguna tidak ditemukan.");
        }

        var nowUtc = DateTime.UtcNow;
        var activeSessions = await dbContext.RefreshTokens
            .Where(x => x.UserId == userId && x.RevokedAtUtc == null && x.ExpiresAtUtc > nowUtc)
            .ToListAsync(cancellationToken);

        foreach (var session in activeSessions)
        {
            session.RevokedAtUtc = nowUtc;
            session.RevokeReason = "Revoked by administrator";
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.UserSessionsRevoked,
            isSuccess: true,
            userId: userId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actorUserId, revokedSessionsCount = activeSessions.Count },
            cancellationToken: cancellationToken);

        return new RevokeSessionsResponse(
            userId,
            activeSessions.Count,
            $"Berhasil mencabut {activeSessions.Count} sesi aktif milik pengguna.");
    }

    // =========================================================================
    // 3. Permissions & Roles per Application
    // =========================================================================

    public async Task<IReadOnlyList<PermissionDto>> GetAppPermissionsAsync(
        Guid appId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAppExistsAsync(appId, cancellationToken);

        return await dbContext.Permissions
            .AsNoTracking()
            .Where(x => x.ClientAppId == appId)
            .OrderBy(x => x.Code)
            .Select(x => new PermissionDto(x.Id, x.ClientAppId, x.Code, x.Description, x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<PermissionDto> CreateAppPermissionAsync(
        Guid appId,
        CreatePermissionRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        await EnsureAppExistsAsync(appId, cancellationToken);

        var code = request.Code.Trim().ToLowerInvariant();
        var exists = await dbContext.Permissions
            .AnyAsync(x => x.ClientAppId == appId && x.Code == code, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Permission '{code}' sudah ada pada aplikasi ini.");
        }

        var permission = new Permission
        {
            Id = Guid.NewGuid(),
            ClientAppId = appId,
            Code = code,
            Description = request.Description?.Trim(),
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Permissions.Add(permission);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.PermissionCreated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { permission.Id, permission.Code },
            cancellationToken: cancellationToken);

        return new PermissionDto(
            permission.Id,
            permission.ClientAppId,
            permission.Code,
            permission.Description,
            permission.CreatedAtUtc);
    }

    public async Task<PermissionDto> UpdateAppPermissionAsync(
        Guid appId,
        Guid permissionId,
        UpdatePermissionRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var permission = await dbContext.Permissions
            .FirstOrDefaultAsync(x => x.Id == permissionId && x.ClientAppId == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Permission tidak ditemukan pada aplikasi ini.");

        var code = request.Code.Trim().ToLowerInvariant();
        var conflict = await dbContext.Permissions
            .AnyAsync(x => x.ClientAppId == appId && x.Id != permissionId && x.Code == code, cancellationToken);

        if (conflict)
        {
            throw new InvalidOperationException($"Permission '{code}' sudah digunakan pada aplikasi ini.");
        }

        permission.Code = code;
        permission.Description = request.Description?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.PermissionUpdated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { permission.Id, permission.Code },
            cancellationToken: cancellationToken);

        return new PermissionDto(
            permission.Id,
            permission.ClientAppId,
            permission.Code,
            permission.Description,
            permission.CreatedAtUtc);
    }

    public async Task DeleteAppPermissionAsync(
        Guid appId,
        Guid permissionId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var permission = await dbContext.Permissions
            .FirstOrDefaultAsync(x => x.Id == permissionId && x.ClientAppId == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Permission tidak ditemukan pada aplikasi ini.");

        var rolePermissions = await dbContext.RolePermissions
            .Where(x => x.PermissionId == permission.Id)
            .ToListAsync(cancellationToken);

        if (rolePermissions.Count > 0)
        {
            dbContext.RolePermissions.RemoveRange(rolePermissions);
        }

        dbContext.Permissions.Remove(permission);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.PermissionDeleted,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { permission.Id, permission.Code },
            cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyList<RoleDto>> GetAppRolesAsync(
        Guid appId,
        CancellationToken cancellationToken = default)
    {
        await EnsureAppExistsAsync(appId, cancellationToken);

        var roles = await dbContext.Roles
            .AsNoTracking()
            .Where(x => x.ClientAppId == appId)
            .Include(x => x.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        return roles.Select(MapToRoleDto).ToList();
    }

    public async Task<RoleDto> CreateAppRoleAsync(
        Guid appId,
        CreateRoleRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        await EnsureAppExistsAsync(appId, cancellationToken);

        var roleName = request.Name.Trim();
        var exists = await dbContext.Roles
            .AnyAsync(x => x.ClientAppId == appId && x.Name == roleName, cancellationToken);

        if (exists)
        {
            throw new InvalidOperationException($"Role '{roleName}' sudah terdaftar pada aplikasi ini.");
        }

        var role = new Role
        {
            Id = Guid.NewGuid(),
            ClientAppId = appId,
            Name = roleName,
            Description = request.Description?.Trim(),
            IsSystemRole = false,
            CreatedAtUtc = DateTime.UtcNow
        };

        dbContext.Roles.Add(role);

        if (request.PermissionIds is { Count: > 0 })
        {
            var distinctPermIds = request.PermissionIds.Distinct().ToList();
            var validPermissions = await dbContext.Permissions
                .Where(x => x.ClientAppId == appId && distinctPermIds.Contains(x.Id))
                .Select(x => x.Id)
                .ToListAsync(cancellationToken);

            if (validPermissions.Count != distinctPermIds.Count)
            {
                throw new ArgumentException("Satu atau lebih PermissionId tidak ditemukan pada aplikasi ini.");
            }

            foreach (var permId in validPermissions)
            {
                dbContext.RolePermissions.Add(new RolePermission
                {
                    RoleId = role.Id,
                    PermissionId = permId,
                    AssignedAtUtc = DateTime.UtcNow
                });
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.RoleCreated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { role.Id, role.Name },
            cancellationToken: cancellationToken);

        return await GetRoleByIdInternalAsync(appId, role.Id, cancellationToken);
    }

    public async Task<RoleDto> UpdateAppRoleAsync(
        Guid appId,
        Guid roleId,
        UpdateRoleRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == roleId && x.ClientAppId == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Role tidak ditemukan pada aplikasi ini.");

        var roleName = request.Name.Trim();
        if (role.IsSystemRole && !string.Equals(role.Name, roleName, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Nama System Role bawaan tidak boleh diubah.");
        }

        var conflict = await dbContext.Roles
            .AnyAsync(x => x.ClientAppId == appId && x.Id != roleId && x.Name == roleName, cancellationToken);

        if (conflict)
        {
            throw new InvalidOperationException($"Role '{roleName}' sudah digunakan pada aplikasi ini.");
        }

        role.Name = roleName;
        role.Description = request.Description?.Trim();

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.RoleUpdated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { role.Id, role.Name },
            cancellationToken: cancellationToken);

        return await GetRoleByIdInternalAsync(appId, role.Id, cancellationToken);
    }

    public async Task DeleteAppRoleAsync(
        Guid appId,
        Guid roleId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == roleId && x.ClientAppId == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Role tidak ditemukan pada aplikasi ini.");

        if (role.IsSystemRole)
        {
            throw new InvalidOperationException("System Role bawaan tidak dapat dihapus.");
        }

        dbContext.Roles.Remove(role);
        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.RoleDeleted,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { role.Id, role.Name },
            cancellationToken: cancellationToken);
    }

    public async Task<RoleDto> SetRolePermissionsAsync(
        Guid appId,
        Guid roleId,
        SetRolePermissionsRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var role = await dbContext.Roles
            .FirstOrDefaultAsync(x => x.Id == roleId && x.ClientAppId == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Role tidak ditemukan pada aplikasi ini.");

        var distinctPermissionIds = request.PermissionIds.Distinct().ToList();

        if (distinctPermissionIds.Count > 0)
        {
            var validCount = await dbContext.Permissions
                .CountAsync(x => x.ClientAppId == appId && distinctPermissionIds.Contains(x.Id), cancellationToken);

            if (validCount != distinctPermissionIds.Count)
            {
                throw new ArgumentException(
                    "Seluruh PermissionId wajib berasal dari aplikasi (ClientAppId) yang sama dengan Role.");
            }
        }

        var existingMappings = await dbContext.RolePermissions
            .Where(x => x.RoleId == role.Id)
            .ToListAsync(cancellationToken);

        if (existingMappings.Count > 0)
        {
            dbContext.RolePermissions.RemoveRange(existingMappings);
        }

        var nowUtc = DateTime.UtcNow;
        foreach (var permissionId in distinctPermissionIds)
        {
            dbContext.RolePermissions.Add(new RolePermission
            {
                RoleId = role.Id,
                PermissionId = permissionId,
                AssignedAtUtc = nowUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.RolePermissionsUpdated,
            isSuccess: true,
            userId: actorUserId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { role.Id, role.Name, permissionsCount = distinctPermissionIds.Count },
            cancellationToken: cancellationToken);

        return await GetRoleByIdInternalAsync(appId, role.Id, cancellationToken);
    }

    // =========================================================================
    // 4. User Access Assignment per Application
    // =========================================================================

    public async Task<UserAppRolesResponse> GetUserAppRolesAsync(
        Guid userId,
        Guid appId,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == userId, cancellationToken)
            ?? throw new KeyNotFoundException("Pengguna tidak ditemukan.");

        var app = await dbContext.ClientApps
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Aplikasi klien tidak ditemukan.");

        var assignedRoles = await dbContext.UserAppRoles
            .AsNoTracking()
            .Where(x => x.UserId == userId && x.ClientAppId == appId)
            .Include(x => x.Role)
                .ThenInclude(r => r.RolePermissions)
                    .ThenInclude(rp => rp.Permission)
            .Select(x => x.Role)
            .OrderBy(r => r.Name)
            .ToListAsync(cancellationToken);

        var roleDtos = assignedRoles.Select(MapToRoleDto).ToList();
        var effectivePermissions = roleDtos
            .SelectMany(r => r.Permissions)
            .Select(p => p.Code)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(code => code, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new UserAppRolesResponse(
            UserId: user.Id,
            Username: user.Username,
            ClientAppId: app.Id,
            ClientId: app.ClientId,
            AssignedRoles: roleDtos,
            EffectivePermissionCodes: effectivePermissions);
    }

    public async Task<UserAppRolesResponse> SetUserAppRolesAsync(
        Guid userId,
        Guid appId,
        SetUserAppRolesRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default)
    {
        var userExists = await dbContext.Users.AnyAsync(x => x.Id == userId, cancellationToken);
        if (!userExists)
        {
            throw new KeyNotFoundException("Pengguna tidak ditemukan.");
        }

        await EnsureAppExistsAsync(appId, cancellationToken);

        var distinctRoleIds = request.RoleIds.Distinct().ToList();
        if (distinctRoleIds.Count > 0)
        {
            var validCount = await dbContext.Roles
                .CountAsync(x => x.ClientAppId == appId && distinctRoleIds.Contains(x.Id), cancellationToken);

            if (validCount != distinctRoleIds.Count)
            {
                throw new ArgumentException("Seluruh RoleId wajib berasal dari aplikasi (ClientAppId) yang dituju.");
            }
        }

        var existingAssignments = await dbContext.UserAppRoles
            .Where(x => x.UserId == userId && x.ClientAppId == appId)
            .ToListAsync(cancellationToken);

        if (existingAssignments.Count > 0)
        {
            dbContext.UserAppRoles.RemoveRange(existingAssignments);
        }

        var nowUtc = DateTime.UtcNow;
        foreach (var roleId in distinctRoleIds)
        {
            dbContext.UserAppRoles.Add(new UserAppRole
            {
                UserId = userId,
                ClientAppId = appId,
                RoleId = roleId,
                AssignedAtUtc = nowUtc
            });
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        await auditLogService.LogAsync(
            AuditEventType.UserAppRolesUpdated,
            isSuccess: true,
            userId: userId,
            clientAppId: appId,
            ipAddress: ipAddress,
            userAgent: userAgent,
            detail: new { actorUserId, assignedRoleIds = distinctRoleIds },
            cancellationToken: cancellationToken);

        return await GetUserAppRolesAsync(userId, appId, cancellationToken);
    }

    // =========================================================================
    // 5. Audit Logs
    // =========================================================================

    public async Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(
        AuditLogFilterQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var logsQuery = dbContext.AuditLogs
            .AsNoTracking()
            .Include(x => x.User)
            .Include(x => x.ClientApp)
            .AsQueryable();

        if (query.UserId.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.UserId == query.UserId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.ClientId))
        {
            var clientId = query.ClientId.Trim();
            logsQuery = logsQuery.Where(x => x.ClientApp != null && x.ClientApp.ClientId == clientId);
        }

        if (!string.IsNullOrWhiteSpace(query.EventType) &&
            Enum.TryParse<AuditEventType>(query.EventType.Trim(), ignoreCase: true, out var parsedEventType))
        {
            logsQuery = logsQuery.Where(x => x.EventType == parsedEventType);
        }

        if (query.IsSuccess.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.IsSuccess == query.IsSuccess.Value);
        }

        if (query.DateFromUtc.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.TimestampUtc >= query.DateFromUtc.Value);
        }

        if (query.DateToUtc.HasValue)
        {
            logsQuery = logsQuery.Where(x => x.TimestampUtc <= query.DateToUtc.Value);
        }

        var totalCount = await logsQuery.CountAsync(cancellationToken);

        var items = await logsQuery
            .OrderByDescending(x => x.TimestampUtc)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new AuditLogDto(
                x.Id,
                x.TimestampUtc,
                x.UserId,
                x.User != null ? x.User.Username : null,
                x.ClientAppId,
                x.ClientApp != null ? x.ClientApp.ClientId : null,
                x.EventType.ToString(),
                x.IsSuccess,
                x.IpAddress,
                x.UserAgent,
                x.DetailJson))
            .ToListAsync(cancellationToken);

        var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
        return new PagedResponse<AuditLogDto>(items, page, pageSize, totalCount, totalPages);
    }

    // =========================================================================
    // Helpers
    // =========================================================================

    private async Task EnsureAppExistsAsync(Guid appId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.ClientApps.AnyAsync(x => x.Id == appId, cancellationToken);
        if (!exists)
        {
            throw new KeyNotFoundException("Aplikasi klien (ClientApp) tidak ditemukan.");
        }
    }

    private async Task<RoleDto> GetRoleByIdInternalAsync(
        Guid appId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        var role = await dbContext.Roles
            .AsNoTracking()
            .Include(x => x.RolePermissions)
                .ThenInclude(rp => rp.Permission)
            .FirstOrDefaultAsync(x => x.Id == roleId && x.ClientAppId == appId, cancellationToken)
            ?? throw new KeyNotFoundException("Role tidak ditemukan.");

        return MapToRoleDto(role);
    }

    private static RoleDto MapToRoleDto(Role role)
    {
        var permissions = role.RolePermissions
            .Where(rp => rp.Permission is not null)
            .Select(rp => new PermissionDto(
                rp.Permission.Id,
                rp.Permission.ClientAppId,
                rp.Permission.Code,
                rp.Permission.Description,
                rp.Permission.CreatedAtUtc))
            .OrderBy(p => p.Code)
            .ToList();

        return new RoleDto(
            role.Id,
            role.ClientAppId,
            role.Name,
            role.Description,
            role.IsSystemRole,
            role.CreatedAtUtc,
            permissions);
    }

    private static AdminUserDto MapToAdminUserDto(User user)
    {
        var appAssignments = user.UserAppRoles
            .Where(x => x.ClientApp is not null && x.Role is not null)
            .GroupBy(x => new { x.ClientAppId, x.ClientApp.ClientId, x.ClientApp.Name })
            .OrderBy(g => g.Key.ClientId)
            .Select(g => new UserAppAssignmentSummaryDto(
                g.Key.ClientAppId,
                g.Key.ClientId,
                g.Key.Name,
                g.Select(x => x.Role.Name).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(n => n).ToList()))
            .ToList();

        return new AdminUserDto(
            user.Id,
            user.Username,
            user.Email,
            user.FullName,
            user.IsActive,
            user.IsAccountVerified,
            user.FailedLoginCount,
            user.LockoutEndUtc,
            user.CreatedAtUtc,
            user.UpdatedAtUtc,
            appAssignments);
    }
}

