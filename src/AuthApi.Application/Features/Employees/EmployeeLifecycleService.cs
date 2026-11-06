using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Employees;

public sealed record LifecycleCandidate(Guid UserId, Guid EmployeeId, string EmployeeNumber);

/// <param name="Remaining">Candidates not handled because the batch limit was reached; run again.</param>
public sealed record LifecycleRunResult(bool DryRun, DateOnly AsOf, IReadOnlyList<LifecycleCandidate> Processed,
    int RevokedSessions, int Remaining);

public interface IEmployeeLifecycleService
{
    /// <summary>
    /// Deactivates still-active accounts of employees whose resignation has taken effect and revokes their refresh tokens.
    /// Idempotent (already inactive accounts are skipped). With <paramref name="dryRun"/> nothing is written.
    /// Roles, permissions and employee history are never touched, and a later reactivation of the employee never
    /// reactivates the account.
    /// </summary>
    Task<LifecycleRunResult> ProcessResignationsAsync(bool dryRun, EmployeeActor actor, CancellationToken cancellationToken = default);
}

public sealed class EmployeeLifecycleService(IAppDbContext db, IEmployeeMutationRunner runner, EmployeeMutationSupport support)
    : IEmployeeLifecycleService
{
    public const int BatchLimit = 200;

    public Task<LifecycleRunResult> ProcessResignationsAsync(bool dryRun, EmployeeActor actor,
        CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var today = mutation.BusinessDate;

        var resignedEmployeeIds = await db.EmployeeStatuses.AsNoTracking()
            .Where(x => x.StatusCode == EmployeeStatusCode.Resigned && x.RecordState == EmployeeRecordState.Confirmed
                && x.SupersededAtUtc == null && x.EffectiveFrom <= today && (x.EffectiveToExclusive == null || today < x.EffectiveToExclusive))
            .Select(x => x.EmployeeId).Distinct().ToListAsync(ct);
        var links = await db.UserEmployeeLinks.AsNoTracking().Where(x => resignedEmployeeIds.Contains(x.EmployeeId)).ToListAsync(ct);
        var linkedUserIds = links.Select(x => x.UserId).ToArray();
        var activeUsers = await db.Users.Where(x => linkedUserIds.Contains(x.Id) && x.IsActive).OrderBy(x => x.Id).ToListAsync(ct);

        var batch = activeUsers.Take(BatchLimit).ToList();
        var remaining = activeUsers.Count - batch.Count;
        var batchEmployeeIds = batch.Select(u => links.Single(l => l.UserId == u.Id).EmployeeId).ToArray();
        var employees = await db.Employees.AsNoTracking().Where(x => batchEmployeeIds.Contains(x.Id)).ToListAsync(ct);
        var processed = batch.Select(u =>
        {
            var link = links.Single(l => l.UserId == u.Id);
            return new LifecycleCandidate(u.Id, link.EmployeeId, employees.Single(e => e.Id == link.EmployeeId).EmployeeNumber);
        }).ToList();
        if (dryRun) return new LifecycleRunResult(true, today, processed, 0, remaining);

        var revoked = 0;
        foreach (var user in batch)
        {
            var link = links.Single(l => l.UserId == user.Id);
            user.IsActive = false;
            user.UpdatedAtUtc = mutation.NowUtc;
            var sessions = await db.RefreshTokens.Where(x => x.UserId == user.Id && x.RevokedAtUtc == null && x.ExpiresAtUtc > mutation.NowUtc).ToListAsync(ct);
            foreach (var session in sessions)
            {
                session.RevokedAtUtc = mutation.NowUtc;
                session.RevokeReason = "Employee resignation took effect";
            }
            revoked += sessions.Count;
            await db.SaveChangesAsync(ct);
            await support.AuditAsync(AuditEventType.EmployeeAccountDeactivated, link.EmployeeId, "Resignation took effect",
                new { userId = user.Id, revokedSessions = sessions.Count, asOf = today }, mutation, ct, user.Id);
        }
        return new LifecycleRunResult(false, today, processed, revoked, remaining);
    }, cancellationToken);
}
