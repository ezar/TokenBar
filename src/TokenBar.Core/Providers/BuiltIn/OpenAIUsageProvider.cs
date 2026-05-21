using TokenBar.Core.Providers.OpenAI;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.BuiltIn;

internal sealed class OpenAIUsageProvider(
    ProviderDescriptor descriptor,
    Func<string?> tokenProvider,
    IOpenAIApiClient apiClient,
    Func<DateTimeOffset>? now = null) : IUsageProvider
{
    public ProviderDescriptor Descriptor { get; } = descriptor;

    public async Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
    {
        var token = tokenProvider();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Today"),
                UsageWindow.Unknown("7 days"),
                "Api",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: "Set OPENAI_ADMIN_KEY or OPENAI_API_KEY to fetch OpenAI usage.");
        }

        try
        {
            return await new OpenAIUsageFetcher(token, apiClient, now).FetchAsync(cancellationToken);
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
                Message: $"OpenAI Usage API unavailable: {ex.Message}");
        }
    }
}
