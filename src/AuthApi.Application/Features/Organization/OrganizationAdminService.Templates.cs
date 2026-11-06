using System.Security.Cryptography;
using System.Text;
using AuthApi.Application.Features.Employees;
using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Enums;
using AuthApi.Domain.Enums.Organization;
using Microsoft.EntityFrameworkCore;

namespace AuthApi.Application.Features.Organization;

public sealed partial class OrganizationAdminService
{
    public Task<TemplateMutationResult> PublishTemplateAsync(Guid templateId, PublishTemplateCommand command, string? expectedVersion,
        EmployeeActor actor, CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var template = await db.StructureTemplates.SingleOrDefaultAsync(x => x.Id == templateId, ct)
            ?? throw new KeyNotFoundException("Template not found.");
        EmployeeMutationSupport.CheckRowVersion(template.RowVersion, expectedVersion);
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        if (template.State != StructureTemplateState.Draft) throw new InvalidOperationException("Only a draft template can be published.");

        var units = await db.TemplateOrgUnits.AsNoTracking().Where(x => x.TemplateId == templateId).ToListAsync(ct);
        var positions = await db.TemplatePositions.AsNoTracking().Where(x => x.TemplateId == templateId).ToListAsync(ct);
        var unitIds = units.Select(x => x.Id).ToHashSet();
        var positionIds = positions.Select(x => x.Id).ToHashSet();
        if (units.Count == 0 || positions.Count == 0) throw new InvalidOperationException("A template without units or positions cannot be published.");
        if (units.Any(x => x.ParentTemplateOrgUnitId is { } p && !unitIds.Contains(p))
            || positions.Any(x => !unitIds.Contains(x.TemplateOrgUnitId) || (x.ReportsToTemplatePositionId is { } r && !positionIds.Contains(r))))
            throw new InvalidOperationException("Template internal references are inconsistent.");
        if (TemplateApplicationPlanner.ParentFirst(units).Count != units.Count)
            throw new InvalidOperationException("Template unit hierarchy contains a cycle.");
        var levelIds = positions.Where(x => x.LevelId != null).Select(x => x.LevelId!.Value).Distinct().ToList();
        var knownLevels = (await db.PositionLevels.AsNoTracking().Where(x => levelIds.Contains(x.Id)).Select(x => x.Id).ToListAsync(ct)).ToHashSet();
        ValidateTemplateContent(units, positions, knownLevels);

        template.State = StructureTemplateState.Published;
        template.PublishedAtUtc = mutation.NowUtc;
        template.PublishedByUserId = mutation.ActorUserId;
        await db.SaveChangesAsync(ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationTemplatePublished, template.Id, reason,
            new { template.ClassificationCode, template.Version, units = units.Count, positions = positions.Count, decisionReference = reference },
            mutation, ct);
        return new TemplateMutationResult(template.Id, EmployeeMutationSupport.ETag(template.RowVersion));
    }, cancellationToken);

    /// <summary>Isi template yang final: dipakai publish dan apply agar template terpublikasi selalu dapat diterapkan.</summary>
    private static void ValidateTemplateContent(IReadOnlyList<TemplateOrgUnit> units, IReadOnlyList<TemplatePosition> positions,
        IReadOnlySet<Guid> knownLevelIds)
    {
        foreach (var u in units)
        {
            if (!CodePattern().IsMatch(u.UnitCode.Trim())) throw new InvalidOperationException($"Template unit code '{u.UnitCode}' is invalid.");
            if (!Enum.IsDefined(u.UnitType)) throw new InvalidOperationException($"Template unit '{u.UnitCode}' has an unknown type.");
            if (string.IsNullOrWhiteSpace(u.Name) || u.Name.Length > 200) throw new InvalidOperationException($"Template unit '{u.UnitCode}' has an invalid name.");
        }
        if (units.Select(x => x.UnitCode.Trim()).Distinct(StringComparer.Ordinal).Count() != units.Count)
            throw new InvalidOperationException("Template unit codes must be unique.");
        foreach (var p in positions)
        {
            if (!CodePattern().IsMatch(p.PositionCode.Trim())) throw new InvalidOperationException($"Template position code '{p.PositionCode}' is invalid.");
            if (string.IsNullOrWhiteSpace(p.Name) || p.Name.Length > 200) throw new InvalidOperationException($"Template position '{p.PositionCode}' has an invalid name.");
            if (!Enum.IsDefined(p.Track)) throw new InvalidOperationException($"Template position '{p.PositionCode}' has an unknown track.");
            if (p.MaxIncumbents is <= 0 or > 100_000) throw new InvalidOperationException($"Template position '{p.PositionCode}' has an invalid capacity.");
            if (p.IsUnitHead && p.MaxIncumbents != 1) throw new InvalidOperationException("A unit head position in the template must allow exactly one incumbent.");
            if (p.LevelId is { } l && !knownLevelIds.Contains(l)) throw new InvalidOperationException($"Template position '{p.PositionCode}' references an unknown level.");
        }
        if (positions.GroupBy(x => (x.TemplateOrgUnitId, Code: x.PositionCode.Trim())).Any(g => g.Count() > 1))
            throw new InvalidOperationException("Template position codes must be unique within a unit.");
        if (units.Any(u => positions.Count(p => p.TemplateOrgUnitId == u.Id && p.IsUnitHead) > 1))
            throw new InvalidOperationException("A template unit can have at most one head position.");

        var reportsTo = positions.Where(x => x.ReportsToTemplatePositionId != null).ToDictionary(x => x.Id, x => x.ReportsToTemplatePositionId!.Value);
        foreach (var start in reportsTo.Keys)
        {
            var seen = new HashSet<Guid> { start };
            var current = start;
            while (reportsTo.TryGetValue(current, out var next))
            {
                if (!seen.Add(next)) throw new InvalidOperationException("Template reporting lines contain a cycle.");
                current = next;
            }
        }
    }

    public Task<TemplateApplyResult> ApplyTemplateAsync(Guid officeId, ApplyTemplateCommand command, EmployeeActor actor,
        CancellationToken cancellationToken = default) => runner.ExecuteAsync(async ct =>
    {
        var mutation = await support.AuthorizeAsync(actor, ct);
        var reason = EmployeeMutationSupport.Text(command.Reason, 500, nameof(command.Reason));
        var reference = EmployeeMutationSupport.OptionalText(command.DecisionReference, 200, nameof(command.DecisionReference));
        var key = EmployeeMutationSupport.Text(command.IdempotencyKey, 128, nameof(command.IdempotencyKey));
        // Bagian ber-length-prefix agar pemisah di dalam teks tidak membuat dua permintaan berbeda berhash sama.
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(string.Join("|",
            new[] { $"{officeId:N}", $"{command.TemplateId:N}", $"{command.EffectiveFrom:yyyy-MM-dd}", reason, reference ?? string.Empty }
                .Select(p => $"{p.Length}:{p}")))));

        var previous = await db.OfficeTemplateApplications.AsNoTracking()
            .SingleOrDefaultAsync(x => x.OfficeId == officeId && x.IdempotencyKey == key, ct);
        if (previous is not null)
        {
            if (!string.Equals(previous.RequestHash, hash, StringComparison.Ordinal))
                throw new InvalidOperationException("This idempotency key was already used with a different request.");
            var unitCount = await db.TemplateOrgUnits.CountAsync(x => x.TemplateId == previous.TemplateId, ct);
            var positionCount = await db.TemplatePositions.CountAsync(x => x.TemplateId == previous.TemplateId, ct);
            return new TemplateApplyResult(previous.Id, officeId, previous.TemplateId, previous.EffectiveFrom, unitCount, positionCount, true);
        }

        // Replay di atas sengaja mendahului validasi tanggal: ulangan dengan kunci sama tetap mengembalikan hasil lama.
        RequireEffectiveDate(command.EffectiveFrom, mutation);
        var plan = await new TemplateApplicationPlanner(db).BuildAsync(officeId, command.TemplateId, command.EffectiveFrom, ct);
        if (plan.Blockers.Count > 0) throw new InvalidOperationException("Template cannot be applied: " + string.Join(" ", plan.Blockers));

        // Level sudah diperiksa planner (blocker); isi template divalidasi dalam memori, tanpa query per jabatan.
        ValidateTemplateContent(plan.Units, plan.Positions, plan.LevelCodes.Keys.ToHashSet());
        var unitMap = new Dictionary<Guid, Guid>();
        var ordered = TemplateApplicationPlanner.ParentFirst(plan.Units);
        if (ordered.Count != plan.Units.Count) throw new InvalidOperationException("Template unit hierarchy contains a cycle.");
        foreach (var tu in ordered)
        {
            var code = tu.UnitCode.Trim();
            var unit = new OrgUnit { OfficeId = officeId, UnitCode = code, CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId };
            unitMap[tu.Id] = unit.Id;
            db.OrgUnits.Add(unit);
            db.OrgUnitVersions.Add(new OrgUnitVersion
            {
                OrgUnitId = unit.Id, ParentOrgUnitId = tu.ParentTemplateOrgUnitId is { } p ? unitMap[p] : null, UnitType = tu.UnitType,
                Name = tu.Name, IsActive = true, EffectiveFrom = command.EffectiveFrom, RevisionNumber = 1, ChangeReason = reason,
                DecisionReference = reference, CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId
            });
        }
        var positionMap = plan.Positions.ToDictionary(x => x.Id, _ => Guid.NewGuid());
        foreach (var tp in plan.Positions)
        {
            var code = tp.PositionCode.Trim();
            db.Positions.Add(new Position { Id = positionMap[tp.Id], OrgUnitId = unitMap[tp.TemplateOrgUnitId], PositionCode = code,
                CreatedAtUtc = mutation.NowUtc, CreatedByUserId = mutation.ActorUserId });
            db.PositionVersions.Add(new PositionVersion
            {
                PositionId = positionMap[tp.Id], LevelId = tp.LevelId,
                ReportsToPositionId = tp.ReportsToTemplatePositionId is { } r ? positionMap[r] : null, Name = tp.Name, Track = tp.Track,
                IsUnitHead = tp.IsUnitHead, MaxIncumbents = tp.MaxIncumbents, IsActive = true, EffectiveFrom = command.EffectiveFrom,
                RevisionNumber = 1, ChangeReason = reason, DecisionReference = reference, CreatedAtUtc = mutation.NowUtc,
                CreatedByUserId = mutation.ActorUserId
            });
        }
        var application = new OfficeTemplateApplication
        {
            OfficeId = officeId, TemplateId = command.TemplateId, EffectiveFrom = command.EffectiveFrom, IdempotencyKey = key,
            RequestHash = hash, AppliedAtUtc = mutation.NowUtc, AppliedByUserId = mutation.ActorUserId
        };
        db.OfficeTemplateApplications.Add(application);
        await db.SaveChangesAsync(ct);
        await ValidateOfficeAsync(officeId, mutation, ct);
        await support.AuditOrganizationAsync(AuditEventType.OrganizationTemplateApplied, officeId, reason,
            new { templateId = command.TemplateId, plan.Template.ClassificationCode, plan.Template.Version, command.EffectiveFrom,
                units = plan.Units.Count, positions = plan.Positions.Count, applicationId = application.Id, decisionReference = reference },
            mutation, ct);
        return new TemplateApplyResult(application.Id, officeId, command.TemplateId, command.EffectiveFrom, plan.Units.Count,
            plan.Positions.Count, false);
    }, cancellationToken);
}
