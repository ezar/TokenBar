using TokenBar.Core.Configuration;
using TokenBar.Core.Providers.Anthropic;
using TokenBar.Core.Providers.Copilot;
using TokenBar.Core.Providers.OpenAI;

namespace TokenBar.Core.Providers.BuiltIn;

public static class BuiltInProviderFactory
{
    private static readonly TimeSpan ApiTimeout = TimeSpan.FromSeconds(15);
    private static readonly Lazy<AppSettingsApiKeys> AppSettingsKeys = new(AppSettingsApiKeyStore.LoadDefault);

    public static IReadOnlyList<IUsageProvider> CreateProviders(
        Func<string?>? openAITokenProvider = null,
        Func<string?>? anthropicAdminKeyProvider = null,
        Func<string?>? copilotTokenProvider = null,
        IOpenAIApiClient? openAIApiClient = null,
        IAnthropicAdminApiClient? anthropicAdminApiClient = null,
        ICopilotApiClient? copilotApiClient = null,
        Func<DateTimeOffset>? now = null)
    {
        openAITokenProvider ??= GetOpenAIToken;
        anthropicAdminKeyProvider ??= GetAnthropicAdminKey;
        copilotTokenProvider ??= GetCopilotToken;
        openAIApiClient ??= new OpenAIApiClient(CreateHttpClient());
        anthropicAdminApiClient ??= new AnthropicAdminApiClient(CreateHttpClient());
        copilotApiClient ??= new CopilotApiClient(CreateHttpClient());

        return
        [
            new OpenAIUsageProvider(
                new ProviderDescriptor(
                    ProviderId.Codex,
                    "Codex",
                    "#111111",
                    DefaultEnabled: true,
                    [ProviderSourceMode.Auto, ProviderSourceMode.Api]),
                openAITokenProvider,
                openAIApiClient,
                now),
            new AnthropicUsageProvider(
                new ProviderDescriptor(
                    ProviderId.Claude,
                    "Claude",
                    "#DA7756",
                    DefaultEnabled: true,
                    [ProviderSourceMode.Auto, ProviderSourceMode.Api]),
                anthropicAdminKeyProvider,
                anthropicAdminApiClient,
                now),
            new CopilotUsageProvider(
                new ProviderDescriptor(
                    ProviderId.Copilot,
                    "GitHub Copilot",
                    "#24292F",
                    DefaultEnabled: true,
                    [ProviderSourceMode.Auto, ProviderSourceMode.Api]),
                copilotTokenProvider,
                copilotApiClient)
        ];
    }

    private static string? GetOpenAIToken()
    {
        return Environment.GetEnvironmentVariable("OPENAI_ADMIN_KEY")
            ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")
            ?? AppSettingsKeys.Value.OpenAIAdminKey;
    }

    private static string? GetAnthropicAdminKey()
    {
        return Environment.GetEnvironmentVariable("ANTHROPIC_ADMIN_KEY")
            ?? AppSettingsKeys.Value.AnthropicAdminKey;
    }

    private static string? GetCopilotToken()
    {
        return Environment.GetEnvironmentVariable("GITHUB_COPILOT_TOKEN")
            ?? Environment.GetEnvironmentVariable("GITHUB_TOKEN")
            ?? AppSettingsKeys.Value.GitHubCopilotToken;
    }

    private static HttpClient CreateHttpClient()
    {
        return new HttpClient
        {
            Timeout = ApiTimeout
        };
    }
}
