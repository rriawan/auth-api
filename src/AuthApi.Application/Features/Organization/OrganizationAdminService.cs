using System.Text.RegularExpressions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Domain.Enums.Organization;
using AuthApi.Domain.Rules.Organization;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Organization;

/// <summary>
/// Office, unit and position master writes. Every write runs inside the shared mutation transaction (global
/// applock), re-authorizes against the database, applies the change as an effective-dated version, then re-validates
/// the WHOLE office (all periods) and every employee placed in it before committing. Any failure rolls everything back.
/// </summary>
public sealed partial class OrganizationAdminService(IAppDbContext db, IEmployeeMutationRunner runner,
    EmployeeMutationSupport support, IEmployeeScheduleWriter schedules) : IOrganizationAdminService
{
    public const int MaxFutureYears = 5;

    [GeneratedRegex("^[0-9]{3}$")] private static partial Regex OfficeCodePattern();
    [GeneratedRegex("^[A-Za-z0-9][A-Za-z0-9_.-]{0,49}$")] private static partial Regex CodePattern();

    // ---- offices ------------------------------------------------------------------------------------------

    public Task<OrganizationMutationResult> CreateOfficeAsync(CreateOfficeCommand command, EmployeeActor actor,
        CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        var code = command.OfficeCode?.Trim() ?? string.Empty;
        if (!OfficeCodePattern().IsMatch(code)) throw new ArgumentException("Office code must be exactly three digits.", nameof(command));
        var name = EmployeeMutationSupport.Text(command.Name, 200, nameof(command.Name));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        await RequireClassificationAsync(command.ClassificationCode, ct);
        if (await db.Offices.AnyAsync(x => x.OfficeCode == code, ct)) throw new InvalidOperationException("An office with this code already exists.");

        var office = new Office { OfficeCode = code, CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId };
        var version = new OfficeVersion
        {
            OfficeId = office.Id, ClassificationCode = command.ClassificationCode.Trim(), Name = name, IsActive = true,
            EffectiveFrom = command.EffectiveFrom, RevisionNumber = 1, ChangeReason = reason, DecisionReference = reference,
            CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
        };
        db.Offices.Add(office);
        db.OfficeVersions.Add(version);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(office.Id, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationOfficeCreated, office.Id, reason,
            new { office.OfficeCode, command.ClassificationCode, command.EffectiveFrom }, mutation, ct);
        return new OrganizationMutationResult(office.Id, EmployeeMutationSupport.ETag(office.RowVersion), version.Id);
    }, cancellationToken);

    public Task<OrganizationMutationResult> AddOfficeVersionAsync(Guid officeId, OfficeVersionCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var office = await db.Offices.SingleOrDefaultAsync(x => x.Id == officeId, ct) ?? throw new KeyNotFoundException("Office not found.");
        EmployeeMutationSupport.CheckRowVersion(office.RowVersion, expectedVersion);
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        var name = EmployeeMutationSupport.Text(command.Name, 200, nameof(command.Name));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        await RequireClassificationAsync(command.ClassificationCode, ct);

        var rows = await db.OfficeVersions.Where(x => x.OfficeId == officeId).ToListAsync(ct);
        var added = await ApplyVersionAsync(db.OfficeVersions, rows, command.EffectiveFrom, reason, mutation,
            () => new OfficeVersion
            {
                OfficeId = officeId, ClassificationCode = command.ClassificationCode.Trim(), Name = name,
                IsActive = command.IsActive, DecisionReference = reference
            },
            c => new OfficeVersion { OfficeId = c.OfficeId, ClassificationCode = c.ClassificationCode, Name = c.Name,
                IsActive = c.IsActive, DecisionReference = c.DecisionReference }, ct);
        support.MarkMasterChanged(office);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(officeId, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationOfficeVersioned, officeId, reason,
            new { versionId = added.Id, command.ClassificationCode, command.IsActive, command.EffectiveFrom }, mutation, ct);
        return new OrganizationMutationResult(officeId, EmployeeMutationSupport.ETag(office.RowVersion), added.Id);
    }, cancellationToken);

    // ---- units --------------------------------------------------------------------------------------------

    public Task<OrganizationMutationResult> CreateUnitAsync(CreateUnitCommand command, EmployeeActor actor,
        CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        var code = command.UnitCode?.Trim() ?? string.Empty;
        if (!CodePattern().IsMatch(code)) throw new ArgumentException("Unit code must be 1-50 letters, digits, '.', '_' or '-'.", nameof(command));
        if (!Enum.IsDefined(command.UnitType)) throw new ArgumentException("Unknown unit type.", nameof(command));
        var name = EmployeeMutationSupport.Text(command.Name, 200, nameof(command.Name));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        if (!await db.Offices.AnyAsync(x => x.Id == command.OfficeId, ct)) throw new KeyNotFoundException("Office not found.");
        await RequireUnitInOfficeAsync(command.ParentOrgUnitId, command.OfficeId, ct);
        if (await db.OrgUnits.AnyAsync(x => x.OfficeId == command.OfficeId && x.UnitCode == code, ct))
            throw new InvalidOperationException("A unit with this code already exists in the office.");

        var unit = new OrgUnit { OfficeId = command.OfficeId, UnitCode = code, CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId };
        var version = new OrgUnitVersion
        {
            OrgUnitId = unit.Id, ParentOrgUnitId = command.ParentOrgUnitId, UnitType = command.UnitType, Name = name, IsActive = true,
            EffectiveFrom = command.EffectiveFrom, RevisionNumber = 1, ChangeReason = reason, DecisionReference = reference,
            CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
        };
        db.OrgUnits.Add(unit);
        db.OrgUnitVersions.Add(version);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(command.OfficeId, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationUnitCreated, unit.Id, reason,
            new { command.OfficeId, unit.UnitCode, command.ParentOrgUnitId, command.EffectiveFrom }, mutation, ct);
        return new OrganizationMutationResult(unit.Id, EmployeeMutationSupport.ETag(unit.RowVersion), version.Id);
    }, cancellationToken);

    public Task<OrganizationMutationResult> AddUnitVersionAsync(Guid unitId, UnitVersionCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var unit = await db.OrgUnits.SingleOrDefaultAsync(x => x.Id == unitId, ct) ?? throw new KeyNotFoundException("Unit not found.");
        EmployeeMutationSupport.CheckRowVersion(unit.RowVersion, expectedVersion);
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        if (!Enum.IsDefined(command.UnitType)) throw new ArgumentException("Unknown unit type.", nameof(command));
        var name = EmployeeMutationSupport.Text(command.Name, 200, nameof(command.Name));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        await RequireUnitInOfficeAsync(command.ParentOrgUnitId, unit.OfficeId, ct);

        var rows = await db.OrgUnitVersions.Where(x => x.OrgUnitId == unitId).ToListAsync(ct);
        var added = await ApplyVersionAsync(db.OrgUnitVersions, rows, command.EffectiveFrom, reason, mutation,
            () => new OrgUnitVersion { OrgUnitId = unitId, ParentOrgUnitId = command.ParentOrgUnitId, UnitType = command.UnitType,
                Name = name, IsActive = command.IsActive, DecisionReference = reference },
            c => new OrgUnitVersion { OrgUnitId = c.OrgUnitId, ParentOrgUnitId = c.ParentOrgUnitId, UnitType = c.UnitType,
                Name = c.Name, IsActive = c.IsActive, DecisionReference = c.DecisionReference }, ct);
        support.MarkMasterChanged(unit);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(unit.OfficeId, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationUnitVersioned, unitId, reason,
            new { versionId = added.Id, command.ParentOrgUnitId, command.IsActive, command.EffectiveFrom }, mutation, ct);
        return new OrganizationMutationResult(unitId, EmployeeMutationSupport.ETag(unit.RowVersion), added.Id);
    }, cancellationToken);

    // ---- positions ----------------------------------------------------------------------------------------

    public Task<OrganizationMutationResult> CreatePositionAsync(CreatePositionCommand command, EmployeeActor actor,
        CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        var code = command.PositionCode?.Trim() ?? string.Empty;
        if (!CodePattern().IsMatch(code)) throw new ArgumentException("Position code must be 1-50 letters, digits, '.', '_' or '-'.", nameof(command));
        var name = EmployeeMutationSupport.Text(command.Name, 200, nameof(command.Name));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        var unit = await db.OrgUnits.SingleOrDefaultAsync(x => x.Id == command.OrgUnitId, ct) ?? throw new KeyNotFoundException("Unit not found.");
        await ValidatePositionFieldsAsync(command.Track, command.IsUnitHead, command.MaxIncumbents, command.LevelId, command.ReportsToPositionId, unit.OfficeId, ct);
        if (await db.Positions.AnyAsync(x => x.OrgUnitId == command.OrgUnitId && x.PositionCode == code, ct))
            throw new InvalidOperationException("A position with this code already exists in the unit.");

        var position = new Position { OrgUnitId = command.OrgUnitId, PositionCode = code, CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId };
        var version = new PositionVersion
        {
            PositionId = position.Id, LevelId = command.LevelId, ReportsToPositionId = command.ReportsToPositionId, Name = name,
            Track = command.Track, IsUnitHead = command.IsUnitHead, MaxIncumbents = command.MaxIncumbents, IsActive = true,
            EffectiveFrom = command.EffectiveFrom, RevisionNumber = 1, ChangeReason = reason, DecisionReference = reference,
            CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
        };
        db.Positions.Add(position);
        db.PositionVersions.Add(version);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(unit.OfficeId, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationPositionCreated, position.Id, reason,
            new { command.OrgUnitId, position.PositionCode, command.Track, command.IsUnitHead, command.MaxIncumbents, command.EffectiveFrom }, mutation, ct);
        return new OrganizationMutationResult(position.Id, EmployeeMutationSupport.ETag(position.RowVersion), version.Id);
    }, cancellationToken);

    public Task<OrganizationMutationResult> AddPositionVersionAsync(Guid positionId, PositionVersionCommand command,
        string? expectedVersion, EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var position = await db.Positions.SingleOrDefaultAsync(x => x.Id == positionId, ct) ?? throw new KeyNotFoundException("Position not found.");
        EmployeeMutationSupport.CheckRowVersion(position.RowVersion, expectedVersion);
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        var name = EmployeeMutationSupport.Text(command.Name, 200, nameof(command.Name));
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        var officeId = await OfficeOfUnitAsync(position.OrgUnitId, ct);
        await ValidatePositionFieldsAsync(command.Track, command.IsUnitHead, command.MaxIncumbents, command.LevelId, command.ReportsToPositionId, officeId, ct);

        var rows = await db.PositionVersions.Where(x => x.PositionId == positionId).ToListAsync(ct);
        var added = await ApplyVersionAsync(db.PositionVersions, rows, command.EffectiveFrom, reason, mutation,
            () => new PositionVersion { PositionId = positionId, LevelId = command.LevelId, ReportsToPositionId = command.ReportsToPositionId,
                Name = name, Track = command.Track, IsUnitHead = command.IsUnitHead, MaxIncumbents = command.MaxIncumbents,
                IsActive = command.IsActive, DecisionReference = reference },
            c => new PositionVersion { PositionId = c.PositionId, LevelId = c.LevelId, ReportsToPositionId = c.ReportsToPositionId,
                Name = c.Name, Track = c.Track, IsUnitHead = c.IsUnitHead, MaxIncumbents = c.MaxIncumbents,
                IsActive = c.IsActive, DecisionReference = c.DecisionReference }, ct);
        support.MarkMasterChanged(position);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(officeId, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationPositionVersioned, positionId, reason,
            new { versionId = added.Id, command.Track, command.IsUnitHead, command.MaxIncumbents, command.ReportsToPositionId,
                command.IsActive, command.EffectiveFrom }, mutation, ct);
        return new OrganizationMutationResult(positionId, EmployeeMutationSupport.ETag(position.RowVersion), added.Id);
    }, cancellationToken);

    // ---- helpers ------------------------------------------------------------------------------------------

    /// <summary>
    /// Supersedes the version covering <paramref name="from"/>, keeps its earlier part as a truncated revision, and adds an
    /// open-ended new version. Rejects the change when a later current version exists (revise that timeline explicitly first).
    /// </summary>
    private async Task<TVersion> ApplyVersionAsync<TVersion>(DbSet<TVersion> set, IReadOnlyList<TVersion> allRows, DateOnly from,
        string reason, AuthorizedEmployeeMutation mutation, Func<TVersion> create, Func<TVersion, TVersion> clone, CancellationToken ct)
        where TVersion : class, IEffectiveVersion
    {
        var current = allRows.Where(x => x.SupersededAtUtc is null).ToList();
        var covering = current.SingleOrDefault(x => x.EffectiveFrom <= from && (x.EffectiveToExclusive == null || from < x.EffectiveToExclusive))
            ?? throw new InvalidOperationException("No current version covers the requested effective date.");
        if (current.Any(x => x.EffectiveFrom > from))
            throw new InvalidOperationException("A later version exists; revise that timeline explicitly first.");

        var revision = allRows.Max(x => x.RevisionNumber);
        covering.SupersededAtUtc = mutation.NowUtc;
        covering.SupersededByUserId = mutation.ActorUserId;
        covering.SupersededReason = reason;
        await db.SaveChangesAsync(ct); // free the filtered current-version index before inserting replacements

        if (covering.EffectiveFrom < from)
        {
            var truncated = clone(covering);
            truncated.EffectiveFrom = covering.EffectiveFrom;
            truncated.EffectiveToExclusive = from;
            truncated.RevisionNumber = ++revision;
            truncated.ChangeReason = covering.ChangeReason; // history of the earlier period; the new reason is on SupersededReason
            truncated.CreatedAtUtc = mutation.NowUtc;
            truncated.CreatedByUserId = mutation.ActorUserId;
            set.Add(truncated);
        }

        var added = create();
        added.EffectiveFrom = from;
        added.EffectiveToExclusive = null;
        added.RevisionNumber = ++revision;
        added.ChangeReason = reason;
        added.CreatedAtUtc = mutation.NowUtc;
        added.CreatedByUserId = mutation.ActorUserId;
        set.Add(added);
        return added;
    }

    /// <summary>Not in the past (D-03) and at most MaxFutureYears ahead, so a typo cannot create an unreachable timeline.</summary>
    private static void RequireEffectiveDate(DateOnly date, AuthorizedEmployeeMutation mutation)
    {
        EmployeeMutationSupport.NoBackdate(date, mutation);
        if (date > mutation.BusinessDate.AddYears(MaxFutureYears))
            throw new ArgumentException($"Effective date cannot be more than {MaxFutureYears} years in the future.");
    }

    private async Task RequireClassificationAsync(string? code, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(code) || !await db.OfficeClassifications.AnyAsync(x => x.Code == code.Trim(), ct))
            throw new ArgumentException("Unknown office classification.", nameof(code));
    }

    private async Task RequireUnitInOfficeAsync(Guid? parentId, Guid officeId, CancellationToken ct)
    {
        if (parentId is null) return;
        var parentOffice = await db.OrgUnits.AsNoTracking().Where(x => x.Id == parentId).Select(x => (Guid?)x.OfficeId).SingleOrDefaultAsync(ct)
            ?? throw new KeyNotFoundException("Parent unit not found.");
        if (parentOffice != officeId) throw new InvalidOperationException("A parent unit cannot belong to another office.");
    }

    private async Task<Guid> OfficeOfUnitAsync(Guid unitId, CancellationToken ct) =>
        await db.OrgUnits.AsNoTracking().Where(x => x.Id == unitId).Select(x => x.OfficeId).SingleAsync(ct);

    private async Task ValidatePositionFieldsAsync(PositionTrack track, bool isUnitHead, int? maxIncumbents, Guid? levelId,
        Guid? reportsTo, Guid officeId, CancellationToken ct)
    {
        if (!Enum.IsDefined(track)) throw new ArgumentException("Unknown position track.", nameof(track));
        if (maxIncumbents is <= 0 or > 100_000) throw new ArgumentException("Capacity must be between 1 and 100000, or empty for unlimited.", nameof(maxIncumbents));
        if (isUnitHead && maxIncumbents != 1) throw new ArgumentException("A unit head position must allow exactly one incumbent.", nameof(maxIncumbents));
        if (levelId is { } level && !await db.PositionLevels.AnyAsync(x => x.Id == level, ct))
            throw new KeyNotFoundException("Position level not found.");
        if (reportsTo is { } target)
        {
            var targetOffice = await db.Positions.AsNoTracking().Where(x => x.Id == target)
                .Select(x => (Guid?)x.OrgUnit.OfficeId).SingleOrDefaultAsync(ct) ?? throw new KeyNotFoundException("Reporting position not found.");
            if (targetOffice != officeId) throw new InvalidOperationException("A reporting position cannot belong to another office.");
        }
    }

    private async Task<(OfficeSnapshot Snapshot, Guid[] PositionIds)> LoadSnapshotAsync(Guid officeId, CancellationToken ct)
    {
        var officeVersions = await db.OfficeVersions.Where(x => x.OfficeId == officeId && x.SupersededAtUtc == null).ToListAsync(ct);
        var unitIds = await db.OrgUnits.Where(x => x.OfficeId == officeId).Select(x => x.Id).ToListAsync(ct);
        var unitVersions = await db.OrgUnitVersions.Where(x => unitIds.Contains(x.OrgUnitId) && x.SupersededAtUtc == null).ToListAsync(ct);
        var positionUnit = await db.Positions.Where(x => unitIds.Contains(x.OrgUnitId)).Select(x => new { x.Id, x.OrgUnitId }).ToListAsync(ct);
        var positionIds = positionUnit.Select(x => x.Id).ToArray();
        var positionVersions = await db.PositionVersions.Where(x => positionIds.Contains(x.PositionId) && x.SupersededAtUtc == null).ToListAsync(ct);
        return (new OfficeSnapshot(officeVersions, unitVersions, positionUnit.ToDictionary(x => x.Id, x => x.OrgUnitId), positionVersions), positionIds);
    }

    /// <summary>Whole-office master integrity, then the schedule of every employee placed or acting in this office.</summary>
    private async Task ValidateOfficeAsync(Guid officeId, AuthorizedEmployeeMutation mutation, CancellationToken ct)
    {
        var (snapshot, positionIds) = await LoadSnapshotAsync(officeId, ct);
        OrganizationIntegrityRules.Validate(snapshot);
        if (positionIds.Length == 0) return;
        var placed = await db.EmployeePositions.Where(x => positionIds.Contains(x.PositionId)
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).Select(x => x.EmployeeId).ToListAsync(ct);
        var acting = await db.EmployeeActingAssignments.Where(x => positionIds.Contains(x.TargetPositionId)
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).Select(x => x.EmployeeId).ToListAsync(ct);
        foreach (var employeeId in placed.Concat(acting).Distinct())
            await schedules.ValidateAsync(employeeId, mutation, ct);
    }
}
