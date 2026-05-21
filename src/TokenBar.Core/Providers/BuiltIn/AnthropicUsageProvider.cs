using TokenBar.Core.Providers.Anthropic;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.BuiltIn;

internal sealed class AnthropicUsageProvider(
    ProviderDescriptor descriptor,
    Func<string?> adminKeyProvider,
    IAnthropicAdminApiClient apiClient,
    Func<DateTimeOffset>? now = null) : IUsageProvider
{
    public ProviderDescriptor Descriptor { get; } = descriptor;

    public async Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
    {
        var adminKey = adminKeyProvider();
        if (string.IsNullOrWhiteSpace(adminKey))
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Today"),
                UsageWindow.Unknown("7 days"),
                "Api",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: "Set ANTHROPIC_ADMIN_KEY to fetch Anthropic usage.");
        }

        try
        {
            return await new AnthropicAdminUsageFetcher(adminKey, apiClient, now).FetchAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Today"),
                UsageWindow.Unknown("7 days"),
                "Api",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: $"Anthropic Admin API unavailable: {ex.Message}");
        }
    }
}
