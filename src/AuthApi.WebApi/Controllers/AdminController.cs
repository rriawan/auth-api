using System.IdentityModel.Tokens.Jwt;
using AuthApi.Application.Features.Admin;
using AuthApi.Application.Features.Admin.DTOs;
using AuthApi.Application.Features.Auth.DTOs;
using AuthApi.Domain.Constants;
using AuthApi.WebApi.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace AuthApi.WebApi.Controllers;

[ApiController]
[Route("api/v1/admin")]
public class AdminController(IAdminService adminService) : ControllerBase
{
    // =========================================================================
    // 1. Client Apps & API Keys (/api/v1/admin/apps)
    // =========================================================================

    [HttpGet("apps")]
    [RequireAdminPermission(SystemConstants.Permissions.AppsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<ClientAppDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<ClientAppDto>>> GetClientApps(CancellationToken cancellationToken)
    {
        var apps = await adminService.GetClientAppsAsync(cancellationToken);
        return Ok(apps);
    }

    [HttpGet("apps/{id:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.AppsRead)]
    [ProducesResponseType(typeof(ClientAppDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientAppDto>> GetClientAppById(Guid id, CancellationToken cancellationToken)
    {
        var app = await adminService.GetClientAppByIdAsync(id, cancellationToken);
        return Ok(app);
    }

    [HttpPost("apps")]
    [RequireAdminPermission(SystemConstants.Permissions.AppsWrite)]
    [ProducesResponseType(typeof(ClientAppWithRawKeyResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ClientAppWithRawKeyResponse>> CreateClientApp(
        [FromBody] CreateClientAppRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.CreateClientAppAsync(
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return CreatedAtAction(nameof(GetClientAppById), new { id = result.App.Id }, result);
    }

    [HttpPut("apps/{id:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.AppsWrite)]
    [ProducesResponseType(typeof(ClientAppDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientAppDto>> UpdateClientApp(
        Guid id,
        [FromBody] UpdateClientAppRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.UpdateClientAppAsync(
            id,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("apps/{id:guid}/rotate-key")]
    [RequireAdminPermission(SystemConstants.Permissions.AppsRotateKey)]
    [ProducesResponseType(typeof(ClientAppWithRawKeyResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ClientAppWithRawKeyResponse>> RotateClientAppApiKey(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await adminService.RotateClientAppApiKeyAsync(
            id,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    // =========================================================================
    // 2. Users Management (/api/v1/admin/users)
    // =========================================================================

    [HttpGet("users")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersRead)]
    [ProducesResponseType(typeof(PagedResponse<AdminUserDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AdminUserDto>>> GetUsers(
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var result = await adminService.GetUsersAsync(search, page, pageSize, cancellationToken);
        return Ok(result);
    }

    [HttpGet("users/{id:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersRead)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> GetUserById(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminService.GetUserByIdAsync(id, cancellationToken);
        return Ok(result);
    }

    [HttpPost("users")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersWrite)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<AdminUserDto>> CreateUser(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.CreateUserAsync(
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return CreatedAtAction(nameof(GetUserById), new { id = result.Id }, result);
    }

    [HttpPut("users/{id:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersWrite)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UpdateUser(
        Guid id,
        [FromBody] UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.UpdateUserAsync(
            id,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("users/{id:guid}/unlock")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersUnlock)]
    [ProducesResponseType(typeof(AdminUserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AdminUserDto>> UnlockUser(Guid id, CancellationToken cancellationToken)
    {
        var result = await adminService.UnlockUserAsync(
            id,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    [HttpPost("users/{id:guid}/revoke-sessions")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersRevokeSessions)]
    [ProducesResponseType(typeof(RevokeSessionsResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RevokeSessionsResponse>> RevokeUserSessions(
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await adminService.RevokeUserSessionsAsync(
            id,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    // =========================================================================
    // 3. Roles & Permissions per Aplikasi (/api/v1/admin/apps/{appId}/...)
    // =========================================================================

    [HttpGet("apps/{appId:guid}/permissions")]
    [RequireAdminPermission(SystemConstants.Permissions.PermissionsRead)]
    [ProducesResponseType(typeof(IReadOnlyList<PermissionDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PermissionDto>>> GetAppPermissions(
        Guid appId,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetAppPermissionsAsync(appId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("apps/{appId:guid}/permissions")]
    [RequireAdminPermission(SystemConstants.Permissions.PermissionsWrite)]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<PermissionDto>> CreateAppPermission(
        Guid appId,
        [FromBody] CreatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.CreateAppPermissionAsync(
            appId,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("apps/{appId:guid}/permissions/{permissionId:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.PermissionsWrite)]
    [ProducesResponseType(typeof(PermissionDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PermissionDto>> UpdateAppPermission(
        Guid appId,
        Guid permissionId,
        [FromBody] UpdatePermissionRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.UpdateAppPermissionAsync(
            appId,
            permissionId,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("apps/{appId:guid}/permissions/{permissionId:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.PermissionsWrite)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> DeleteAppPermission(
        Guid appId,
        Guid permissionId,
        CancellationToken cancellationToken)
    {
        await adminService.DeleteAppPermissionAsync(
            appId,
            permissionId,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(new MessageResponse("Permission berhasil dihapus."));
    }

    [HttpGet("apps/{appId:guid}/roles")]
    [RequireAdminPermission(SystemConstants.Permissions.RolesRead)]
    [ProducesResponseType(typeof(IReadOnlyList<RoleDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<RoleDto>>> GetAppRoles(
        Guid appId,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetAppRolesAsync(appId, cancellationToken);
        return Ok(result);
    }

    [HttpPost("apps/{appId:guid}/roles")]
    [RequireAdminPermission(SystemConstants.Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<RoleDto>> CreateAppRole(
        Guid appId,
        [FromBody] CreateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.CreateAppRoleAsync(
            appId,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return StatusCode(StatusCodes.Status201Created, result);
    }

    [HttpPut("apps/{appId:guid}/roles/{roleId:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> UpdateAppRole(
        Guid appId,
        Guid roleId,
        [FromBody] UpdateRoleRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.UpdateAppRoleAsync(
            appId,
            roleId,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    [HttpDelete("apps/{appId:guid}/roles/{roleId:guid}")]
    [RequireAdminPermission(SystemConstants.Permissions.RolesWrite)]
    [ProducesResponseType(typeof(MessageResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MessageResponse>> DeleteAppRole(
        Guid appId,
        Guid roleId,
        CancellationToken cancellationToken)
    {
        await adminService.DeleteAppRoleAsync(
            appId,
            roleId,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(new MessageResponse("Role berhasil dihapus."));
    }

    [HttpPut("apps/{appId:guid}/roles/{roleId:guid}/permissions")]
    [RequireAdminPermission(SystemConstants.Permissions.RolesWrite)]
    [ProducesResponseType(typeof(RoleDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<RoleDto>> SetRolePermissions(
        Guid appId,
        Guid roleId,
        [FromBody] SetRolePermissionsRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.SetRolePermissionsAsync(
            appId,
            roleId,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    // =========================================================================
    // 4. User Access Assignment (/api/v1/admin/users/{userId}/apps/{appId}/roles)
    // =========================================================================

    [HttpGet("users/{userId:guid}/apps/{appId:guid}/roles")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersRead)]
    [ProducesResponseType(typeof(UserAppRolesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserAppRolesResponse>> GetUserAppRoles(
        Guid userId,
        Guid appId,
        CancellationToken cancellationToken)
    {
        var result = await adminService.GetUserAppRolesAsync(userId, appId, cancellationToken);
        return Ok(result);
    }

    [HttpPut("users/{userId:guid}/apps/{appId:guid}/roles")]
    [RequireAdminPermission(SystemConstants.Permissions.UsersWrite)]
    [ProducesResponseType(typeof(UserAppRolesResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserAppRolesResponse>> SetUserAppRoles(
        Guid userId,
        Guid appId,
        [FromBody] SetUserAppRolesRequest request,
        CancellationToken cancellationToken)
    {
        var result = await adminService.SetUserAppRolesAsync(
            userId,
            appId,
            request,
            TryGetActorUserId(),
            GetClientIpAddress(),
            Request.Headers.UserAgent.ToString(),
            cancellationToken);

        return Ok(result);
    }

    // =========================================================================
    // 5. Audit Logs (/api/v1/admin/audit-logs)
    // =========================================================================

    [HttpGet("audit-logs")]
    [RequireAdminPermission(SystemConstants.Permissions.AuditLogsRead)]
    [ProducesResponseType(typeof(PagedResponse<AuditLogDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResponse<AuditLogDto>>> GetAuditLogs(
        [FromQuery] Guid? userId = null,
        [FromQuery] string? clientId = null,
        [FromQuery] string? eventType = null,
        [FromQuery] bool? isSuccess = null,
        [FromQuery] DateTime? dateFrom = null,
        [FromQuery] DateTime? dateTo = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 25,
        CancellationToken cancellationToken = default)
    {
        var query = new AuditLogFilterQuery(
            UserId: userId,
            ClientId: clientId,
            EventType: eventType,
            IsSuccess: isSuccess,
            DateFromUtc: dateFrom,
            DateToUtc: dateTo,
            Page: page,
            PageSize: pageSize);

        var result = await adminService.GetAuditLogsAsync(query, cancellationToken);
        return Ok(result);
    }

    private Guid? TryGetActorUserId()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        return Guid.TryParse(sub, out var userId) ? userId : null;
    }

    private string? GetClientIpAddress() =>
        HttpContext.Connection.RemoteIpAddress?.ToString();
}

