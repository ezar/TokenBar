namespace TokenBar.Core.Usage;

public sealed record UsageSnapshot(
    string ProviderId,
    UsageWindow PrimaryWindow,
    UsageWindow? SecondaryWindow,
    string Source,
    UsageStatus Status,
    DateTimeOffset UpdatedAt,
    string? Identity = null,
    string? Message = null);
