using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AuthApi.Application.Common.Exceptions;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Employees;

public sealed record SelfEmployeeDto(Guid Id, string EmployeeNumber, string EmployeeFullName, string Status);
public sealed record SelfOfficeDto(Guid Id, string Code, string Name, string Classification);
public sealed record SelfUnitDto(Guid Id, string Name);
public sealed record SelfPositionDto(Guid Id, string Name, string Track, string? LevelCode, bool IsUnitHead);
public sealed record SelfSupervisorDto(Guid PositionId, string PositionName, bool IsVacant);

/// <param name="Id">Stable logical id (AssignmentKey), not the revision row id.</param>
/// <param name="Type">DEFINITIVE, PLH or PLT.</param>
public sealed record SelfAssignmentDto(Guid Id, string Type, bool IsPrimary, DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive,
    Guid? SourceAssignmentId, string? DecisionReference, SelfOfficeDto Office, SelfUnitDto OrgUnit, SelfPositionDto Position,
    SelfSupervisorDto? FormalSupervisor);

public sealed record SelfOrganizationProfileDto(Guid UserId, string AccountDisplayName, DateOnly AsOf, string BusinessTimeZone,
    string ProfileVersion, DateTime? NextEffectiveChangeAtUtc, SelfEmployeeDto? Employee, Guid? PrimaryAssignmentId,
    IReadOnlyList<SelfAssignmentDto> Assignments);

public sealed record ReportingNodeDto(int Depth, Guid PositionId, string PositionName, string Track, string? LevelCode, string OfficeCode, bool IsVacant);
public sealed record SelfReportingLineDto(Guid AssignmentId, DateOnly AsOf, IReadOnlyList<ReportingNodeDto> Positions);

public interface ISelfOrganizationProfileService
{
    Task<SelfOrganizationProfileDto> GetAsync(Guid userId, string clientId, CancellationToken cancellationToken = default);
    Task<SelfReportingLineDto> GetReportingLineAsync(Guid userId, string clientId, Guid assignmentId, CancellationToken cancellationToken = default);
}

/// <summary>
/// Organization profile of the authenticated account only (planning §8.1). Everything is read fresh from the database:
/// account state, the token's application, the account's role on that application and the linked employee's effective status.
/// The profile is reference data; it never carries permissions, approval authority or other people's identities.
/// </summary>
public sealed class SelfOrganizationProfileService(IAppDbContext db, TimeProvider clock) : ISelfOrganizationProfileService
{
    public const string BusinessTimeZoneId = "Asia/Jakarta";
    public const int MaxReportingDepth = 32;
    private static readonly TimeZoneInfo BusinessTimeZone = TimeZoneInfo.FindSystemTimeZoneById(BusinessTimeZoneId);
    private static readonly JsonSerializerOptions HashJson = new(JsonSerializerDefaults.Web);

    public async Task<SelfOrganizationProfileDto> GetAsync(Guid userId, string clientId, CancellationToken cancellationToken = default) =>
        (await BuildAsync(userId, clientId, cancellationToken)).Profile;

    public async Task<SelfReportingLineDto> GetReportingLineAsync(Guid userId, string clientId, Guid assignmentId,
        CancellationToken cancellationToken = default)
    {
        var (profile, _) = await BuildAsync(userId, clientId, cancellationToken);
        // Unknown and foreign ids are indistinguishable on purpose.
        var assignment = profile.Assignments.SingleOrDefault(x => x.Id == assignmentId)
            ?? throw new KeyNotFoundException("Assignment not found.");
        var date = profile.AsOf;

        var officeId = assignment.Office.Id;
        var unitIds = await db.OrgUnits.AsNoTracking().Where(x => x.OfficeId == officeId).Select(x => x.Id).ToListAsync(cancellationToken);
        var positions = await db.Positions.AsNoTracking().Where(x => unitIds.Contains(x.OrgUnitId)).ToListAsync(cancellationToken);
        var positionIds = positions.Select(x => x.Id).ToArray();
        var versions = (await db.PositionVersions.AsNoTracking().Include(x => x.Level)
            .Where(v => positionIds.Contains(v.PositionId) && v.SupersededAtUtc == null
                && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive))
            .ToListAsync(cancellationToken)).ToDictionary(x => x.PositionId);
        var occupied = (await db.EmployeePositions.AsNoTracking().Where(x => positionIds.Contains(x.PositionId)
                && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= date && (x.EffectiveToExclusive == null || date < x.EffectiveToExclusive))
            .Select(x => x.PositionId).Distinct().ToListAsync(cancellationToken)).ToHashSet();
        var officeCode = await db.Offices.AsNoTracking().Where(x => x.Id == officeId).Select(x => x.OfficeCode).SingleAsync(cancellationToken);

