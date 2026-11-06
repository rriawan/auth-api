using AuthApi.Domain.Entities.Employees;
using AuthApi.Domain.Enums.Employees;
using AuthApi.Domain.Rules.Employees;
using Xunit;

namespace AuthApi.Tests;

public sealed class EmployeePlacementRulesTests
{
    private static readonly DateOnly Start = new(2027, 1, 1);

    [Fact]
    public void EffectivePeriods_AreHalfOpenAndValidateTheirBoundaries()
    {
        var first = new EffectivePeriod(Start, Start.AddDays(10));
        var second = new EffectivePeriod(Start.AddDays(10), null);
        Assert.True(first.Contains(Start));
        Assert.False(first.Contains(Start.AddDays(10)));
        Assert.False(first.Overlaps(second));
        Assert.True(second.Contains(Start.AddDays(10)));
        Assert.True(new EffectivePeriod(Start, null).Covers(second));
        Assert.False(first.Covers(new EffectivePeriod(Start, null)));
        Assert.Throws<ArgumentException>(() => new EffectivePeriod(Start, Start));
    }

    [Fact]
    public void BackToBackPrimaryOccupants_DoNotExceedSinglePositionCapacity()
    {
        var position = Guid.NewGuid();
        var first = Placement(Guid.NewGuid(), position, Start, Start.AddDays(10));
        var second = Placement(Guid.NewGuid(), position, Start.AddDays(10), null);
        PlacementScheduleRules.ValidateDefinitiveSchedule([first, second], Capacities((position, 1)));
    }

    [Fact]
    public void MultipleStaffOccupants_AreAllowedWithExplicitUnboundedCapacity()
    {
        var position = Guid.NewGuid();
        var placements = Enumerable.Range(0, 3).Select(_ => Placement(Guid.NewGuid(), position, Start, null));
        PlacementScheduleRules.ValidateDefinitiveSchedule(placements, Capacities((position, null)));
    }

    [Fact]
    public void FutureCapacityConflict_IsRejectedWithoutLookingAtToday()
    {
        var position = Guid.NewGuid();
        var first = Placement(Guid.NewGuid(), position, Start, Start.AddDays(20));
        var second = Placement(Guid.NewGuid(), position, Start.AddDays(15), null);
        Assert.Throws<InvalidOperationException>(() =>
            PlacementScheduleRules.ValidateDefinitiveSchedule([first, second], Capacities((position, 1))));
    }

    [Fact]
    public void OneEmployee_CannotHaveOverlappingPrimaryPlacements()
    {
        var employee = Guid.NewGuid();
        var first = Placement(employee, Guid.NewGuid(), Start, null);
        var second = Placement(employee, Guid.NewGuid(), Start.AddDays(30), null);
        Assert.Throws<InvalidOperationException>(() =>
            PlacementScheduleRules.ValidateDefinitiveSchedule([first, second], Capacities((first.PositionId, null), (second.PositionId, null))));
    }

    [Fact]
    public void AdditionalPlacement_NeedsContinuousPrimaryCoverageIncludingAcrossTransfers()
    {
        var employee = Guid.NewGuid();
        var first = Placement(employee, Guid.NewGuid(), Start, Start.AddDays(10));
        var second = Placement(employee, Guid.NewGuid(), Start.AddDays(10), null);
        var additional = Placement(employee, Guid.NewGuid(), Start, null);
        additional.IsPrimary = false;
        var capacities = Capacities((first.PositionId, null), (second.PositionId, null), (additional.PositionId, null));
        PlacementScheduleRules.ValidateDefinitiveSchedule([first, second, additional], capacities);
        second.EffectiveFrom = Start.AddDays(11);
        Assert.Throws<InvalidOperationException>(() => PlacementScheduleRules.ValidateDefinitiveSchedule([first, second, additional], capacities));
    }

    [Fact]
    public void DraftCancelledAndSupersededRevisions_DoNotOccupyPositions()
    {
        var position = Guid.NewGuid();
        var confirmed = Placement(Guid.NewGuid(), position, Start, null);
        var draft = Placement(Guid.NewGuid(), position, Start, null);
        draft.RecordState = EmployeeRecordState.Draft;
        var cancelled = Placement(Guid.NewGuid(), position, Start, null);
        cancelled.RecordState = EmployeeRecordState.Cancelled;
        var superseded = Placement(Guid.NewGuid(), position, Start, null);
        superseded.SupersededAtUtc = DateTime.UtcNow;
        PlacementScheduleRules.ValidateDefinitiveSchedule([confirmed, draft, cancelled, superseded], Capacities((position, 1)));
    }

