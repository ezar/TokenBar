namespace TokenBar.Core.Usage;

public sealed record UsageWindow(
    string Label,
    decimal? Used,
    decimal? Limit,
    decimal? PercentUsed,
    decimal? PercentRemaining,
    DateTimeOffset? ResetAt)
{
    public static UsageWindow FromUsedAndLimit(
        string label,
        decimal used,
        decimal limit,
        DateTimeOffset? resetAt)
    {
        if (limit <= 0)
        {
            return new UsageWindow(label, used, limit, null, null, resetAt);
        }

        var percentUsed = Math.Round((used / limit) * 100m, 2, MidpointRounding.AwayFromZero);
        var percentRemaining = Math.Max(0m, Math.Round(100m - percentUsed, 2, MidpointRounding.AwayFromZero));

        return new UsageWindow(label, used, limit, percentUsed, percentRemaining, resetAt);
    }

    public static UsageWindow Unknown(string label)
    {
        return new UsageWindow(label, null, null, null, null, null);
    }
}
