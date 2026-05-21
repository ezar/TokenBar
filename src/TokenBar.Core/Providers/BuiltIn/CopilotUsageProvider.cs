using TokenBar.Core.Providers.Copilot;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.BuiltIn;

internal sealed class CopilotUsageProvider(
    ProviderDescriptor descriptor,
    Func<string?> tokenProvider,
    ICopilotApiClient apiClient) : IUsageProvider
{
    public ProviderDescriptor Descriptor { get; } = descriptor;

    public async Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
    {
        var token = tokenProvider();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Premium"),
                UsageWindow.Unknown("Chat"),
                "Api",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: "Set GITHUB_COPILOT_TOKEN or GITHUB_TOKEN to fetch Copilot usage.");
        }

        try
        {
            return await new CopilotUsageFetcher(token, apiClient).FetchAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Premium"),
                UsageWindow.Unknown("Chat"),
                "Api",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: $"Copilot API unavailable: {ex.Message}");
        }
    }
}