    [Fact]
    public void MissingCapacityCannotSilentlyBecomeUnbounded()
    {
        var placement = Placement(Guid.NewGuid(), Guid.NewGuid(), Start, null);
        Assert.Throws<ArgumentException>(() =>
            PlacementScheduleRules.ValidateDefinitiveSchedule([placement], []));
    }

    [Fact]
    public void CapacityChanges_AreEvaluatedAtTheirOwnEffectiveDates()
    {
        var position = Guid.NewGuid();
        var first = Placement(Guid.NewGuid(), position, Start, null);
        var second = Placement(Guid.NewGuid(), position, Start.AddDays(10), null);
        var capacity = new[]
        {
            new PositionCapacityPeriod(position, new EffectivePeriod(Start, Start.AddDays(10)), 1),
            new PositionCapacityPeriod(position, new EffectivePeriod(Start.AddDays(10), null), 2)
        };
        PlacementScheduleRules.ValidateDefinitiveSchedule([first, second], capacity);
        capacity[1] = capacity[1] with { MaxIncumbents = 1 };
        Assert.Throws<InvalidOperationException>(() => PlacementScheduleRules.ValidateDefinitiveSchedule([first, second], capacity));
    }

    [Fact]
    public void APlacementCannotCrossAFutureInactivePositionPeriod()
    {
        var placement = Placement(Guid.NewGuid(), Guid.NewGuid(), Start, null);
        var capacities = new[]
        {
            new PositionCapacityPeriod(placement.PositionId, new EffectivePeriod(Start, Start.AddDays(10)), null),
            new PositionCapacityPeriod(placement.PositionId, new EffectivePeriod(Start.AddDays(10), null), null, IsActive: false)
        };
        Assert.Throws<InvalidOperationException>(() => PlacementScheduleRules.ValidateDefinitiveSchedule([placement], capacities));
    }

    [Fact]
    public void ActingRequiresOwnExactCurrentSourceAndDecisionReference()
    {
        var source = Placement(Guid.NewGuid(), Guid.NewGuid(), Start, null);
        var acting = Acting(source, Start.AddDays(1), Start.AddDays(5));
        PlacementScheduleRules.ValidateActingSource(source, acting);
        acting.EmployeeId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => PlacementScheduleRules.ValidateActingSource(source, acting));
        acting.EmployeeId = source.EmployeeId;
        acting.SourceEmployeePositionId = Guid.NewGuid();
        Assert.Throws<ArgumentException>(() => PlacementScheduleRules.ValidateActingSource(source, acting));
        acting.SourceEmployeePositionId = source.Id;
        acting.DecisionReference = " ";
        Assert.Throws<ArgumentException>(() => PlacementScheduleRules.ValidateActingSource(source, acting));
        acting.DecisionReference = "SK-TEST-1";
        source.SupersededAtUtc = DateTime.UtcNow;
        Assert.Throws<InvalidOperationException>(() => PlacementScheduleRules.ValidateActingSource(source, acting));
    }

    [Fact]
    public void ActingCannotExtendPastTheSourceDefinitivePeriod()
    {
        var source = Placement(Guid.NewGuid(), Guid.NewGuid(), Start, Start.AddDays(10));
        Assert.Throws<InvalidOperationException>(() =>
            PlacementScheduleRules.ValidateActingSource(source, Acting(source, Start, Start.AddDays(11))));
        Assert.Throws<InvalidOperationException>(() =>
            PlacementScheduleRules.ValidateActingSource(source, Acting(source, Start, null)));
    }

    private static EmployeePosition Placement(Guid employee, Guid position, DateOnly from, DateOnly? to) => new()
    {
        Id = Guid.NewGuid(), AssignmentKey = Guid.NewGuid(), EmployeeId = employee, PositionId = position,
        IsPrimary = true, EffectiveFrom = from, EffectiveToExclusive = to,
        RecordState = EmployeeRecordState.Confirmed, ChangeReason = "Test schedule"
    };

    private static EmployeeActingAssignment Acting(EmployeePosition source, DateOnly from, DateOnly? to) => new()
    {
        EmployeeId = source.EmployeeId, SourceEmployeePositionId = source.Id, TargetPositionId = Guid.NewGuid(),
        AssignmentType = ActingAssignmentType.Plh, EffectiveFrom = from, EffectiveToExclusive = to,
        RecordState = EmployeeRecordState.Draft, DecisionReference = "SK-TEST-1", ChangeReason = "Test acting"
    };

    private static IReadOnlyList<PositionCapacityPeriod> Capacities(params (Guid Position, int? Capacity)[] values) =>
        values.Select(x => new PositionCapacityPeriod(x.Position, new EffectivePeriod(DateOnly.MinValue, null), x.Capacity)).ToArray();
}
