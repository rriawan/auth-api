using AuthApi.Application.Features.Admin.DTOs;

namespace AuthApi.Application.Features.Admin;

public interface IAdminService
{
    // 1. Client Apps & API Keys
    Task<IReadOnlyList<ClientAppDto>> GetClientAppsAsync(CancellationToken cancellationToken = default);
    Task<ClientAppDto> GetClientAppByIdAsync(Guid appId, CancellationToken cancellationToken = default);
    Task<ClientAppWithRawKeyResponse> CreateClientAppAsync(
        CreateClientAppRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<ClientAppDto> UpdateClientAppAsync(
        Guid appId,
        UpdateClientAppRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<ClientAppWithRawKeyResponse> RotateClientAppApiKeyAsync(
        Guid appId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    // 2. Users Management
    Task<PagedResponse<AdminUserDto>> GetUsersAsync(
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);
    Task<AdminUserDto> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<AdminUserDto> CreateUserAsync(
        CreateUserRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<AdminUserDto> UpdateUserAsync(
        Guid userId,
        UpdateUserRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<AdminUserDto> UnlockUserAsync(
        Guid userId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<RevokeSessionsResponse> RevokeUserSessionsAsync(
        Guid userId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    // 3. Permissions & Roles per Application
    Task<IReadOnlyList<PermissionDto>> GetAppPermissionsAsync(
        Guid appId,
        CancellationToken cancellationToken = default);
    Task<PermissionDto> CreateAppPermissionAsync(
        Guid appId,
        CreatePermissionRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<PermissionDto> UpdateAppPermissionAsync(
        Guid appId,
        Guid permissionId,
        UpdatePermissionRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task DeleteAppPermissionAsync(
        Guid appId,
        Guid permissionId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RoleDto>> GetAppRolesAsync(
        Guid appId,
        CancellationToken cancellationToken = default);
    Task<RoleDto> CreateAppRoleAsync(
        Guid appId,
        CreateRoleRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<RoleDto> UpdateAppRoleAsync(
        Guid appId,
        Guid roleId,
        UpdateRoleRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task DeleteAppRoleAsync(
        Guid appId,
        Guid roleId,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);
    Task<RoleDto> SetRolePermissionsAsync(
        Guid appId,
        Guid roleId,
        SetRolePermissionsRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    // 4. User Access Assignment per Application
    Task<UserAppRolesResponse> GetUserAppRolesAsync(
        Guid userId,
        Guid appId,
        CancellationToken cancellationToken = default);
    Task<UserAppRolesResponse> SetUserAppRolesAsync(
        Guid userId,
        Guid appId,
        SetUserAppRolesRequest request,
        Guid? actorUserId,
        string? ipAddress,
        string? userAgent,
        CancellationToken cancellationToken = default);

    // 5. Audit Logs
    Task<PagedResponse<AuditLogDto>> GetAuditLogsAsync(
        AuditLogFilterQuery query,
        CancellationToken cancellationToken = default);
}

