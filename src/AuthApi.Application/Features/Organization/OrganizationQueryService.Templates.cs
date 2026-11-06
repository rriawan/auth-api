using AuthApi.Application.Features.Employees;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Organization;

public sealed partial class OrganizationQueryService
{
    public async Task<IReadOnlyList<TemplateSummaryDto>> ListTemplatesAsync(string? classificationCode, EmployeeActor actor,
        CancellationToken cancellationToken = default)
    {
        await support.AuthorizeReadAsync(actor, cancellationToken);
        var code = string.IsNullOrWhiteSpace(classificationCode) ? null : classificationCode.Trim();
        var templates = await db.StructureTemplates.AsNoTracking().Where(x => code == null || x.ClassificationCode == code)
            .OrderBy(x => x.ClassificationCode).ThenBy(x => x.Version).ToListAsync(cancellationToken);
        var unitCounts = await db.TemplateOrgUnits.AsNoTracking().GroupBy(x => x.TemplateId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        var positionCounts = await db.TemplatePositions.AsNoTracking().GroupBy(x => x.TemplateId)
            .Select(g => new { g.Key, Count = g.Count() }).ToDictionaryAsync(x => x.Key, x => x.Count, cancellationToken);
        return templates.Select(t => new TemplateSummaryDto(t.Id, t.ClassificationCode, t.Version, t.State.ToString(),
            unitCounts.GetValueOrDefault(t.Id), positionCounts.GetValueOrDefault(t.Id), t.Notes, t.PublishedAtUtc,
            EmployeeMutationSupport.ETag(t.RowVersion))).ToList();
    }

    public async Task<TemplateApplyPreviewDto> PreviewTemplateApplicationAsync(Guid officeId, Guid templateId, DateOnly effectiveFrom,
        EmployeeActor actor, CancellationToken cancellationToken = default)
    {
        var auth = await support.AuthorizeReadAsync(actor, cancellationToken);
        var plan = await new TemplateApplicationPlanner(db).BuildAsync(officeId, templateId, effectiveFrom, cancellationToken, auth.BusinessDate);
        var unitCodes = plan.Units.ToDictionary(x => x.Id, x => x.UnitCode);
        var positionCodes = plan.Positions.ToDictionary(x => x.Id, x => x.PositionCode);
        var units = TemplateApplicationPlanner.ParentFirst(plan.Units).Select(u => new TemplatePreviewUnitDto(u.UnitCode, u.Name, u.UnitType,
            u.ParentTemplateOrgUnitId is { } p ? unitCodes.GetValueOrDefault(p) : null,
            plan.Positions.Where(x => x.TemplateOrgUnitId == u.Id).OrderBy(x => x.PositionCode).Select(x => new TemplatePreviewPositionDto(
                x.PositionCode, x.Name, x.Track, x.IsUnitHead, x.MaxIncumbents,
                x.LevelId is { } l ? plan.LevelCodes.GetValueOrDefault(l) : null,
                x.ReportsToTemplatePositionId is { } r ? positionCodes.GetValueOrDefault(r) : null)).ToList())).ToList();
        return new TemplateApplyPreviewDto(officeId, plan.Office.OfficeCode, plan.OfficeName, templateId, plan.Template.ClassificationCode,
            plan.Template.Version, plan.Template.State.ToString(), effectiveFrom, plan.Units.Count, plan.Positions.Count,
            plan.Blockers.Count == 0, plan.Blockers, units);
    }
}
