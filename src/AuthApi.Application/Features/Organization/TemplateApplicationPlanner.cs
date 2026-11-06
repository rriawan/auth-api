using AuthApi.Application.Common.Interfaces;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums.Organization;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Organization;

/// <summary>Loaded template content plus the reasons it cannot be applied to a given office. Shared by preview and apply.</summary>
public sealed record TemplateApplicationPlan(Office Office, string? OfficeName, StructureTemplate Template,
    IReadOnlyList<TemplateOrgUnit> Units, IReadOnlyList<TemplatePosition> Positions, IReadOnlyDictionary<Guid, string> LevelCodes,
    IReadOnlyList<string> Blockers);

public sealed class TemplateApplicationPlanner(IAppDbContext db)
{
    public async Task<TemplateApplicationPlan> BuildAsync(Guid officeId, Guid templateId, DateOnly effectiveFrom, CancellationToken ct,
        DateOnly? businessDate = null)
    {
        var office = await db.Offices.AsNoTracking().SingleOrDefaultAsync(x => x.Id == officeId, ct)
            ?? throw new KeyNotFoundException("Office not found.");
        var template = await db.StructureTemplates.AsNoTracking().SingleOrDefaultAsync(x => x.Id == templateId, ct)
            ?? throw new KeyNotFoundException("Template not found.");
        var units = await db.TemplateOrgUnits.AsNoTracking().Where(x => x.TemplateId == templateId).ToListAsync(ct);
        var positions = await db.TemplatePositions.AsNoTracking().Where(x => x.TemplateId == templateId).ToListAsync(ct);
        var levelIds = positions.Where(x => x.LevelId != null).Select(x => x.LevelId!.Value).Distinct().ToList();
        var levels = await db.PositionLevels.AsNoTracking().Where(x => levelIds.Contains(x.Id)).ToDictionaryAsync(x => x.Id, x => x.Code, ct);

        var blockers = new List<string>();
        // Pratinjau mencerminkan aturan tanggal penerapan (apply memeriksanya sendiri).
        if (businessDate is { } today)
        {
            if (effectiveFrom < today) blockers.Add("Tanggal efektif tidak boleh di masa lalu.");
            else if (effectiveFrom > today.AddYears(5)) blockers.Add("Tanggal efektif tidak boleh lebih dari 5 tahun ke depan.");
        }
        if (template.State != StructureTemplateState.Published) blockers.Add("Template belum dipublikasikan.");

        var version = await db.OfficeVersions.AsNoTracking().Where(x => x.OfficeId == officeId && x.SupersededAtUtc == null
            && x.EffectiveFrom <= effectiveFrom && (x.EffectiveToExclusive == null || effectiveFrom < x.EffectiveToExclusive))
            .SingleOrDefaultAsync(ct);
        if (version is null) blockers.Add("Kantor tidak memiliki versi yang berlaku pada tanggal efektif.");
        else
        {
            if (!version.IsActive) blockers.Add("Kantor tidak aktif pada tanggal efektif.");
            if (!string.Equals(version.ClassificationCode, template.ClassificationCode, StringComparison.Ordinal))
                blockers.Add($"Klasifikasi kantor ({version.ClassificationCode}) berbeda dari template ({template.ClassificationCode}).");
        }
        if (await db.OrgUnits.AnyAsync(x => x.OfficeId == officeId, ct))
            blockers.Add("Kantor sudah memiliki unit atau jabatan; template hanya dapat diterapkan pada kantor kosong.");

        var unitIds = units.Select(x => x.Id).ToHashSet();
        var positionIds = positions.Select(x => x.Id).ToHashSet();
        if (units.Count == 0 || positions.Count == 0) blockers.Add("Template tidak memiliki unit atau jabatan.");
        if (units.Any(x => x.ParentTemplateOrgUnitId is { } p && !unitIds.Contains(p))
            || positions.Any(x => !unitIds.Contains(x.TemplateOrgUnitId) || (x.ReportsToTemplatePositionId is { } r && !positionIds.Contains(r))))
            blockers.Add("Referensi internal template tidak konsisten.");
        if (positions.Any(x => x.LevelId is { } l && !levels.ContainsKey(l))) blockers.Add("Level jabatan pada template tidak ditemukan.");
        return new TemplateApplicationPlan(office, version?.Name, template, units, positions, levels, blockers);
    }

    /// <summary>Units ordered so a parent always precedes its children (unreachable/cyclic units are excluded; callers reject them).</summary>
    public static List<TemplateOrgUnit> ParentFirst(IReadOnlyList<TemplateOrgUnit> units)
    {
        var ordered = new List<TemplateOrgUnit>(units.Count);
        var placed = new HashSet<Guid>();
        var pending = units.OrderBy(x => x.UnitCode, StringComparer.Ordinal).ToList();
        while (pending.Count > 0)
        {
            var next = pending.Where(x => x.ParentTemplateOrgUnitId is null || placed.Contains(x.ParentTemplateOrgUnitId.Value)).ToList();
            if (next.Count == 0) break;
            foreach (var u in next) { ordered.Add(u); placed.Add(u.Id); }
            pending = pending.Except(next).ToList();
        }
        return ordered;
    }
}