        var chain = new List<ReportingNodeDto>();
        var seen = new HashSet<Guid>();
        Guid? current = versions.GetValueOrDefault(assignment.Position.Id)?.ReportsToPositionId;
        while (current is { } id && chain.Count < MaxReportingDepth && seen.Add(id) && versions.TryGetValue(id, out var v))
        {
            chain.Add(new ReportingNodeDto(chain.Count + 1, id, v.Name, v.Track.ToString().ToUpperInvariant(), v.Level?.Code, officeCode,
                !occupied.Contains(id)));
            current = v.ReportsToPositionId;
        }
        return new SelfReportingLineDto(assignmentId, date, chain);
    }

    private async Task<(SelfOrganizationProfileDto Profile, DateOnly AsOf)> BuildAsync(Guid userId, string clientId, CancellationToken ct)
    {
        var now = clock.GetUtcNow().UtcDateTime;
        var asOf = DateOnly.FromDateTime(TimeZoneInfo.ConvertTimeFromUtc(now, BusinessTimeZone));

        var user = await db.Users.AsNoTracking().SingleOrDefaultAsync(x => x.Id == userId, ct);
        if (user is null || !user.IsActive || !user.IsAccountVerified || user.IsLockedOut(now))
            throw new ForbiddenAccessException("Akun tidak aktif, belum terverifikasi, atau terkunci.");
        var app = await db.ClientApps.AsNoTracking().SingleOrDefaultAsync(x => x.ClientId == clientId && x.IsActive, ct)
            ?? throw new ForbiddenAccessException("Aplikasi tujuan token tidak terdaftar atau tidak aktif.");
        if (!await db.UserAppRoles.AsNoTracking().AnyAsync(x => x.UserId == userId && x.ClientAppId == app.Id, ct))
            throw new ForbiddenAccessException("Akun tidak memiliki akses ke aplikasi ini.");

        var employeeId = await db.UserEmployeeLinks.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.EmployeeId).SingleOrDefaultAsync(ct);
        if (employeeId is null)
            return Finish(userId, user.FullName, asOf, null, null, [], null);

        var employee = await db.Employees.AsNoTracking().SingleAsync(x => x.Id == employeeId, ct);
        var statuses = await db.EmployeeStatuses.AsNoTracking().Where(x => x.EmployeeId == employee.Id
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(ct);
        var effectiveStatuses = statuses.Where(x => Contains(x.EffectiveFrom, x.EffectiveToExclusive, asOf)).ToList();
        var status = effectiveStatuses.Count == 1 ? effectiveStatuses[0] : null;
        if (status is null || status.StatusCode != EmployeeStatusCode.Active)
            throw new ForbiddenAccessException("Pegawai yang terhubung dengan akun ini tidak aktif.");

        var placements = await db.EmployeePositions.AsNoTracking().Where(x => x.EmployeeId == employee.Id
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(ct);
        var acting = await db.EmployeeActingAssignments.AsNoTracking().Where(x => x.EmployeeId == employee.Id
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null).ToListAsync(ct);
        var effectivePlacements = placements.Where(x => Contains(x.EffectiveFrom, x.EffectiveToExclusive, asOf)).ToList();
        var effectiveActing = acting.Where(x => Contains(x.EffectiveFrom, x.EffectiveToExclusive, asOf)).ToList();

        var positionIds = effectivePlacements.Select(x => x.PositionId).Concat(effectiveActing.Select(x => x.TargetPositionId)).Distinct().ToArray();
        var positions = await db.Positions.AsNoTracking().Where(x => positionIds.Contains(x.Id)).ToListAsync(ct);
        var unitIds = positions.Select(x => x.OrgUnitId).Distinct().ToArray();
        var units = await db.OrgUnits.AsNoTracking().Where(x => unitIds.Contains(x.Id)).ToListAsync(ct);
        var officeIds = units.Select(x => x.OfficeId).Distinct().ToArray();
        var offices = await db.Offices.AsNoTracking().Where(x => officeIds.Contains(x.Id)).ToListAsync(ct);
        var positionVersions = await db.PositionVersions.AsNoTracking().Include(x => x.Level)
            .Where(x => positionIds.Contains(x.PositionId) && x.SupersededAtUtc == null).ToListAsync(ct);
        var unitVersions = await db.OrgUnitVersions.AsNoTracking().Where(x => unitIds.Contains(x.OrgUnitId) && x.SupersededAtUtc == null).ToListAsync(ct);
        var officeVersions = await db.OfficeVersions.AsNoTracking().Where(x => officeIds.Contains(x.OfficeId) && x.SupersededAtUtc == null).ToListAsync(ct);

        var supervisorIds = positionVersions.Where(v => Contains(v.EffectiveFrom, v.EffectiveToExclusive, asOf) && v.ReportsToPositionId.HasValue)
            .Select(v => v.ReportsToPositionId!.Value).Distinct().ToArray();
        var supervisorAll = await db.PositionVersions.AsNoTracking().Where(v => supervisorIds.Contains(v.PositionId) && v.SupersededAtUtc == null).ToListAsync(ct);
        var supervisorVersions = (await db.PositionVersions.AsNoTracking().Where(v => supervisorIds.Contains(v.PositionId)
                && v.SupersededAtUtc == null && v.EffectiveFrom <= asOf && (v.EffectiveToExclusive == null || asOf < v.EffectiveToExclusive))
            .ToListAsync(ct)).ToDictionary(x => x.PositionId);
        var supervisorOccupied = (await db.EmployeePositions.AsNoTracking().Where(x => supervisorIds.Contains(x.PositionId)
                && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= asOf && (x.EffectiveToExclusive == null || asOf < x.EffectiveToExclusive))
            .Select(x => x.PositionId).Distinct().ToListAsync(ct)).ToHashSet();

        SelfAssignmentDto Compose(Guid key, string type, bool primary, DateOnly from, DateOnly? to, Guid? source, string? reference, Guid positionId)
        {
            var position = positions.Single(x => x.Id == positionId);
            var unit = units.Single(x => x.Id == position.OrgUnitId);
            var office = offices.Single(x => x.Id == unit.OfficeId);
            var pv = positionVersions.SingleOrDefault(v => v.PositionId == positionId && Contains(v.EffectiveFrom, v.EffectiveToExclusive, asOf)) ?? throw Inconsistent();
            var uv = unitVersions.SingleOrDefault(v => v.OrgUnitId == unit.Id && Contains(v.EffectiveFrom, v.EffectiveToExclusive, asOf)) ?? throw Inconsistent();
            var ov = officeVersions.SingleOrDefault(v => v.OfficeId == office.Id && Contains(v.EffectiveFrom, v.EffectiveToExclusive, asOf)) ?? throw Inconsistent();
            SelfSupervisorDto? supervisor = null;
            if (pv.ReportsToPositionId is { } sid && supervisorVersions.TryGetValue(sid, out var sv))
                supervisor = new SelfSupervisorDto(sid, sv.Name, !supervisorOccupied.Contains(sid));
            return new SelfAssignmentDto(key, type, primary, from, to, source, reference,
                new SelfOfficeDto(office.Id, office.OfficeCode, ov.Name, ov.ClassificationCode),
                new SelfUnitDto(unit.Id, uv.Name),
                new SelfPositionDto(positionId, pv.Name, pv.Track.ToString().ToUpperInvariant(), pv.Level?.Code, pv.IsUnitHead),
                supervisor);
        }

        var assignments = new List<SelfAssignmentDto>();
        foreach (var p in effectivePlacements.OrderByDescending(x => x.IsPrimary).ThenBy(x => x.EffectiveFrom).ThenBy(x => x.AssignmentKey))
            assignments.Add(Compose(p.AssignmentKey, "DEFINITIVE", p.IsPrimary, p.EffectiveFrom, p.EffectiveToExclusive, null, p.DecisionReference, p.PositionId));
        foreach (var a in effectiveActing.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.AssignmentKey))
        {
            var sourceKey = placements.SingleOrDefault(x => x.Id == a.SourceEmployeePositionId)?.AssignmentKey;
            assignments.Add(Compose(a.AssignmentKey, a.AssignmentType.ToString().ToUpperInvariant(), false, a.EffectiveFrom,
                a.EffectiveToExclusive, sourceKey, a.DecisionReference, a.TargetPositionId));
        }

        // Next day on which anything shown here can change (assignments, status, or master versions).
        var boundaries = new List<DateOnly>();
        void Collect(DateOnly from, DateOnly? to)
        {
            if (from > asOf) boundaries.Add(from);
            if (to is { } end && end > asOf) boundaries.Add(end);
        }
        foreach (var x in statuses) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        foreach (var x in placements) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        foreach (var x in acting) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        foreach (var x in positionVersions) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        foreach (var x in unitVersions) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        foreach (var x in officeVersions) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        foreach (var x in supervisorAll) Collect(x.EffectiveFrom, x.EffectiveToExclusive);
        DateTime? next = boundaries.Count == 0 ? null
            : TimeZoneInfo.ConvertTimeToUtc(boundaries.Min().ToDateTime(TimeOnly.MinValue), BusinessTimeZone);

        var primaryKey = assignments.FirstOrDefault(x => x.IsPrimary)?.Id;
        return Finish(userId, user.FullName, asOf,
            new SelfEmployeeDto(employee.Id, employee.EmployeeNumber, employee.EmployeeFullName, status.StatusCode.ToString().ToUpperInvariant()),
            primaryKey, assignments, next);
    }

    private static (SelfOrganizationProfileDto Profile, DateOnly AsOf) Finish(Guid userId, string displayName, DateOnly asOf,
        SelfEmployeeDto? employee, Guid? primaryKey, IReadOnlyList<SelfAssignmentDto> assignments, DateTime? next)
    {
        var draft = new SelfOrganizationProfileDto(userId, displayName, asOf, BusinessTimeZoneId, string.Empty, next, employee, primaryKey, assignments);
        // Opaque strong version over the whole content including asOf, so a 304 can never leave a client with yesterday's date.
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(draft, HashJson)));
        var version = Convert.ToHexString(hash, 0, 16).ToLowerInvariant();
        return (draft with { ProfileVersion = version }, asOf);
    }

    private static InvalidOperationException Inconsistent() =>
        new("Data organisasi untuk penempatan ini belum konsisten. Hubungi administrator.");

    private static bool Contains(DateOnly from, DateOnly? toExclusive, DateOnly date) => from <= date && (toExclusive is null || date < toExclusive);
}
