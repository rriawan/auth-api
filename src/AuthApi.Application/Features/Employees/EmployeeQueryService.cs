using System.Text.Json.Serialization;
using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Employees;

public sealed record EmployeePage(IReadOnlyList<EmployeeSummary> Items, int Page, int PageSize, int TotalCount);

public sealed record StatusRecordDto(Guid Id, [property: JsonConverter(typeof(JsonStringEnumConverter))] EmployeeStatusCode StatusCode,
    DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] EmployeeRecordState RecordState, int RevisionNumber, string ChangeReason, string? DecisionReference,
    DateTime CreatedAtUtc, DateTime? SupersededAtUtc);

public sealed record PlacementRecordDto(Guid Id, Guid AssignmentKey, Guid PositionId, string PositionCode, bool IsPrimary,
    DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] EmployeeRecordState RecordState, int RevisionNumber,
    string ChangeReason, string? DecisionReference, DateTime CreatedAtUtc, DateTime? SupersededAtUtc);

public sealed record ActingRecordDto(Guid Id, Guid AssignmentKey, Guid SourceRecordId, Guid TargetPositionId, string TargetPositionCode,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] ActingAssignmentType AssignmentType, DateOnly EffectiveFrom, DateOnly? EffectiveToExclusive,
    [property: JsonConverter(typeof(JsonStringEnumConverter))] EmployeeRecordState RecordState,
    int RevisionNumber, string ChangeReason, string DecisionReference, DateTime CreatedAtUtc, DateTime? SupersededAtUtc);

public sealed record EmployeeDetail(EmployeeSummary Employee, IReadOnlyList<StatusRecordDto> Statuses,
    IReadOnlyList<PlacementRecordDto> Placements, IReadOnlyList<ActingRecordDto> ActingAssignments);

public sealed record EmployeeHistory(Guid EmployeeId, IReadOnlyList<StatusRecordDto> Statuses,
    IReadOnlyList<PlacementRecordDto> Placements, IReadOnlyList<ActingRecordDto> ActingAssignments);

public interface IEmployeeQueryService
{
    Task<EmployeePage> ListAsync(string? search, int page, int pageSize, EmployeeActor actor, CancellationToken cancellationToken = default);
    /// <summary>Current (non-superseded) confirmed records only.</summary>
    Task<EmployeeDetail> GetAsync(Guid employeeId, EmployeeActor actor, CancellationToken cancellationToken = default);
    /// <summary>Every revision, including superseded and cancelled rows.</summary>
    Task<EmployeeHistory> GetHistoryAsync(Guid employeeId, EmployeeActor actor, CancellationToken cancellationToken = default);
    Task<Guid?> FindEmployeeIdByUserAsync(Guid userId, EmployeeActor actor, CancellationToken cancellationToken = default);
    /// <summary>Detail of the employee currently linked to the account, or null when the account has no link.</summary>
    Task<EmployeeDetail?> GetByUserAsync(Guid userId, EmployeeActor actor, CancellationToken cancellationToken = default);
}

/// <summary>
/// Read side for administrators. Authorization is re-evaluated against the database on every call
/// (never from JWT claims). Employee data is personal data: callers must not cache it in shared caches.
/// </summary>
public sealed class EmployeeQueryService(IAppDbContext db, EmployeeMutationSupport support) : IEmployeeQueryService
{
    public const int MaxPageSize = 100;
    public const int MaxPage = 1_000_000;

