using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums.Employees;

namespace AuthApi.Domain.Rules.Employees;

/// <summary>
/// Pure checks over a complete proposed schedule, including future periods.
/// Callers must load all affected employees/positions and run these checks under
/// the same transaction/locks as the write. These checks do not acquire locks,
/// query persistence, determine employee status, or grant any authorization.
/// </summary>
public static class PlacementScheduleRules
{
    public static void ValidateDefinitiveSchedule(
        IEnumerable<EmployeePosition> placements,
        IEnumerable<PositionCapacityPeriod> positionCapacities)
    {
        ArgumentNullException.ThrowIfNull(placements);
        ArgumentNullException.ThrowIfNull(positionCapacities);

        var confirmed = placements
            .Where(x => x.RecordState == EmployeeRecordState.Confirmed && x.SupersededAtUtc is null)
            .ToArray();

        if (confirmed.Any(x => x.EmployeeId == Guid.Empty || x.PositionId == Guid.Empty || x.AssignmentKey == Guid.Empty))
            throw new ArgumentException("A confirmed placement requires employee, position and assignment identifiers.");
        if (confirmed.GroupBy(x => x.AssignmentKey).Any(group => group.Count() > 1))
            throw new InvalidOperationException("A logical assignment cannot have multiple current confirmed revisions.");

        // Do not filter by today's date: a future conflict must be rejected now.
        var windows = confirmed.Select(x => new Window(x, new EffectivePeriod(x.EffectiveFrom, x.EffectiveToExclusive))).ToArray();
        var capacities = positionCapacities.GroupBy(x => x.PositionId).ToDictionary(x => x.Key, x => x.OrderBy(y => y.Period.From).ToArray());
        foreach (var employee in windows.GroupBy(x => x.Placement.EmployeeId))
        {
            var primary = employee.Where(x => x.Placement.IsPrimary).OrderBy(x => x.Period.From).ToArray();
            for (var index = 1; index < primary.Length; index++)
            {
                if (primary[index - 1].Period.Overlaps(primary[index].Period))
                    throw new InvalidOperationException("Primary placement periods cannot overlap for one employee.");
            }

            foreach (var additional in employee.Where(x => !x.Placement.IsPrimary))
            {
                if (!HasContinuousCoverage(additional.Period, primary.Select(x => x.Period)))
                    throw new InvalidOperationException("An additional definitive placement needs a primary placement throughout its period.");
            }
        }

        foreach (var position in windows.GroupBy(x => x.Placement.PositionId))
        {
            if (!capacities.TryGetValue(position.Key, out var capacityPeriods))
                throw new ArgumentException("Capacity must be supplied explicitly for every affected position.", nameof(positionCapacities));
            for (var index = 0; index < capacityPeriods.Length; index++)
            {
                if (capacityPeriods[index].MaxIncumbents is <= 0)
                    throw new ArgumentException("A configured position capacity must be positive.", nameof(positionCapacities));
                if (index > 0 && capacityPeriods[index - 1].Period.Overlaps(capacityPeriods[index].Period))
                    throw new ArgumentException("Position capacity versions cannot overlap.", nameof(positionCapacities));
            }
            foreach (var window in position)
            {
                if (!HasContinuousCoverage(window.Period, capacityPeriods.Where(x => x.IsActive).Select(x => x.Period)))
                    throw new InvalidOperationException("The position must remain active and configured throughout every placement period.");
            }
            foreach (var capacity in capacityPeriods.Where(x => x.IsActive && x.MaxIncumbents.HasValue))
            {
                var events = new List<(DateOnly Date, int Delta)>();
                foreach (var window in position)
                {
                    if (window.Period.Intersect(capacity.Period) is not { } overlap) continue;
                    events.Add((overlap.From, 1));
                    if (overlap.ToExclusive is { } end) events.Add((end, -1));
                }

                var occupancy = 0;
                foreach (var day in events.GroupBy(x => x.Date).OrderBy(x => x.Key))
                {
                    // Evaluate starts/ends together so adjoining periods do not overlap.
                    occupancy += day.Sum(x => x.Delta);
                    if (occupancy > capacity.MaxIncumbents!.Value)
                        throw new InvalidOperationException("Definitive placement capacity is exceeded during the proposed schedule.");
                }
            }
        }
    }

    public static void ValidateActingSource(EmployeePosition source, EmployeeActingAssignment acting)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(acting);

        if (source.Id == Guid.Empty || source.EmployeeId == Guid.Empty || acting.TargetPositionId == Guid.Empty ||
            source.Id != acting.SourceEmployeePositionId || source.EmployeeId != acting.EmployeeId)
            throw new ArgumentException("The acting assignment must reference the exact definitive revision of the same employee.");
        if (source.RecordState != EmployeeRecordState.Confirmed || source.SupersededAtUtc is not null)
            throw new InvalidOperationException("The source definitive revision must be current and confirmed.");
        if (acting.RecordState == EmployeeRecordState.Cancelled || acting.SupersededAtUtc is not null)
            throw new InvalidOperationException("A cancelled or superseded acting revision cannot be confirmed.");
        if (!Enum.IsDefined(acting.AssignmentType) || string.IsNullOrWhiteSpace(acting.DecisionReference))
            throw new ArgumentException("An acting assignment needs a valid type and decision reference.");

        var sourcePeriod = new EffectivePeriod(source.EffectiveFrom, source.EffectiveToExclusive);
        var actingPeriod = new EffectivePeriod(acting.EffectiveFrom, acting.EffectiveToExclusive);
        if (!sourcePeriod.Covers(actingPeriod))
            throw new InvalidOperationException("The source definitive placement must cover the entire acting period.");
    }

    private static bool HasContinuousCoverage(EffectivePeriod target, IEnumerable<EffectivePeriod> coverage)
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

    private sealed record Window(EmployeePosition Placement, EffectivePeriod Period);
}

/// <summary>A current position-version interval, not a snapshot of today's capacity.</summary>
public sealed record PositionCapacityPeriod(Guid PositionId, EffectivePeriod Period, int? MaxIncumbents, bool IsActive = true);
