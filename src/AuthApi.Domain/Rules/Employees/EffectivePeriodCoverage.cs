namespace AuthApi.Domain.Rules.Employees;

public static class EffectivePeriodCoverage
{
    /// <summary>True when the union of <paramref name="coverage"/> contiguously covers every day of <paramref name="target"/>.</summary>
    public static bool IsFullyCovered(this EffectivePeriod target, IEnumerable<EffectivePeriod> coverage)
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
}
