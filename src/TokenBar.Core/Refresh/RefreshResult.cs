using TokenBar.Core.Usage;

namespace TokenBar.Core.Refresh;

public sealed record RefreshResult(
    IReadOnlyList<UsageSnapshot> Snapshots,
    DateTimeOffset RefreshedAt);
