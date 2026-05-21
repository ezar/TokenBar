using TokenBar.Core.Providers.Anthropic;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.BuiltIn;

internal sealed class ClaudeUsageProvider(
    ProviderDescriptor descriptor,
    Func<string?> tokenProvider,
    IClaudeOAuthApiClient apiClient) : IUsageProvider
{
    public ProviderDescriptor Descriptor { get; } = descriptor;

    public async Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
    {
        var token = tokenProvider();
        if (string.IsNullOrWhiteSpace(token))
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Current session"),
                UsageWindow.Unknown("Weekly limits"),
                "OAuth",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: "Sign in with Claude Code or set CLAUDE_CODE_OAUTH_TOKEN to fetch Claude plan usage limits.");
        }

        try
        {
            return await new ClaudeOAuthUsageFetcher(token, apiClient).FetchAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Current session"),
                UsageWindow.Unknown("Weekly limits"),
                "OAuth",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: $"Claude OAuth usage unavailable: {ex.Message}");
        }
    }
}
