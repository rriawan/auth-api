namespace AuthApi.Domain.Rules.Employees;

/// <summary>A business-date interval with an inclusive start and exclusive end.</summary>
public readonly record struct EffectivePeriod
{
    public EffectivePeriod(DateOnly from, DateOnly? toExclusive)
    {
        if (toExclusive.HasValue && toExclusive.Value <= from)
            throw new ArgumentException("The exclusive end must be later than the start.", nameof(toExclusive));

        From = from;
        ToExclusive = toExclusive;
    }

    public DateOnly From { get; }
    public DateOnly? ToExclusive { get; }

    public bool Contains(DateOnly date) => date >= From && (!ToExclusive.HasValue || date < ToExclusive.Value);

    public bool Overlaps(EffectivePeriod other) =>
        (!ToExclusive.HasValue || other.From < ToExclusive.Value) &&
        (!other.ToExclusive.HasValue || From < other.ToExclusive.Value);

    public bool Covers(EffectivePeriod other) =>
        From <= other.From && (!ToExclusive.HasValue ||
            (other.ToExclusive.HasValue && other.ToExclusive.Value <= ToExclusive.Value));

    public EffectivePeriod? Intersect(EffectivePeriod other)
    {
        if (!Overlaps(other)) return null;
        var from = From >= other.From ? From : other.From;
        var end = !ToExclusive.HasValue ? other.ToExclusive
            : !other.ToExclusive.HasValue ? ToExclusive
            : ToExclusive.Value <= other.ToExclusive.Value ? ToExclusive : other.ToExclusive;
        return new EffectivePeriod(from, end);
    }
}
