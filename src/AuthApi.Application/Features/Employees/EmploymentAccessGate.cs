using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Common.Models;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace AuthApi.Application.Features.Employees;

public sealed record EmploymentAccessResult(bool IsAllowed, string? Reason)
{
    public static EmploymentAccessResult Allowed { get; } = new(true, null);
}

/// <summary>
/// Decides whether the employee linked to an account may still use it (D-02). Accounts without a link are never affected.
/// This is evaluated on every login, refresh, realtime verification and permission lookup, so a resignation that has taken
/// effect is refused even if the lifecycle job has not run yet. It never changes roles, permissions or accounts.
/// </summary>
public interface IEmploymentAccessGate
{
    Task<EmploymentAccessResult> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed class EmploymentAccessGate(IAppDbContext db, TimeProvider clock, IOptions<SecuritySettings> options) : IEmploymentAccessGate
{
    public const string BlockedReason = "Pegawai yang terhubung dengan akun ini sudah tidak aktif.";
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById("Asia/Jakarta");

    public async Task<EmploymentAccessResult> EvaluateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        // Activation is an operator decision (planning §13: after link/status data is validated and the pilot).
        if (!options.Value.EnforceEmploymentStatus) return EmploymentAccessResult.Allowed;

        var employeeId = await db.UserEmployeeLinks.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.EmployeeId).SingleOrDefaultAsync(cancellationToken);
        if (employeeId is null) return EmploymentAccessResult.Allowed;

        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(clock.GetUtcNow().UtcDateTime, BusinessTimeZone));
        var effective = await db.EmployeeStatuses.AsNoTracking().Where(x => x.EmployeeId == employeeId
                && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= today && (x.EffectiveToExclusive == null || today < x.EffectiveToExclusive))
            .Select(x => x.StatusCode).ToListAsync(cancellationToken);

        // Fail closed: exactly one effective ACTIVE status is required for a linked employee.
        return effective.Count == 1 && effective[0] == EmployeeStatusCode.Active
            ? EmploymentAccessResult.Allowed
            : new EmploymentAccessResult(false, BlockedReason);
    }
}
