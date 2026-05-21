using TokenBar.Core.Providers;

namespace TokenBar.Core.Configuration;

public sealed record ProviderConfig(
    string ProviderId,
    bool Enabled,
    ProviderSourceMode SourceMode);
