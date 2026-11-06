using AuthApi.Domain.Entities.Organization;
using AuthApi.Domain.Rules.Employees;

namespace AuthApi.Domain.Rules.Organization;

/// <summary>Complete master data of ONE office, loaded by the caller (superseded rows are ignored).</summary>
public sealed record OfficeSnapshot(
    IReadOnlyList<OfficeVersion> OfficeVersions,
    IReadOnlyList<OrgUnitVersion> UnitVersions,
    IReadOnlyDictionary<Guid, Guid> PositionUnit,
    IReadOnlyList<PositionVersion> PositionVersions);

/// <summary>
/// Pure whole-office integrity checks over every effective period (including future ones).
/// Callers must load the snapshot under the same lock/transaction as the write.
/// </summary>
public static class OrganizationIntegrityRules
{
    public static void Validate(OfficeSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        var offices = snapshot.OfficeVersions.Where(x => x.SupersededAtUtc is null).ToArray();
        var units = snapshot.UnitVersions.Where(x => x.SupersededAtUtc is null).ToArray();
        var positions = snapshot.PositionVersions.Where(x => x.SupersededAtUtc is null).ToArray();

        ValidateTimeline(offices.Select(PeriodOf), "office");
        foreach (var group in units.GroupBy(x => x.OrgUnitId)) ValidateTimeline(group.Select(PeriodOf), "organization unit");
        foreach (var group in positions.GroupBy(x => x.PositionId)) ValidateTimeline(group.Select(PeriodOf), "position");

        var officeActive = offices.Where(x => x.IsActive).Select(PeriodOf).ToArray();
        var unitActive = units.Where(x => x.IsActive).GroupBy(x => x.OrgUnitId)
            .ToDictionary(x => x.Key, x => x.Select(PeriodOf).ToArray());
        var positionActive = positions.Where(x => x.IsActive).GroupBy(x => x.PositionId)
            .ToDictionary(x => x.Key, x => x.Select(PeriodOf).ToArray());

        foreach (var unit in units.Where(x => x.IsActive))
        {
            var period = PeriodOf(unit);
            if (!period.IsFullyCovered(officeActive))
                throw new InvalidOperationException("The office must be active throughout every active unit period.");
            if (unit.ParentOrgUnitId is { } parentId)
            {
                if (parentId == unit.OrgUnitId) throw new InvalidOperationException("A unit cannot be its own parent.");
                if (!unitActive.TryGetValue(parentId, out var parentCoverage) || !period.IsFullyCovered(parentCoverage))
                    throw new InvalidOperationException("The parent unit must exist in the same office and be active throughout the child unit period.");
            }
        }

        foreach (var position in positions.Where(x => x.IsActive))
        {
            var period = PeriodOf(position);
            if (!snapshot.PositionUnit.TryGetValue(position.PositionId, out var unitId))
                throw new InvalidOperationException("A position must belong to a unit of this office.");
            if (!unitActive.TryGetValue(unitId, out var unitCoverage) || !period.IsFullyCovered(unitCoverage))
                throw new InvalidOperationException("The unit must be active throughout every active position period.");
            if (position.IsUnitHead && position.MaxIncumbents != 1)
                throw new InvalidOperationException("A unit head position must allow exactly one incumbent.");
            if (position.ReportsToPositionId is { } reportsTo)
            {
                if (reportsTo == position.PositionId) throw new InvalidOperationException("A position cannot report to itself.");
                if (!positionActive.TryGetValue(reportsTo, out var reportsCoverage) || !period.IsFullyCovered(reportsCoverage))
                    throw new InvalidOperationException("The reporting position must exist in the same office and be active throughout the period.");
            }
        }

        // Structure only changes at an effective-from date, so checking those dates is exhaustive.
        var dates = units.Select(x => x.EffectiveFrom).Concat(positions.Select(x => x.EffectiveFrom)).Distinct().ToArray();
        foreach (var date in dates)
        {
            var parentOf = units.Where(x => PeriodOf(x).Contains(date) && x.ParentOrgUnitId.HasValue)
                .ToDictionary(x => x.OrgUnitId, x => x.ParentOrgUnitId!.Value);
            AssertAcyclic(parentOf, "Organization unit hierarchy contains a cycle.");
            var reportsOf = positions.Where(x => PeriodOf(x).Contains(date) && x.ReportsToPositionId.HasValue)
                .ToDictionary(x => x.PositionId, x => x.ReportsToPositionId!.Value);
            AssertAcyclic(reportsOf, "Reporting line contains a cycle.");

            var activeHeads = positions.Where(x => x.IsActive && x.IsUnitHead && PeriodOf(x).Contains(date))
                .GroupBy(x => snapshot.PositionUnit.GetValueOrDefault(x.PositionId));
            if (activeHeads.Any(g => g.Count() > 1))
                throw new InvalidOperationException("A unit can have only one active head position at a time.");
        }
    }

    private static EffectivePeriod PeriodOf(OfficeVersion v) => new(v.EffectiveFrom, v.EffectiveToExclusive);
    private static EffectivePeriod PeriodOf(OrgUnitVersion v) => new(v.EffectiveFrom, v.EffectiveToExclusive);
    private static EffectivePeriod PeriodOf(PositionVersion v) => new(v.EffectiveFrom, v.EffectiveToExclusive);

    private static void ValidateTimeline(IEnumerable<EffectivePeriod> periods, string owner)
    {
        var ordered = periods.OrderBy(x => x.From).ToArray();
        for (var i = 1; i < ordered.Length; i++)
        {
            if (ordered[i - 1].Overlaps(ordered[i]))
                throw new InvalidOperationException($"Current {owner} versions cannot overlap.");
            if (ordered[i - 1].ToExclusive != ordered[i].From)
                throw new InvalidOperationException($"Current {owner} versions cannot contain gaps.");
        }
    }

    private static void AssertAcyclic(IReadOnlyDictionary<Guid, Guid> next, string message)
    {
        foreach (var start in next.Keys)
        {
            var seen = new HashSet<Guid> { start };
            var current = start;
            while (next.TryGetValue(current, out var parent))
            {
                if (!seen.Add(parent)) throw new InvalidOperationException(message);
                current = parent;
            }
        }
    }
}
