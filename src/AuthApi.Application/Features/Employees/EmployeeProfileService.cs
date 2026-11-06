using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Employees;

public sealed class EmployeeProfileService(
    IAppDbContext db,
    IEmployeeMutationRunner runner,
    EmployeeMutationSupport support,
    IEmployeeScheduleWriter schedules) : IEmployeeProfileService
{
    public Task<EmployeeMutationResult> CreateAsync(CreateEmployeeCommand command, EmployeeActor actor,
        CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var number = EmployeeMutationSupport.Text(command.EmployeeNumber, 50, nameof(command.EmployeeNumber));
        var name = EmployeeMutationSupport.Text(command.EmployeeFullName, 200, nameof(command.EmployeeFullName));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        EmployeeMutationSupport.NoBackdate(command.EffectiveFrom, mutation);
        if (await db.Employees.AnyAsync(x => x.EmployeeNumber == number, ct))
            throw new InvalidOperationException("An employee with this employee number already exists.");

        var employee = new Employee
        {
            EmployeeNumber = number,
            EmployeeFullName = name,
            CreatedAtUtc = mutation.NowUtc,
            CreatedByUserId = mutation.ActorUserId,
            UpdatedAtUtc = mutation.NowUtc,
            UpdatedByUserId = mutation.ActorUserId
        };
        db.Employees.Add(employee);
        db.EmployeeStatuses.Add(new EmployeeStatus
        {
            EmployeeId = employee.Id,
            StatusCode = EmployeeStatusCode.Active,
            EffectiveFrom = command.EffectiveFrom,
            RecordState = EmployeeRecordState.Confirmed,
            RevisionNumber = 1,
            ChangeReason = reason,
            CreatedAtUtc = mutation.NowUtc,
            CreatedByUserId = mutation.ActorUserId
        });
        support.Touch(employee, mutation);
        await db.SaveChangesAsync(ct);
        await schedules.ValidateAsync(employee.Id, mutation, ct);
        await support.AuditAsync(AuditEventType.EmployeeCreated, employee.Id, reason,
            new { employee.Id, effectiveFrom = command.EffectiveFrom }, mutation, ct);
        return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct));
    }, cancellationToken);

    public Task<EmployeeMutationResult> UpdateIdentityAsync(Guid employeeId, UpdateEmployeeIdentityCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
        var number = EmployeeMutationSupport.Text(command.EmployeeNumber, 50, nameof(command.EmployeeNumber));
        var name = EmployeeMutationSupport.Text(command.EmployeeFullName, 200, nameof(command.EmployeeFullName));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        if (await db.Employees.AnyAsync(x => x.Id != employeeId && x.EmployeeNumber == number, ct))
            throw new InvalidOperationException("An employee with this employee number already exists.");
        var changedFields = new List<string>(2);
        if (!String.Equals(employee.EmployeeNumber, number, StringComparison.Ordinal)) changedFields.Add(nameof(Employee.EmployeeNumber));
        if (!String.Equals(employee.EmployeeFullName, name, StringComparison.Ordinal)) changedFields.Add(nameof(Employee.EmployeeFullName));
        employee.EmployeeNumber = number;
        employee.EmployeeFullName = name;
        support.Touch(employee, mutation);
        await db.SaveChangesAsync(ct);
        await schedules.ValidateAsync(employeeId, mutation, ct);
        await support.AuditAsync(AuditEventType.EmployeeIdentityUpdated, employeeId, reason,
            new { changedFields }, mutation, ct);
        return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct));
    }, cancellationToken);

    public Task<EmployeeMutationResult> ChangeStatusAsync(Guid employeeId, ChangeEmployeeStatusCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
        EmployeeMutationSupport.NoBackdate(command.EffectiveFrom, mutation);
        if (!Enum.IsDefined(command.StatusCode)) throw new ArgumentOutOfRangeException(nameof(command.StatusCode));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));

        var current = await db.EmployeeStatuses.Where(x => x.EmployeeId == employeeId &&
                x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null)
            .OrderBy(x => x.EffectiveFrom).ToListAsync(ct);
        var source = current.SingleOrDefault(x => x.EffectiveFrom <= command.EffectiveFrom &&
            (x.EffectiveToExclusive == null || command.EffectiveFrom < x.EffectiveToExclusive));
        if (source is null) throw new InvalidOperationException("No confirmed employee status covers the requested effective date.");
        if (source.StatusCode == command.StatusCode) throw new InvalidOperationException("The employee already has this status on the requested date.");
        if (current.Any(x => x.Id != source.Id && x.EffectiveFrom >= command.EffectiveFrom))
            throw new InvalidOperationException("A later confirmed status exists; revise that timeline explicitly first.");

        var maxRevision = await db.EmployeeStatuses.Where(x => x.EmployeeId == employeeId)
            .Select(x => (int?)x.RevisionNumber).MaxAsync(ct) ?? 0;
        source.SupersededAtUtc = mutation.NowUtc;
        source.SupersededByUserId = mutation.ActorUserId;
        source.SupersededReason = reason;
        // Release the filtered current-revision key inside the surrounding transaction.
        await db.SaveChangesAsync(ct);
        if (source.EffectiveFrom < command.EffectiveFrom)
        {
            db.EmployeeStatuses.Add(new EmployeeStatus
            {
                EmployeeId = employeeId, StatusCode = source.StatusCode, EffectiveFrom = source.EffectiveFrom,
                EffectiveToExclusive = command.EffectiveFrom, RecordState = EmployeeRecordState.Confirmed,
                RevisionNumber = ++maxRevision, ChangeReason = reason, DecisionReference = source.DecisionReference,
                CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
            });
        }
        var changed = new EmployeeStatus
        {
            EmployeeId = employeeId, StatusCode = command.StatusCode, EffectiveFrom = command.EffectiveFrom,
            EffectiveToExclusive = null, RecordState = EmployeeRecordState.Confirmed,
            RevisionNumber = ++maxRevision, ChangeReason = reason, DecisionReference = reference,
            CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
        };
        db.EmployeeStatuses.Add(changed);
        support.Touch(employee, mutation);
        await db.SaveChangesAsync(ct);

        if (command.StatusCode == EmployeeStatusCode.Resigned)
            await schedules.CloseForResignationAsync(employeeId, command.EffectiveFrom, reason, mutation, ct);
        else
            await schedules.ValidateAsync(employeeId, mutation, ct);
        await support.AuditAsync(AuditEventType.EmployeeStatusChanged, employeeId, reason,
            new { previous = source.StatusCode, current = command.StatusCode, command.EffectiveFrom,
                command.DecisionReference }, mutation, ct);
        return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct), changed.Id);
    }, cancellationToken);

    public Task<EmployeeMutationResult> LinkUserAsync(Guid employeeId, Guid userId, string reason,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
        reason = EmployeeMutationSupport.Text(reason, 500, nameof(reason));
        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");
        var currentLink = await db.UserEmployeeLinks.SingleOrDefaultAsync(x => x.EmployeeId == employeeId, ct);
        if (currentLink is not null)
            throw new InvalidOperationException(currentLink.UserId == userId ? "User is already linked to this employee." : "Employee is already linked to another user.");
        if (await db.UserEmployeeLinks.AnyAsync(x => x.UserId == userId, ct))
            throw new InvalidOperationException("User is already linked to another employee.");
        if (await HasCurrentOrFutureResignationAsync(employeeId, mutation.BusinessDate, ct))
            throw new InvalidOperationException("A current or scheduled resignation prevents linking.");
        var active = await db.EmployeeStatuses.AnyAsync(x => x.EmployeeId == employeeId && x.StatusCode == EmployeeStatusCode.Active &&
            x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null &&
            x.EffectiveFrom <= mutation.BusinessDate && (x.EffectiveToExclusive == null || mutation.BusinessDate < x.EffectiveToExclusive), ct);
        if (!active) throw new InvalidOperationException("Only a currently active employee can be linked.");
        db.UserEmployeeLinks.Add(new UserEmployeeLink
        {
            UserId = userId, EmployeeId = employeeId, LinkedAtUtc = mutation.NowUtc, LinkedByUserId = mutation.ActorUserId
        });
        support.Touch(employee, mutation);
        await db.SaveChangesAsync(ct);
        await support.AuditAsync(AuditEventType.UserEmployeeLinked, employeeId, reason,
            new { userId }, mutation, ct, userId);
        return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct));
    }, cancellationToken);

    public Task<EmployeeMutationResult> UnlinkUserAsync(Guid employeeId, Guid userId, string reason,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
        reason = EmployeeMutationSupport.Text(reason, 500, nameof(reason));
        var link = await db.UserEmployeeLinks.SingleOrDefaultAsync(x => x.EmployeeId == employeeId, ct)
            ?? throw new InvalidOperationException("Employee has no linked user.");
        if (link.UserId != userId) throw new InvalidOperationException("The requested user is not linked to this employee.");
        if (await HasCurrentOrFutureResignationAsync(employeeId, mutation.BusinessDate, ct))
            throw new InvalidOperationException("A current or scheduled resignation prevents unlinking.");
        db.UserEmployeeLinks.Remove(link);
        support.Touch(employee, mutation);
        await db.SaveChangesAsync(ct);
        await support.AuditAsync(AuditEventType.UserEmployeeUnlinked, employeeId, reason,
            new { userId }, mutation, ct, userId);
        return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct));
    }, cancellationToken);

    private Task<bool> HasCurrentOrFutureResignationAsync(Guid employeeId, DateOnly businessDate, CancellationToken ct) =>
        db.EmployeeStatuses.AnyAsync(x => x.EmployeeId == employeeId && x.StatusCode == EmployeeStatusCode.Resigned &&
            x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null &&
            (x.EffectiveToExclusive == null || x.EffectiveToExclusive > businessDate), ct);
}