    public async Task<EmployeePage> ListAsync(string? search, int page, int pageSize, EmployeeActor actor,
        CancellationToken cancellationToken = default)
    {
        var access = await support.AuthorizeReadAsync(actor, cancellationToken);
        if (page is < 1 or > MaxPage) throw new ArgumentException($"Page must be between 1 and {MaxPage}.", nameof(page));
        if (pageSize is < 1 or > MaxPageSize) throw new ArgumentException($"Page size must be between 1 and {MaxPageSize}.", nameof(pageSize));
        var query = db.Employees.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100) throw new ArgumentException("Search text must not exceed 100 characters.", nameof(search));
            query = query.Where(x => x.EmployeeNumber.Contains(term) || x.EmployeeFullName.Contains(term));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.EmployeeNumber).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var ids = rows.Select(x => x.Id).ToArray();
        var statuses = await db.EmployeeStatuses.AsNoTracking()
            .Where(x => ids.Contains(x.EmployeeId) && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= access.BusinessDate && (x.EffectiveToExclusive == null || access.BusinessDate < x.EffectiveToExclusive))
            .ToListAsync(cancellationToken);
        var links = await db.UserEmployeeLinks.AsNoTracking().Where(x => ids.Contains(x.EmployeeId)).ToListAsync(cancellationToken);
        var items = rows.Select(e => ToSummary(e,
            statuses.SingleOrDefault(s => s.EmployeeId == e.Id)?.StatusCode,
            links.SingleOrDefault(l => l.EmployeeId == e.Id)?.UserId)).ToList();
        return new EmployeePage(items, page, pageSize, total);
    }

    public async Task<EmployeeDetail> GetAsync(Guid employeeId, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        var access = await support.AuthorizeReadAsync(actor, cancellationToken);
        var summary = await LoadSummaryAsync(employeeId, access.BusinessDate, cancellationToken);
        var history = await LoadRecordsAsync(employeeId, currentOnly: true, cancellationToken);
        return new EmployeeDetail(summary, history.Statuses, history.Placements, history.Acting);
    }

    public async Task<EmployeeHistory> GetHistoryAsync(Guid employeeId, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        if (!await db.Employees.AnyAsync(x => x.Id == employeeId, cancellationToken)) throw new KeyNotFoundException("Employee not found.");
        var records = await LoadRecordsAsync(employeeId, currentOnly: false, cancellationToken);
        return new EmployeeHistory(employeeId, records.Statuses, records.Placements, records.Acting);
    }

    public async Task<EmployeeDetail?> GetByUserAsync(Guid userId, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        var employeeId = await FindEmployeeIdByUserAsync(userId, actor, cancellationToken);
        return employeeId is null ? null : await GetAsync(employeeId.Value, actor, cancellationToken);
    }

    public async Task<Guid?> FindEmployeeIdByUserAsync(Guid userId, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        return await db.UserEmployeeLinks.AsNoTracking().Where(x => x.UserId == userId)
            .Select(x => (Guid?)x.EmployeeId).SingleOrDefaultAsync(cancellationToken);
    }

    private async Task<EmployeeSummary> LoadSummaryAsync(Guid employeeId, DateOnly businessDate, CancellationToken ct)
    {
        var employee = await db.Employees.AsNoTracking().SingleOrDefaultAsync(x => x.Id == employeeId, ct)
            ?? throw new KeyNotFoundException("Employee not found.");
        var status = await db.EmployeeStatuses.AsNoTracking()
            .Where(x => x.EmployeeId == employeeId && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
                && x.EffectiveFrom <= businessDate && (x.EffectiveToExclusive == null || businessDate < x.EffectiveToExclusive))
            .Select(x => (EmployeeStatusCode?)x.StatusCode).SingleOrDefaultAsync(ct);
        var userId = await db.UserEmployeeLinks.AsNoTracking().Where(x => x.EmployeeId == employeeId)
            .Select(x => (Guid?)x.UserId).SingleOrDefaultAsync(ct);
        return ToSummary(employee, status, userId);
    }

    private async Task<(List<StatusRecordDto> Statuses, List<PlacementRecordDto> Placements, List<ActingRecordDto> Acting)> LoadRecordsAsync(
        Guid employeeId, bool currentOnly, CancellationToken ct)
    {
        var statusQuery = db.EmployeeStatuses.AsNoTracking().Where(x => x.EmployeeId == employeeId);
        var placementQuery = db.EmployeePositions.AsNoTracking().Include(x => x.Position).Where(x => x.EmployeeId == employeeId);
        var actingQuery = db.EmployeeActingAssignments.AsNoTracking().Include(x => x.TargetPosition).Where(x => x.EmployeeId == employeeId);
        if (currentOnly)
        {
            statusQuery = statusQuery.Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null);
            placementQuery = placementQuery.Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null);
            actingQuery = actingQuery.Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null);
        }

        var statuses = (await statusQuery.ToListAsync(ct)).OrderBy(x => x.EffectiveFrom).ThenBy(x => x.RevisionNumber)
            .Select(x => new StatusRecordDto(x.Id, x.StatusCode, x.EffectiveFrom, x.EffectiveToExclusive, x.RecordState,
                x.RevisionNumber, x.ChangeReason, x.DecisionReference, x.CreatedAtUtc, x.SupersededAtUtc)).ToList();
        var placements = (await placementQuery.ToListAsync(ct)).OrderBy(x => x.EffectiveFrom).ThenBy(x => x.RevisionNumber)
            .Select(x => new PlacementRecordDto(x.Id, x.AssignmentKey, x.PositionId, x.Position.PositionCode, x.IsPrimary,
                x.EffectiveFrom, x.EffectiveToExclusive, x.RecordState, x.RevisionNumber, x.ChangeReason, x.DecisionReference,
                x.CreatedAtUtc, x.SupersededAtUtc)).ToList();
        var acting = (await actingQuery.ToListAsync(ct)).OrderBy(x => x.EffectiveFrom).ThenBy(x => x.RevisionNumber)
            .Select(x => new ActingRecordDto(x.Id, x.AssignmentKey, x.SourceEmployeePositionId, x.TargetPositionId,
                x.TargetPosition.PositionCode, x.AssignmentType, x.EffectiveFrom, x.EffectiveToExclusive, x.RecordState,
                x.RevisionNumber, x.ChangeReason, x.DecisionReference, x.CreatedAtUtc, x.SupersededAtUtc)).ToList();
        return (statuses, placements, acting);
    }

    private static EmployeeSummary ToSummary(Employee employee, EmployeeStatusCode? status, Guid? userId) =>
        new(employee.Id, employee.EmployeeNumber, employee.EmployeeFullName,
            '"' + Convert.ToBase64String(employee.RowVersion) + '"', status, userId);
}
