using System.Security.Cryptography;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Constants;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Employees;

public sealed class EmployeeMutationSupport(IAppDbContext db, IEmployeeMutationRunner runner,
    IAuditLogService audit, TimeProvider clock)
{
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");

    public void RequireTransaction()
    {
        if (!runner.IsActive) throw new InvalidOperationException("Employee writes require the mutation transaction boundary.");
    }

    public Task<AuthorizedEmployeeMutation> AuthorizeAsync(EmployeeActor actor, CancellationToken cancellationToken)
    {
        RequireTransaction();
        return AuthorizeReadAsync(actor, cancellationToken);
    }

    /// <summary>Fresh database authorization without a write transaction (read endpoints). Never trusts token claims.</summary>
    public async Task<AuthorizedEmployeeMutation> AuthorizeReadAsync(EmployeeActor actor, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == actor.UserId, cancellationToken);
        if (user is null || !user.IsActive || !user.IsAccountVerified || user.IsLockedOut(now))
            throw new ForbiddenAccessException("An active verified administrator is required.");
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, BusinessTimeZone));
        var linkedEmployeeId = await db.UserEmployeeLinks.AsNoTracking().Where(x => x.UserId == actor.UserId)
            .Select(x => (Guid?)x.EmployeeId).SingleOrDefaultAsync(cancellationToken);
        if (linkedEmployeeId.HasValue)
        {
            var states = await db.EmployeeStatuses.AsNoTracking().Where(x => x.EmployeeId == linkedEmployeeId.Value
                && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= today && (x.EffectiveToExclusive == null || today < x.EffectiveToExclusive))
                .Select(x => x.StatusCode).ToListAsync(cancellationToken);
            if (states.Count != 1 || states[0] != EmployeeStatusCode.Active)
                throw new ForbiddenAccessException("The administrator's linked employee is not active.");
        }

        // Deny by default until P3/D-06 defines finer profile-administration permissions.
        // This is fresh database membership, never the caller's role claims.
        var appId = await db.UserAppRoles.AsNoTracking()
            .Where(x => x.UserId == actor.UserId && x.ClientApp.ClientId == SystemConstants.DefaultAdminClientId
                && x.ClientApp.IsActive && x.Role.ClientAppId == x.ClientAppId && x.Role.IsSystemRole
                && x.Role.Name == SystemConstants.DefaultSuperAdminRole)
            .Select(x => x.ClientAppId).FirstOrDefaultAsync(cancellationToken);
        if (appId == Guid.Empty) throw new ForbiddenAccessException("Internal auth-admin system SuperAdmin membership is required.");

        return new AuthorizedEmployeeMutation(actor.UserId, appId,
            today, now, actor.IpAddress, actor.UserAgent);
    }

    public async Task<Employee> LoadEmployeeAsync(Guid employeeId, string? expectedVersion, CancellationToken cancellationToken)
    {
        RequireTransaction();
        var employee = await db.Employees.SingleOrDefaultAsync(x => x.Id == employeeId, cancellationToken)
            ?? throw new KeyNotFoundException("Employee not found.");
        CheckVersion(employee, expectedVersion);
        return employee;
    }

    public static void CheckVersion(Employee employee, string? expectedVersion) => CheckRowVersion(employee.RowVersion, expectedVersion);

    public static void CheckRowVersion(byte[] rowVersion, string? expectedVersion)
    {
        if (string.IsNullOrWhiteSpace(expectedVersion)) throw new EmployeePreconditionException(true);
        if (expectedVersion.Length < 3 || expectedVersion[0] != '"' || expectedVersion[^1] != '"')
            throw new ArgumentException("Supply one quoted, strong employee ETag.", nameof(expectedVersion));
        byte[] supplied;
        try { supplied = Convert.FromBase64String(expectedVersion[1..^1]); }
        catch (FormatException) { throw new ArgumentException("Invalid employee ETag.", nameof(expectedVersion)); }
        if (supplied.Length != 8) throw new ArgumentException("Invalid employee ETag length.", nameof(expectedVersion));
        if (!CryptographicOperations.FixedTimeEquals(supplied, rowVersion))
            throw new EmployeePreconditionException(false);
    }

    public static string Text(string value, int maxLength, string name)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim().Length > maxLength)
            throw new ArgumentException($"{name} is required and must not exceed {maxLength} characters.", name);
        return value.Trim();
    }

    public static string? OptionalText(string? value, int maxLength, string name) =>
        string.IsNullOrWhiteSpace(value) ? null : Text(value, maxLength, name);

    public static void NoBackdate(DateOnly date, AuthorizedEmployeeMutation mutation)
    {
        if (date < mutation.BusinessDate) throw new ArgumentException("Backdated employee changes are not supported in this release.");
    }

    public void Touch(Employee employee, AuthorizedEmployeeMutation mutation)
    {
        RequireTransaction();
        employee.UpdatedAtUtc = mutation.NowUtc;
        employee.UpdatedByUserId = mutation.ActorUserId;
        // Force an aggregate UPDATE even when actor and clock instant repeat.
        // SQL Server must issue a new rowversion for child-only mutations too.
        runner.MarkEmployeeChanged(employee);
    }

    public async Task AuditAsync(AuditEventType eventType, Guid employeeId, string reason,
        object? data, AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken, Guid? targetUserId = null)
    {
        RequireTransaction();
        await audit.LogAsync(eventType, true, userId: targetUserId, clientAppId: mutation.AdminClientAppId,
            ipAddress: mutation.IpAddress, userAgent: mutation.UserAgent,
            detail: new { actorUserId = mutation.ActorUserId, employeeId, reason, data }, cancellationToken: cancellationToken);
    }

    public void MarkMasterChanged(object master)
    {
        RequireTransaction();
        runner.MarkMasterChanged(master);
    }

    public static string ETag(byte[] rowVersion) => '"' + Convert.ToBase64String(rowVersion) + '"';

    public async Task AuditOrganizationAsync(AuditEventType eventType, Guid entityId, string reason,
        object? data, AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken)
    {
        RequireTransaction();
        await audit.LogAsync(eventType, true, clientAppId: mutation.AdminClientAppId,
            ipAddress: mutation.IpAddress, userAgent: mutation.UserAgent,
            detail: new { actorUserId = mutation.ActorUserId, entityId, reason, data }, cancellationToken: cancellationToken);
    }

    public async Task<EmployeeSummary> SummaryAsync(Employee employee, AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken)
    {
        RequireTransaction();
        var status = await db.EmployeeStatuses.AsNoTracking()
            .Where(x => x.EmployeeId == employee.Id && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= mutation.BusinessDate && (x.EffectiveToExclusive == null || mutation.BusinessDate < x.EffectiveToExclusive))
            .Select(x => (EmployeeStatusCode?)x.StatusCode).SingleOrDefaultAsync(cancellationToken);
        var userId = await db.UserEmployeeLinks.AsNoTracking().Where(x => x.EmployeeId == employee.Id)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(cancellationToken);
        return new EmployeeSummary(employee.Id, employee.EmployeeNumber, employee.EmployeeFullName,
            '"' + Convert.ToBase64String(employee.RowVersion) + '"', status, userId);
    }
}
