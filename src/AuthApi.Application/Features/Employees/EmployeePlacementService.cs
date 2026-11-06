using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Domain.Rules.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Employees;

/// <summary>Transactional employee placement and acting-assignment use cases.</summary>
public sealed class EmployeePlacementService(IAppDbContext db, IEmployeeMutationRunner runner,
    EmployeeMutationSupport support) : IEmployeePlacementService, IEmployeeScheduleWriter
{
    public Task<EmployeeMutationResult> AssignDefinitiveAsync(Guid employeeId, CreateDefinitiveAssignmentCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) =>
        runner.ExecuteAsync(async ct =>
        {
            var mutation = await support.AuthorizeAsync(actor, ct);
            var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
            EmployeeMutationSupport.NoBackdate(command.EffectiveFrom, mutation);
            var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
            var period = new EffectivePeriod(command.EffectiveFrom, command.EffectiveToExclusive);
            _ = period;
            await RequireActiveEmployeeAsync(employeeId, new EffectivePeriod(command.EffectiveFrom, command.EffectiveToExclusive), ct);
            var position = await RequirePositionAsync(command.PositionId, ct);
            var current = await db.EmployeePositions.Where(x => (x.EmployeeId == employeeId || x.PositionId == command.PositionId)
                && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(ct);
            var row = NewPlacement(employeeId, command.PositionId, command.IsPrimary, command.EffectiveFrom,
                command.EffectiveToExclusive, reason, command.DecisionReference, mutation);
            db.EmployeePositions.Add(row);
            var proposed = current.Append(row).ToArray();
            var capacities = await LoadCapacityPeriodsAsync(proposed.Select(x => x.PositionId), ct);
            PlacementScheduleRules.ValidateDefinitiveSchedule(proposed, capacities);
            await ValidateAsync(employeeId, mutation, ct);
            support.Touch(employee, mutation);
            await support.AuditAsync(AuditEventType.EmployeePositionAssigned, employeeId, reason,
                new { recordId = row.Id, positionId = position.Id, row.IsPrimary, row.EffectiveFrom, row.EffectiveToExclusive }, mutation, ct);
            return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct), row.Id);
        }, cancellationToken);

    public Task<EmployeeMutationResult> TransferAsync(Guid employeeId, TransferEmployeeCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) =>
        runner.ExecuteAsync(async ct =>
        {
            var mutation = await support.AuthorizeAsync(actor, ct);
            var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
            EmployeeMutationSupport.NoBackdate(command.EffectiveFrom, mutation);
            var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
            var source = await CurrentPlacementAsync(employeeId, command.SourceRecordId, ct);
            var target = await RequirePositionAsync(command.TargetPositionId, ct);
            var sourcePeriod = new EffectivePeriod(source.EffectiveFrom, source.EffectiveToExclusive);
            if (!sourcePeriod.Contains(command.EffectiveFrom)) throw new ArgumentException("Transfer date must fall within the source placement period.");
            if (source.PositionId == target.Id) throw new ArgumentException("Transfer target must differ from the source position.");
            var newEnd = source.EffectiveToExclusive;
            var replacementPeriod = new EffectivePeriod(command.EffectiveFrom, newEnd);
            await RequireActiveEmployeeAsync(employeeId, replacementPeriod, ct);
            // The source always ends where the target starts; a transfer on the source's start date cancels it.
            var sourceEnd = (DateOnly?)command.EffectiveFrom;
            var revisedSourcePeriod = sourceEnd.HasValue && sourceEnd.Value <= source.EffectiveFrom
                ? (EffectivePeriod?)null : new EffectivePeriod(source.EffectiveFrom, sourceEnd);
            await EnsureActingRebasePossibleAsync(source.Id, revisedSourcePeriod, command.EffectiveFrom, ct);
            var affected = await LoadCurrentActingForSourceAsync(source.Id, ct);
            Supersede(source, "Revised by transfer", mutation);
            await db.SaveChangesAsync(ct); // free the filtered current-revision index before inserting the replacement
            var sourceReplacement = sourceEnd.HasValue && sourceEnd.Value <= source.EffectiveFrom
                ? NewCancelledPlacement(source, reason, mutation)
                : NewPlacementRevision(source, sourceEnd, reason, mutation);
            db.EmployeePositions.Add(sourceReplacement);
            var targetRow = NewPlacement(employeeId, target.Id, source.IsPrimary, command.EffectiveFrom,
                newEnd, reason, command.DecisionReference, mutation);
            db.EmployeePositions.Add(targetRow);
            await db.SaveChangesAsync(ct); // free filtered current-key indexes before rebasing/inserting
            await RebaseActingAsync(affected, source.Id, sourceReplacement, revisedSourcePeriod, reason, mutation, ct);
            await ValidateAsync(employeeId, mutation, ct);
            support.Touch(employee, mutation);
            await support.AuditAsync(AuditEventType.EmployeePositionTransferred, employeeId, reason,
                new { sourceRecordId = source.Id, sourceReplacementId = sourceReplacement.Id, targetRecordId = targetRow.Id,
                    sourcePositionId = source.PositionId, targetPositionId = target.Id, command.EffectiveFrom }, mutation, ct);
            return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct), sourceReplacement.Id, targetRow.Id);
        }, cancellationToken);

    public Task<EmployeeMutationResult> EndDefinitiveAsync(Guid employeeId, EndDefinitiveAssignmentCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) =>
        runner.ExecuteAsync(async ct =>
        {
            var mutation = await support.AuthorizeAsync(actor, ct);
            var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
            EmployeeMutationSupport.NoBackdate(command.EffectiveToExclusive, mutation);
            var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
            var source = await CurrentPlacementAsync(employeeId, command.RecordId, ct);
            if (source.EffectiveToExclusive.HasValue && command.EffectiveToExclusive > source.EffectiveToExclusive.Value)
                throw new ArgumentException("An assignment cannot be extended through an end operation.");
            if (command.EffectiveToExclusive < source.EffectiveFrom) throw new ArgumentException("End date precedes assignment start.");
            var revisedPeriod = command.EffectiveToExclusive <= source.EffectiveFrom
                ? (EffectivePeriod?)null : new EffectivePeriod(source.EffectiveFrom, command.EffectiveToExclusive);
            await EnsureActingRebasePossibleAsync(source.Id, revisedPeriod, command.EffectiveToExclusive, ct);
            var affected = await LoadCurrentActingForSourceAsync(source.Id, ct);
            Supersede(source, "Revised by end assignment", mutation);
            await db.SaveChangesAsync(ct);
            var replacement = revisedPeriod is null
                ? NewCancelledPlacement(source, reason, mutation)
                : NewPlacementRevision(source, command.EffectiveToExclusive, reason, mutation);
            db.EmployeePositions.Add(replacement);
            await db.SaveChangesAsync(ct);
            await RebaseActingAsync(affected, source.Id, replacement, revisedPeriod, reason, mutation, ct);
            await ValidateAsync(employeeId, mutation, ct);
            support.Touch(employee, mutation);
            await support.AuditAsync(AuditEventType.EmployeePositionEnded, employeeId, reason,
                new { sourceRecordId = source.Id, recordId = replacement.Id, command.EffectiveToExclusive }, mutation, ct);
            return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct), replacement.Id);
        }, cancellationToken);

    public Task<EmployeeMutationResult> AssignActingAsync(Guid employeeId, CreateActingAssignmentCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) =>
        runner.ExecuteAsync(async ct =>
        {
            var mutation = await support.AuthorizeAsync(actor, ct);
            var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
            EmployeeMutationSupport.NoBackdate(command.EffectiveFrom, mutation);
            if (!Enum.IsDefined(command.AssignmentType)) throw new ArgumentException("Unknown acting assignment type.");
            var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
            var reference = EmployeeMutationSupport.Text(command.DecisionReference, 200, nameof(command.DecisionReference));
            var period = new EffectivePeriod(command.EffectiveFrom, command.EffectiveToExclusive);
            var source = await CurrentPlacementAsync(employeeId, command.SourceRecordId, ct);
            var target = await RequirePositionAsync(command.TargetPositionId, ct);
            var row = NewActing(employeeId, source.Id, target.Id, command.AssignmentType, period, reference, reason, mutation);
            PlacementScheduleRules.ValidateActingSource(source, row);
            await RequireActiveEmployeeAsync(employeeId, period, ct);
            await ValidatePositionPeriodAsync(target.Id, period, ct);
            db.EmployeeActingAssignments.Add(row);
            await db.SaveChangesAsync(ct);
            await ValidateAsync(employeeId, mutation, ct);
            support.Touch(employee, mutation);
            await support.AuditAsync(AuditEventType.EmployeeActingAssigned, employeeId, reason,
                new { recordId = row.Id, sourceRecordId = source.Id, targetPositionId = target.Id,
                    command.AssignmentType, period.From, period.ToExclusive }, mutation, ct);
            return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct), row.Id);
        }, cancellationToken);

    public Task<EmployeeMutationResult> EndActingAsync(Guid employeeId, EndActingAssignmentCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) =>
        runner.ExecuteAsync(async ct =>
        {
            var mutation = await support.AuthorizeAsync(actor, ct);
            var employee = await support.LoadEmployeeAsync(employeeId, expectedVersion, ct);
            EmployeeMutationSupport.NoBackdate(command.EffectiveToExclusive, mutation);
            var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
            var source = await CurrentActingAsync(employeeId, command.RecordId, ct);
            if (source.EffectiveToExclusive.HasValue && command.EffectiveToExclusive > source.EffectiveToExclusive.Value)
                throw new ArgumentException("An acting assignment cannot be extended through an end operation.");
            if (command.EffectiveToExclusive < source.EffectiveFrom) throw new ArgumentException("End date precedes assignment start.");
            Supersede(source, "Revised by end acting assignment", mutation);
            await db.SaveChangesAsync(ct);
            var replacement = NewActingRevision(source, command.EffectiveToExclusive <= source.EffectiveFrom
                ? EmployeeRecordState.Cancelled : EmployeeRecordState.Confirmed,
                command.EffectiveToExclusive <= source.EffectiveFrom ? source.EffectiveToExclusive : command.EffectiveToExclusive,
                reason, mutation);
            db.EmployeeActingAssignments.Add(replacement);
            await db.SaveChangesAsync(ct);
            await ValidateAsync(employeeId, mutation, ct);
            support.Touch(employee, mutation);
            await support.AuditAsync(AuditEventType.EmployeeActingEnded, employeeId, reason,
                new { sourceRecordId = source.Id, recordId = replacement.Id, command.EffectiveToExclusive }, mutation, ct);
            return new EmployeeMutationResult(await support.SummaryAsync(employee, mutation, ct), replacement.Id);
        }, cancellationToken);

    public async Task CloseForResignationAsync(Guid employeeId, DateOnly date, string reason,
        AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken)
    {
        support.RequireTransaction();
        var cleanReason = EmployeeMutationSupport.Text(reason, 500, nameof(reason));
        var placements = await db.EmployeePositions.Where(x => x.EmployeeId == employeeId
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(cancellationToken);
        var resignAffected = placements.Where(x => x.EmployeeId == employeeId &&
            (x.EffectiveToExclusive is null || x.EffectiveToExclusive > date)).ToArray();
        var actingCandidates = await db.EmployeeActingAssignments
            .Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null && x.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
        var touchedSources = new HashSet<Guid>();
        foreach (var current in resignAffected)
        {
            var shortened = current.EffectiveFrom < date ? new EffectivePeriod(current.EffectiveFrom, date) : (EffectivePeriod?)null;
            var affectedActing = actingCandidates.Where(x => x.SourceEmployeePositionId == current.Id).ToArray();
            Supersede(current, "Closed on resignation", mutation);
            await db.SaveChangesAsync(cancellationToken);
            var replacement = shortened is null
                ? NewCancelledPlacement(current, cleanReason, mutation)
                : NewPlacementRevision(current, date, cleanReason, mutation);
            db.EmployeePositions.Add(replacement);
            await db.SaveChangesAsync(cancellationToken);
            await RebaseActingAsync(affectedActing, current.Id, replacement, shortened, cleanReason, mutation,
                cancellationToken, resignationDate: date);
            touchedSources.Add(current.Id);
        }
        // Current confirmed acting rows may reference an already-ended definitive row. Rebase them too,
        // preserving their dates and history; only rows starting on/after resignation are cancelled.
        var remaining = await db.EmployeeActingAssignments
            .Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null && x.EmployeeId == employeeId)
            .ToListAsync(cancellationToken);
        foreach (var row in remaining.Where(x => x.EffectiveToExclusive is null || x.EffectiveToExclusive > date).ToArray())
        {
            var oldSource = await db.EmployeePositions.SingleOrDefaultAsync(x => x.Id == row.SourceEmployeePositionId, cancellationToken);
            if (oldSource is null) throw new InvalidOperationException("Confirmed acting assignment has no source placement.");
            if (touchedSources.Contains(oldSource.Id)) continue;
            // This source remains unmodified, but resigning acting periods must still be ended/cancelled.
            Supersede(row, "Closed on resignation", mutation);
            await db.SaveChangesAsync(cancellationToken);
            db.EmployeeActingAssignments.Add(NewActingRevision(row,
                row.EffectiveFrom < date ? EmployeeRecordState.Confirmed : EmployeeRecordState.Cancelled,
                row.EffectiveFrom < date ? date : row.EffectiveToExclusive, cleanReason, mutation));
        }
        await db.SaveChangesAsync(cancellationToken);
        await ValidateAsync(employeeId, mutation, cancellationToken);
    }

    public async Task ValidateAsync(Guid employeeId, AuthorizedEmployeeMutation mutation, CancellationToken cancellationToken)
    {
        support.RequireTransaction();
        await db.SaveChangesAsync(cancellationToken);
        // Scope: this employee's own timeline, plus every other current placement at positions this employee
        // occupies (needed only for capacity). Unrelated employees' data cannot make this mutation fail.
        var own = await db.EmployeePositions.Where(x => x.EmployeeId == employeeId
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(cancellationToken);
        var ownPositionIds = own.Select(x => x.PositionId).Distinct().ToArray();
        var sharedPositions = await db.EmployeePositions.Where(x => ownPositionIds.Contains(x.PositionId)
            && x.EmployeeId != employeeId && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null)
            .ToListAsync(cancellationToken);
        var placements = own.Concat(sharedPositions).ToList();
        var acting = await db.EmployeeActingAssignments.Where(x => x.EmployeeId == employeeId
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(cancellationToken);
        var statuses = await db.EmployeeStatuses.Where(x => x.EmployeeId == employeeId
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(cancellationToken);
        var positionIds = placements.Select(x => x.PositionId).Concat(acting.Select(x => x.TargetPositionId)).Distinct().ToArray();
        var capacities = await LoadCapacityPeriodsAsync(positionIds, cancellationToken);
        PlacementScheduleRules.ValidateDefinitiveSchedule(placements, capacities);
        ValidateEmploymentCoverage(own, acting, statuses);
        foreach (var row in acting)
        {
            var period = new EffectivePeriod(row.EffectiveFrom, row.EffectiveToExclusive);
            if (!HasCoverage(period, capacities.Where(x => x.PositionId == row.TargetPositionId && x.IsActive).Select(x => x.Period)))
                throw new InvalidOperationException("Acting target position, unit and office must remain active throughout the acting period.");
        }
        foreach (var group in statuses.GroupBy(x => x.EmployeeId))
        {
            var ordered = group.OrderBy(x => x.EffectiveFrom).ToArray();
            for (var i = 1; i < ordered.Length; i++)
            {
                var previous = new EffectivePeriod(ordered[i - 1].EffectiveFrom, ordered[i - 1].EffectiveToExclusive);
                if (previous.ToExclusive != ordered[i].EffectiveFrom)
                    throw new InvalidOperationException("Current confirmed employee status periods cannot overlap or contain gaps.");
            }
        }
        await ValidateOrganizationGraphAsync(own, acting, cancellationToken);
        foreach (var row in acting)
        {
            var source = own.SingleOrDefault(x => x.Id == row.SourceEmployeePositionId);
            if (source is null) throw new InvalidOperationException("Current acting assignment must reference a current confirmed definitive row.");
            PlacementScheduleRules.ValidateActingSource(source, row);
        }
        _ = mutation;
    }

    private async Task<EmployeePosition> CurrentPlacementAsync(Guid employeeId, Guid recordId, CancellationToken ct) =>
        await db.EmployeePositions.SingleOrDefaultAsync(x => x.Id == recordId && x.EmployeeId == employeeId &&
            x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null, ct)
        ?? throw new KeyNotFoundException("Current confirmed definitive assignment not found.");

    private async Task<EmployeeActingAssignment> CurrentActingAsync(Guid employeeId, Guid recordId, CancellationToken ct) =>
        await db.EmployeeActingAssignments.SingleOrDefaultAsync(x => x.Id == recordId && x.EmployeeId == employeeId &&
            x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null, ct)
        ?? throw new KeyNotFoundException("Current confirmed acting assignment not found.");

    private async Task<Position> RequirePositionAsync(Guid positionId, CancellationToken ct) =>
        await db.Positions.SingleOrDefaultAsync(x => x.Id == positionId, ct)
        ?? throw new KeyNotFoundException("Position not found.");

    private async Task<List<EmployeePosition>> LoadCurrentPlacementsAsync(CancellationToken ct) =>
        await db.EmployeePositions.Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(ct);

    private async Task<List<PositionCapacityPeriod>> LoadCapacityPeriodsAsync(IEnumerable<Guid> positionIds, CancellationToken ct)
    {
        var ids = positionIds.Distinct().ToArray();
        if (ids.Length == 0) return [];
        // Scoped to the requested positions and their own units/offices (not whole tables): organization writes validate
        // every placed employee while holding the global mutation lock, so this must stay cheap.
        var rows = await db.PositionVersions.AsNoTracking().Where(x => ids.Contains(x.PositionId)).ToListAsync(ct);
        var positions = await db.Positions.AsNoTracking().Where(x => ids.Contains(x.Id)).ToListAsync(ct);
        var unitIds = positions.Select(x => x.OrgUnitId).Distinct().ToArray();
        var units = await db.OrgUnits.AsNoTracking().Where(x => unitIds.Contains(x.Id)).ToListAsync(ct);
        var unitVersions = await db.OrgUnitVersions.AsNoTracking().Where(x => unitIds.Contains(x.OrgUnitId)).ToListAsync(ct);
        var officeIds = units.Select(x => x.OfficeId).Distinct().ToArray();
        var offices = await db.Offices.AsNoTracking().Where(x => officeIds.Contains(x.Id)).ToListAsync(ct);
        var officeVersions = await db.OfficeVersions.AsNoTracking().Where(x => officeIds.Contains(x.OfficeId)).ToListAsync(ct);
        var periods = new List<PositionCapacityPeriod>();
        foreach (var id in ids)
        {
            var position = positions.SingleOrDefault(x => x.Id == id) ?? throw new KeyNotFoundException("Position not found.");
            var unit = units.SingleOrDefault(x => x.Id == position.OrgUnitId) ?? throw new InvalidOperationException("Position has no organization unit.");
            var office = offices.SingleOrDefault(x => x.Id == unit.OfficeId) ?? throw new InvalidOperationException("Organization unit has no office.");
            var activeUnit = unitVersions.Where(x => x.OrgUnitId == unit.Id && x.SupersededAtUtc is null && x.IsActive)
                .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive)).ToArray();
            var activeOffice = officeVersions.Where(x => x.OfficeId == office.Id && x.SupersededAtUtc is null && x.IsActive)
                .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive)).ToArray();
            foreach (var version in rows.Where(x => x.PositionId == id && x.SupersededAtUtc is null))
            {
                var range = new EffectivePeriod(version.EffectiveFrom, version.EffectiveToExclusive);
                var covered = IntersectCoverage(range, activeUnit)
                    .SelectMany(unitPeriod => IntersectCoverage(unitPeriod, activeOffice)).ToList();
                if (version.IsActive)
                    periods.AddRange(covered.Select(p => new PositionCapacityPeriod(id, p, version.MaxIncumbents, true)));
                else periods.Add(new PositionCapacityPeriod(id, range, version.MaxIncumbents, false));
            }
        }
        return periods;
    }

    private static List<EffectivePeriod> IntersectCoverage(EffectivePeriod range, IEnumerable<EffectivePeriod> coverages) =>
        coverages.Select(x => range.Intersect(x)).Where(x => x.HasValue).Select(x => x!.Value).ToList();

    private async Task RequireActiveEmployeeAsync(Guid employeeId, EffectivePeriod period, CancellationToken ct)
    {
        var rows = await db.EmployeeStatuses.Where(x => x.EmployeeId == employeeId && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null)
            .ToListAsync(ct);
        if (!HasCoverage(period, rows.Where(x => x.StatusCode == EmployeeStatusCode.Active)
            .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive))))
            throw new InvalidOperationException("Employee must have ACTIVE status throughout the assignment period.");
    }

    private async Task ValidatePositionPeriodAsync(Guid positionId, EffectivePeriod period, CancellationToken ct)
    {
        var capacities = await LoadCapacityPeriodsAsync([positionId], ct);
        if (!HasCoverage(period, capacities.Where(x => x.IsActive).Select(x => x.Period)))
            throw new InvalidOperationException("Target position, unit and office must remain active throughout the acting period.");
    }

    private static void ValidateEmploymentCoverage(IEnumerable<EmployeePosition> placements,
        IEnumerable<EmployeeActingAssignment> acting, IEnumerable<EmployeeStatus> statuses)
    {
        foreach (var group in placements.GroupBy(x => x.EmployeeId))
        {
            var active = statuses.Where(x => x.EmployeeId == group.Key && x.StatusCode == EmployeeStatusCode.Active)
                .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive)).ToArray();
            foreach (var row in group)
                if (!HasCoverage(new EffectivePeriod(row.EffectiveFrom, row.EffectiveToExclusive), active))
                    throw new InvalidOperationException("Every definitive assignment must be covered by ACTIVE employment status.");
        }
        foreach (var row in acting)
        {
            var active = statuses.Where(x => x.EmployeeId == row.EmployeeId && x.StatusCode == EmployeeStatusCode.Active)
                .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive));
            if (!HasCoverage(new EffectivePeriod(row.EffectiveFrom, row.EffectiveToExclusive), active))
                throw new InvalidOperationException("Every acting assignment must be covered by ACTIVE employment status.");
        }
    }

    private static bool HasCoverage(EffectivePeriod target, IEnumerable<EffectivePeriod> coverage)
    {
        var cursor = target.From;
        foreach (var period in coverage.OrderBy(x => x.From))
        {
            if (period.ToExclusive.HasValue && period.ToExclusive.Value <= cursor) continue;
            if (period.From > cursor) return false;
            if (!period.ToExclusive.HasValue) return true;
            cursor = period.ToExclusive.Value;
            if (target.ToExclusive.HasValue && cursor >= target.ToExclusive.Value) return true;
        }
        return false;
    }

    private async Task ValidateOrganizationGraphAsync(IEnumerable<EmployeePosition> placements,
        IEnumerable<EmployeeActingAssignment> acting, CancellationToken ct)
    {
        var allPositions = await db.Positions.ToListAsync(ct);
        var allUnits = await db.OrgUnits.ToListAsync(ct);
        var versions = (await db.PositionVersions.ToListAsync(ct)).Where(x => x.SupersededAtUtc is null).ToArray();
        var unitVersions = (await db.OrgUnitVersions.ToListAsync(ct)).Where(x => x.SupersededAtUtc is null).ToArray();
        var activePeriods = placements.Select(x => new { x.PositionId, Period = new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive) })
            .Concat(acting.Select(x => new { PositionId = x.TargetPositionId, Period = new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive) })).ToArray();
        foreach (var entry in activePeriods)
        {
            var target = allPositions.SingleOrDefault(x => x.Id == entry.PositionId) ?? throw new KeyNotFoundException("Referenced position not found.");
            var unit = allUnits.SingleOrDefault(x => x.Id == target.OrgUnitId) ?? throw new InvalidOperationException("Position organization unit is missing.");
            var targetUnitVersions = unitVersions.Where(x => x.OrgUnitId == unit.Id).ToArray();
            foreach (var segment in targetUnitVersions)
            {
                var period = new EffectivePeriod(segment.EffectiveFrom, segment.EffectiveToExclusive).Intersect(entry.Period);
                if (period is null) continue;
                if (segment.ParentOrgUnitId is { } parentId)
                {
                    var parent = allUnits.SingleOrDefault(x => x.Id == parentId) ?? throw new InvalidOperationException("Parent unit does not exist.");
                    if (parent.OfficeId != unit.OfficeId) throw new InvalidOperationException("Organization parent cannot cross office boundaries.");
                    var parentCoverage = unitVersions.Where(x => x.OrgUnitId == parentId && x.IsActive && x.SupersededAtUtc is null)
                        .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive));
                    if (!HasCoverage(period.Value, parentCoverage)) throw new InvalidOperationException("Parent unit must be active throughout the child unit period.");
                }
                if (!segment.IsActive) throw new InvalidOperationException("An assignment cannot use an inactive organization unit.");
                var targetPositionVersions = versions.Where(x => x.PositionId == entry.PositionId).ToArray();
                foreach (var positionVersion in targetPositionVersions)
                {
                    if (!new EffectivePeriod(positionVersion.EffectiveFrom, positionVersion.EffectiveToExclusive).Overlaps(period.Value)) continue;
                    if (positionVersion.ReportsToPositionId is { } reportsTo)
                    {
                        var reporting = allPositions.SingleOrDefault(x => x.Id == reportsTo) ?? throw new InvalidOperationException("Reporting position does not exist.");
                        var reportingUnit = allUnits.SingleOrDefault(x => x.Id == reporting.OrgUnitId)
                            ?? throw new InvalidOperationException("Reporting position organization unit is missing.");
                        if (reportingUnit.OfficeId != unit.OfficeId) throw new InvalidOperationException("Reporting position cannot cross office boundaries.");
                        var coverage = versions.Where(x => x.PositionId == reportsTo && x.IsActive && x.SupersededAtUtc is null)
                            .Select(x => new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive));
                        if (!HasCoverage(period.Value, coverage)) throw new InvalidOperationException("Reporting position must be active throughout the assignment period.");
                        DetectReportingCycle(entry.PositionId, period.Value, versions);
                    }
                }
            }
        }
    }

    /// <summary>Walks reporting lines only through versions that coexist within <paramref name="period"/>.</summary>
    private static void DetectReportingCycle(Guid positionId, EffectivePeriod period, IEnumerable<PositionVersion> versions)
    {
        var byPosition = versions.GroupBy(x => x.PositionId).ToDictionary(x => x.Key, x => x.ToArray());
        VisitReporting(positionId, period, new HashSet<Guid> { positionId }, byPosition);
    }

    private static void VisitReporting(Guid current, EffectivePeriod period, HashSet<Guid> path,
        Dictionary<Guid, PositionVersion[]> byPosition)
    {
        if (!byPosition.TryGetValue(current, out var rows)) return;
        var overlapping = rows.Select(v => (Version: v, Overlap: new EffectivePeriod(v.EffectiveFrom, v.EffectiveToExclusive).Intersect(period)))
            .Where(x => x.Overlap.HasValue && x.Version.ReportsToPositionId.HasValue).ToArray();
        foreach (var (version, overlap) in overlapping)
        {
            var next = version.ReportsToPositionId!.Value;
            if (!path.Add(next)) throw new InvalidOperationException("Reporting-line cycle detected.");
            VisitReporting(next, overlap!.Value, path, byPosition);
            path.Remove(next);
        }
    }

    private async Task EnsureActingRebasePossibleAsync(Guid sourceId, EffectivePeriod? newSourcePeriod, DateOnly changeDate,
        CancellationToken ct)
    {
        var rows = await LoadCurrentActingForSourceAsync(sourceId, ct);
        foreach (var row in rows)
        {
            if (newSourcePeriod is null || !newSourcePeriod.Value.Covers(new EffectivePeriod(row.EffectiveFrom, row.EffectiveToExclusive)))
                throw new InvalidOperationException("End or revise acting assignments before shortening their definitive source.");
        }
        _ = changeDate;
    }

    private Task<List<EmployeeActingAssignment>> LoadCurrentActingForSourceAsync(Guid sourceId, CancellationToken ct) =>
        db.EmployeeActingAssignments.Where(x => x.SourceEmployeePositionId == sourceId && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null)
            .ToListAsync(ct);

    private async Task RebaseActingAsync(IEnumerable<EmployeeActingAssignment> rows, Guid oldSourceId, EmployeePosition newSource,
        EffectivePeriod? sourcePeriod, string reason, AuthorizedEmployeeMutation mutation, CancellationToken ct,
        DateOnly? resignationDate = null)
    {
        foreach (var row in rows)
        {
            Supersede(row, "Rebased to definitive revision", mutation);
            await db.SaveChangesAsync(ct);
            var state = row.RecordState;
            var end = row.EffectiveToExclusive;
            if (resignationDate.HasValue && (end is null || end > resignationDate.Value))
            {
                if (row.EffectiveFrom >= resignationDate.Value) state = EmployeeRecordState.Cancelled;
                else end = resignationDate.Value;
            }
            var replacementSource = sourcePeriod is null && state == EmployeeRecordState.Confirmed
                ? oldSourceId : newSource.Id;
            var replacement = NewActingRevision(row, state, end, reason, mutation,
                sourceId: replacementSource, revisionIncrement: true);
            db.EmployeeActingAssignments.Add(replacement);
        }
        await db.SaveChangesAsync(ct);
    }

    private static EmployeePosition NewPlacement(Guid employeeId, Guid positionId, bool primary, DateOnly from, DateOnly? to,
        string reason, string? reference, AuthorizedEmployeeMutation mutation) => new()
    {
        EmployeeId = employeeId, PositionId = positionId, IsPrimary = primary, EffectiveFrom = from, EffectiveToExclusive = to,
        RecordState = EmployeeRecordState.Confirmed, RevisionNumber = 1, ChangeReason = reason,
        DecisionReference = EmployeeMutationSupport.OptionalText(reference, 200, nameof(reference)),
        CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
    };

    private static EmployeePosition NewPlacementRevision(EmployeePosition source, DateOnly? end, string reason, AuthorizedEmployeeMutation mutation) => new()
    {
        AssignmentKey = source.AssignmentKey, EmployeeId = source.EmployeeId, PositionId = source.PositionId, IsPrimary = source.IsPrimary,
        EffectiveFrom = source.EffectiveFrom, EffectiveToExclusive = end, RecordState = EmployeeRecordState.Confirmed,
        RevisionNumber = source.RevisionNumber + 1, ChangeReason = reason, DecisionReference = source.DecisionReference,
        CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
    };

    private static EmployeePosition NewCancelledPlacement(EmployeePosition source, string reason, AuthorizedEmployeeMutation mutation) => new()
    {
        AssignmentKey = source.AssignmentKey, EmployeeId = source.EmployeeId, PositionId = source.PositionId, IsPrimary = source.IsPrimary,
        EffectiveFrom = source.EffectiveFrom, EffectiveToExclusive = source.EffectiveToExclusive, RecordState = EmployeeRecordState.Cancelled,
        RevisionNumber = source.RevisionNumber + 1, ChangeReason = reason, DecisionReference = source.DecisionReference,
        CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
    };

    private static EmployeeActingAssignment NewActing(Guid employeeId, Guid sourceId, Guid targetId, ActingAssignmentType type,
        EffectivePeriod period, string reference, string reason, AuthorizedEmployeeMutation mutation) => new()
    {
        EmployeeId = employeeId, SourceEmployeePositionId = sourceId, TargetPositionId = targetId, AssignmentType = type,
        EffectiveFrom = period.From, EffectiveToExclusive = period.ToExclusive, RecordState = EmployeeRecordState.Confirmed,
        RevisionNumber = 1, DecisionReference = reference, ChangeReason = reason,
        CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
    };

    private static EmployeeActingAssignment NewActingRevision(EmployeeActingAssignment source, EmployeeRecordState state,
        DateOnly? end, string reason, AuthorizedEmployeeMutation mutation, Guid? sourceId = null, bool revisionIncrement = true) => new()
    {
        AssignmentKey = source.AssignmentKey, EmployeeId = source.EmployeeId, SourceEmployeePositionId = sourceId ?? source.SourceEmployeePositionId,
        TargetPositionId = source.TargetPositionId, AssignmentType = source.AssignmentType, EffectiveFrom = source.EffectiveFrom,
        EffectiveToExclusive = end, RecordState = state, RevisionNumber = source.RevisionNumber + (revisionIncrement ? 1 : 0),
        DecisionReference = source.DecisionReference, ChangeReason = reason,
        CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
    };

    private static void Supersede(EmployeePosition row, string reason, AuthorizedEmployeeMutation mutation)
    {
        row.SupersededAtUtc = mutation.NowUtc; row.SupersededByUserId = mutation.ActorUserId; row.SupersededReason = reason;
    }
    private static void Supersede(EmployeeActingAssignment row, string reason, AuthorizedEmployeeMutation mutation)
    {
        row.SupersededAtUtc = mutation.NowUtc; row.SupersededByUserId = mutation.ActorUserId; row.SupersededReason = reason;
    }
}
