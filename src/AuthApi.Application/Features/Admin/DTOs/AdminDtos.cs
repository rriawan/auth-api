using System.ComponentModel.DataAnnotations;

namespace AuthApi.Application.Features.Admin.DTOs;

public sealed record PagedResponse<T>(
    IReadOnlyList<T> Items,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages);

// 1. ClientApps & API Keys
public sealed record CreateClientAppRequest(
    [Required, MaxLength(100)] string ClientId,
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string? Description = null,
    [MaxLength(2000)] string? AllowedOrigins = null,
    bool IsActive = true);

public sealed record UpdateClientAppRequest(
    [Required, MaxLength(200)] string Name,
    [MaxLength(500)] string? Description,
    [MaxLength(2000)] string? AllowedOrigins,
    bool IsActive);

public sealed record ClientAppDto(
    Guid Id,
    string ClientId,
    string Name,
    string? Description,
    string ApiKeyPrefix,
    string? AllowedOrigins,
    bool IsActive,
    int RolesCount,
    int PermissionsCount,
    int AssignedUsersCount,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc);

public sealed record ClientAppWithRawKeyResponse(
    ClientAppDto App,
    string RawApiKey,
    string Notice);

// 2. User Management
public sealed record CreateUserRequest(
    [Required, MaxLength(100)] string Username,
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MinLength(8), MaxLength(256)] string Password,
    [Required, MaxLength(200)] string FullName,
    bool IsActive = true,
    bool IsAccountVerified = true);

public sealed record UpdateUserRequest(
    [Required, EmailAddress, MaxLength(256)] string Email,
    [Required, MaxLength(200)] string FullName,
    bool IsActive,
    bool IsAccountVerified);

public sealed record UserAppAssignmentSummaryDto(
    Guid ClientAppId,
    string ClientId,
    string AppName,
    IReadOnlyList<string> Roles);

public sealed record AdminUserDto(
    Guid Id,
    string Username,
    string Email,
    string FullName,
    bool IsActive,
    bool IsAccountVerified,
    int FailedLoginCount,
    DateTime? LockoutEndUtc,
    DateTime CreatedAtUtc,
    DateTime? UpdatedAtUtc,
    IReadOnlyList<UserAppAssignmentSummaryDto> AppAssignments);

public sealed record RevokeSessionsResponse(
    Guid UserId,
    int RevokedSessionsCount,
    string Message);

// 3. Permissions & Roles per ClientApp
public sealed record CreatePermissionRequest(
    [Required, MaxLength(150)] string Code,
    [MaxLength(500)] string? Description = null);

public sealed record UpdatePermissionRequest(
    [Required, MaxLength(150)] string Code,
    [MaxLength(500)] string? Description = null);

public sealed record PermissionDto(
    Guid Id,
    Guid ClientAppId,
    string Code,
    string? Description,
    DateTime CreatedAtUtc);

public sealed record CreateRoleRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null,
    IReadOnlyCollection<Guid>? PermissionIds = null);

public sealed record UpdateRoleRequest(
    [Required, MaxLength(100)] string Name,
    [MaxLength(500)] string? Description = null);

public sealed record SetRolePermissionsRequest(
    [Required] IReadOnlyCollection<Guid> PermissionIds);

public sealed record RoleDto(
    Guid Id,
    Guid ClientAppId,
    string Name,
    string? Description,
    bool IsSystemRole,
    DateTime CreatedAtUtc,
    IReadOnlyList<PermissionDto> Permissions);

// 4. User Access Assignment per App
public sealed record SetUserAppRolesRequest(
    [Required] IReadOnlyCollection<Guid> RoleIds);

public sealed record UserAppRolesResponse(
    Guid UserId,
    string Username,
    Guid ClientAppId,
    string ClientId,
    IReadOnlyList<RoleDto> AssignedRoles,
    IReadOnlyList<string> EffectivePermissionCodes);

// 5. Audit Logs
public sealed record AuditLogFilterQuery(
    Guid? UserId = null,
    string? ClientId = null,
    string? EventType = null,
    bool? IsSuccess = null,
    DateTime? DateFromUtc = null,
    DateTime? DateToUtc = null,
    int Page = 1,
    int PageSize = 25);

public sealed record AuditLogDto(
    Guid Id,
    DateTime TimestampUtc,
    Guid? UserId,
    string? Username,
    Guid? ClientAppId,
    string? ClientId,
    string EventType,
    bool IsSuccess,
    string? IpAddress,
    string? UserAgent,
    string? DetailJson);
