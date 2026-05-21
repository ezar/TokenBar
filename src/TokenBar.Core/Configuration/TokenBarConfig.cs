using TokenBar.Core.Providers;

namespace TokenBar.Core.Configuration;

public sealed record TokenBarConfig(
    TimeSpan RefreshInterval,
    IReadOnlyList<ProviderConfig> Providers)
{
    public static TokenBarConfig CreateDefault()
    {
        return new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [
                new ProviderConfig(ProviderId.Codex, true, ProviderSourceMode.Auto),
                new ProviderConfig(ProviderId.Claude, true, ProviderSourceMode.Auto),
                new ProviderConfig(ProviderId.Copilot, true, ProviderSourceMode.Auto)
            ]);
    }
}
