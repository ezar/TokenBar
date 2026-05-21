using TokenBar.Core.Providers.OpenAI;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.BuiltIn;

internal sealed class CodexUsageProvider(
    ProviderDescriptor descriptor,
    Func<CodexOAuthCredentials?> credentialsProvider,
    ICodexOAuthApiClient apiClient,
    Func<DateTimeOffset>? now = null) : IUsageProvider
{
    public ProviderDescriptor Descriptor { get; } = descriptor;

    public async Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
    {
        var credentials = credentialsProvider();
        if (credentials is null || string.IsNullOrWhiteSpace(credentials.AccessToken))
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("5h"),
                UsageWindow.Unknown("Weekly"),
                "OAuth",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: "Sign in with Codex CLI or set CODEX_ACCESS_TOKEN to fetch Codex usage limits.");
        }

        try
        {
            return await new CodexOAuthUsageFetcher(credentials, apiClient, now).FetchAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("5h"),
                UsageWindow.Unknown("Weekly"),
                "OAuth",
                UsageStatus.Stale,
                DateTimeOffset.UtcNow,
                Message: $"Codex OAuth usage unavailable: {ex.Message}");
        }
    }
}
