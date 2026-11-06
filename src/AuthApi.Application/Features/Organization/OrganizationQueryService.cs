using AuthApi.Application.Common.Interfaces;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Organization;

/// <summary>Administrator read side of the organization master. Authorization is re-checked in the database on every call.</summary>
public sealed partial class OrganizationQueryService(IAppDbContext db, EmployeeMutationSupport support) : IOrganizationQueryService
{
    public const int MaxPageSize = 100;
    public const int MaxPage = 1_000_000;

    public async Task<IReadOnlyList<ClassificationDto>> ListClassificationsAsync(EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        return await db.OfficeClassifications.AsNoTracking().OrderBy(x => x.Code)
            .Select(x => new ClassificationDto(x.Code, x.DisplayName)).ToListAsync(cancellationToken);
    }

    public async Task<OfficePageDto> ListOfficesAsync(string? search, string? classificationCode, DateOnly? asOf, int page, int pageSize,
        EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        var access = await support.AuthorizeReadAsync(actor, cancellationToken);
        if (page is < 1 or > MaxPage) throw new ArgumentException($"Page must be between 1 and {MaxPage}.", nameof(page));
        if (pageSize is < 1 or > MaxPageSize) throw new ArgumentException($"Page size must be between 1 and {MaxPageSize}.", nameof(pageSize));
        var date = asOf ?? access.BusinessDate;

        var versions = db.OfficeVersions.AsNoTracking().Where(v => v.SupersededAtUtc == null
            && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive));
        var query = db.Offices.AsNoTracking().Select(o => new { Office = o, Version = versions.FirstOrDefault(v => v.OfficeId == o.Id) });
        if (!string.IsNullOrWhiteSpace(classificationCode))
        {
            var code = classificationCode.Trim();
            query = query.Where(x => x.Version != null && x.Version.ClassificationCode == code);
        }
        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim();
            if (term.Length > 100) throw new ArgumentException("Search text must not exceed 100 characters.", nameof(search));
            query = query.Where(x => x.Office.OfficeCode.Contains(term) || (x.Version != null && x.Version.Name.Contains(term)));
        }

        var total = await query.CountAsync(cancellationToken);
        var rows = await query.OrderBy(x => x.Office.OfficeCode).Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        var items = rows.Select(x => new OfficeSummaryDto(x.Office.Id, x.Office.OfficeCode, x.Version?.Name,
            x.Version?.ClassificationCode, x.Version?.IsActive, EmployeeMutationSupport.ETag(x.Office.RowVersion))).ToList();
        return new OfficePageDto(items, page, pageSize, total, date);
    }

    public async Task<OfficeDetailDto> GetOfficeAsync(Guid id, bool includeHistory, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        var office = await db.Offices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Office not found.");
        var rows = await db.OfficeVersions.AsNoTracking().Where(x => x.OfficeId == id && (includeHistory || x.SupersededAtUtc == null))
            .ToListAsync(cancellationToken);
        return new OfficeDetailDto(office.Id, office.OfficeCode, EmployeeMutationSupport.ETag(office.RowVersion),
            rows.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.RevisionNumber).Select(x => new OfficeVersionDto(x.Id, x.ClassificationCode, x.Name,
                x.EffectiveFrom, x.EffectiveToExclusive, x.IsActive, x.RevisionNumber, x.ChangeReason, x.DecisionReference,
                x.CreatedAtUtc, x.SupersededAtUtc)).ToList());
    }

    public async Task<IReadOnlyList<UnitSummaryDto>> ListUnitsAsync(Guid officeId, DateOnly? asOf, EmployeeActor actor,
        CancellationToken cancellationToken = default)
    {
        var access = await support.AuthorizeReadAsync(actor, cancellationToken);
        var date = asOf ?? access.BusinessDate;
        if (!await db.Offices.AnyAsync(x => x.Id == officeId, cancellationToken)) throw new KeyNotFoundException("Office not found.");
        var units = await db.OrgUnits.AsNoTracking().Where(x => x.OfficeId == officeId).OrderBy(x => x.UnitCode).ToListAsync(cancellationToken);
        var ids = units.Select(x => x.Id).ToArray();
        var versions = await db.OrgUnitVersions.AsNoTracking().Where(v => ids.Contains(v.OrgUnitId) && v.SupersededAtUtc == null
            && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive)).ToListAsync(cancellationToken);
        return units.Select(u =>
        {
            var v = versions.SingleOrDefault(x => x.OrgUnitId == u.Id);
            return new UnitSummaryDto(u.Id, u.OfficeId, u.UnitCode, v?.ParentOrgUnitId, v?.UnitType, v?.Name, v?.IsActive,
                EmployeeMutationSupport.ETag(u.RowVersion));
        }).ToList();
    }

    public async Task<UnitDetailDto> GetUnitAsync(Guid id, bool includeHistory, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        var unit = await db.OrgUnits.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Unit not found.");
        var rows = await db.OrgUnitVersions.AsNoTracking().Where(x => x.OrgUnitId == id && (includeHistory || x.SupersededAtUtc == null))
            .ToListAsync(cancellationToken);
        return new UnitDetailDto(unit.Id, unit.OfficeId, unit.UnitCode, EmployeeMutationSupport.ETag(unit.RowVersion),
            rows.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.RevisionNumber).Select(x => new UnitVersionDto(x.Id, x.ParentOrgUnitId, x.UnitType,
                x.Name, x.EffectiveFrom, x.EffectiveToExclusive, x.IsActive, x.RevisionNumber, x.ChangeReason, x.DecisionReference,
                x.CreatedAtUtc, x.SupersededAtUtc)).ToList());
    }

    public async Task<IReadOnlyList<PositionSummaryDto>> ListPositionsAsync(Guid? officeId, Guid? unitId, DateOnly? asOf, EmployeeActor actor,
        CancellationToken cancellationToken = default)
    {
        var access = await support.AuthorizeReadAsync(actor, cancellationToken);
        if (officeId is null && unitId is null) throw new ArgumentException("Supply officeId or unitId.", nameof(officeId));
        var date = asOf ?? access.BusinessDate;
        if (unitId is { } u && !await db.OrgUnits.AnyAsync(x => x.Id == u && (officeId == null || x.OfficeId == officeId), cancellationToken))
            throw new KeyNotFoundException("Unit not found in the requested office.");
        if (unitId is null && officeId is { } o && !await db.Offices.AnyAsync(x => x.Id == o, cancellationToken))
            throw new KeyNotFoundException("Office not found.");
        var query = db.Positions.AsNoTracking().AsQueryable();
        if (unitId is { } unit) query = query.Where(x => x.OrgUnitId == unit);
        if (officeId is { } office) query = query.Where(x => x.OrgUnit.OfficeId == office);
        var positions = await query.OrderBy(x => x.PositionCode).ToListAsync(cancellationToken);
        var ids = positions.Select(x => x.Id).ToArray();
        var versions = await db.PositionVersions.AsNoTracking().Where(v => ids.Contains(v.PositionId) && v.SupersededAtUtc == null
            && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive)).ToListAsync(cancellationToken);
        return positions.Select(p =>
        {
            var v = versions.SingleOrDefault(x => x.PositionId == p.Id);
            return new PositionSummaryDto(p.Id, p.OrgUnitId, p.PositionCode, v?.Name, v?.Track, v?.IsUnitHead, v?.MaxIncumbents,
                v?.ReportsToPositionId, v?.IsActive, EmployeeMutationSupport.ETag(p.RowVersion), v?.LevelId);
        }).ToList();
    }

    public async Task<PositionDetailDto> GetPositionAsync(Guid id, bool includeHistory, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        var position = await db.Positions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
            ?? throw new KeyNotFoundException("Position not found.");
        var rows = await db.PositionVersions.AsNoTracking().Where(x => x.PositionId == id && (includeHistory || x.SupersededAtUtc == null))
            .ToListAsync(cancellationToken);
        return new PositionDetailDto(position.Id, position.OrgUnitId, position.PositionCode, EmployeeMutationSupport.ETag(position.RowVersion),
            rows.OrderBy(x => x.EffectiveFrom).ThenBy(x => x.RevisionNumber).Select(x => new PositionVersionDto(x.Id, x.Name, x.Track, x.IsUnitHead,
                x.MaxIncumbents, x.LevelId, x.ReportsToPositionId, x.EffectiveFrom, x.EffectiveToExclusive, x.IsActive, x.RevisionNumber,
                x.ChangeReason, x.DecisionReference, x.CreatedAtUtc, x.SupersededAtUtc)).ToList());
    }

    public async Task<OfficeStructureDto> GetStructureAsync(Guid officeId, DateOnly? asOf, EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        var access = await support.AuthorizeReadAsync(actor, cancellationToken);
        var date = asOf ?? access.BusinessDate;
        var office = await db.Offices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == officeId, cancellationToken)
            ?? throw new KeyNotFoundException("Office not found.");
        var officeName = await db.OfficeVersions.AsNoTracking().Where(v => v.OfficeId == officeId && v.SupersededAtUtc == null
            && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive))
            .Select(v => v.Name).FirstOrDefaultAsync(cancellationToken);

        var units = await db.OrgUnits.AsNoTracking().Where(x => x.OfficeId == officeId).ToListAsync(cancellationToken);
        var unitIds = units.Select(x => x.Id).ToArray();
        var unitVersions = (await db.OrgUnitVersions.AsNoTracking().Where(v => unitIds.Contains(v.OrgUnitId) && v.SupersededAtUtc == null
            && v.IsActive && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive))
            .ToListAsync(cancellationToken)).ToDictionary(x => x.OrgUnitId);
        var positions = await db.Positions.AsNoTracking().Where(x => unitIds.Contains(x.OrgUnitId)).ToListAsync(cancellationToken);
        var positionIds = positions.Select(x => x.Id).ToArray();
        var positionVersions = (await db.PositionVersions.AsNoTracking().Where(v => positionIds.Contains(v.PositionId) && v.SupersededAtUtc == null
            && v.IsActive && v.EffectiveFrom <= date && (v.EffectiveToExclusive == null || date < v.EffectiveToExclusive))
            .ToListAsync(cancellationToken)).ToDictionary(x => x.PositionId);
        var incumbents = (await db.EmployeePositions.AsNoTracking().Where(x => positionIds.Contains(x.PositionId)
            && x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc == null
            && x.EffectiveFrom <= date && (x.EffectiveToExclusive == null || date < x.EffectiveToExclusive))
            .GroupBy(x => x.PositionId).Select(g => new { g.Key, Count = g.Count() }).ToListAsync(cancellationToken))
            .ToDictionary(x => x.Key, x => x.Count);

        StructureUnitNode Build(OrgUnit unit)
        {
            var v = unitVersions[unit.Id];
            var nodePositions = positions.Where(p => p.OrgUnitId == unit.Id && positionVersions.ContainsKey(p.Id)).OrderBy(p => p.PositionCode)
                .Select(p =>
                {
                    var pv = positionVersions[p.Id];
                    var count = incumbents.GetValueOrDefault(p.Id);
                    return new StructurePositionNode(p.Id, p.PositionCode, pv.Name, pv.Track, pv.IsUnitHead, pv.MaxIncumbents,
                        pv.ReportsToPositionId, count, count == 0);
                }).ToList();
            var children = units.Where(c => unitVersions.TryGetValue(c.Id, out var cv) && cv.ParentOrgUnitId == unit.Id)
                .OrderBy(c => c.UnitCode).Select(Build).ToList();
            return new StructureUnitNode(unit.Id, unit.UnitCode, v.Name, v.UnitType, nodePositions, children);
        }

        // Depth is bounded by the integrity rules (acyclic); roots are active units without an active parent in this office.
        var roots = units.Where(u => unitVersions.TryGetValue(u.Id, out var v) && (v.ParentOrgUnitId is null || !unitVersions.ContainsKey(v.ParentOrgUnitId.Value)))
            .OrderBy(u => u.UnitCode).Select(Build).ToList();
        return new OfficeStructureDto(office.Id, office.OfficeCode, officeName, date, roots);
    }
}
