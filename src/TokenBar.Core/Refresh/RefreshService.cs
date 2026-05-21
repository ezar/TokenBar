using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Refresh;

public sealed class RefreshService(ProviderRegistry providerRegistry)
{
    public async Task<RefreshResult> RefreshOnceAsync(TokenBarConfig config, CancellationToken cancellationToken)
    {
        var enabledConfig = config.Providers.Where(provider => provider.Enabled).ToList();
        var providers = providerRegistry.GetEnabledProviders(enabledConfig.Select(provider => provider.ProviderId).ToList());
        var snapshots = new List<UsageSnapshot>();

        foreach (var provider in providers)
        {
            var providerConfig = enabledConfig.First(configItem =>
                string.Equals(configItem.ProviderId, provider.Descriptor.ProviderId, StringComparison.OrdinalIgnoreCase));

            try
            {
                snapshots.Add(await provider.FetchAsync(providerConfig.SourceMode, cancellationToken));
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                snapshots.Add(new UsageSnapshot(
                    provider.Descriptor.ProviderId,
                    UsageWindow.Unknown("Session"),
                    null,
                    "error",
                    UsageStatus.Error,
                    DateTimeOffset.UtcNow,
                    Message: ex.Message));
            }
        }

        return new RefreshResult(snapshots, DateTimeOffset.UtcNow);
    }
}
