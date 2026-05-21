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
        Func<CodexOAuthCredentials?>? codexOAuthCredentialsProvider = null,
        Func<string?>? claudeOAuthTokenProvider = null,
        Func<string?>? openAITokenProvider = null,
        Func<string?>? anthropicAdminKeyProvider = null,
        Func<string?>? copilotTokenProvider = null,
        ICodexOAuthApiClient? codexOAuthApiClient = null,
        IClaudeOAuthApiClient? claudeOAuthApiClient = null,
        IOpenAIApiClient? openAIApiClient = null,
        IAnthropicAdminApiClient? anthropicAdminApiClient = null,
        ICopilotApiClient? copilotApiClient = null,
        Func<DateTimeOffset>? now = null)
    {
        codexOAuthCredentialsProvider ??= GetCodexOAuthCredentials;
        claudeOAuthTokenProvider ??= GetClaudeOAuthToken;
        openAITokenProvider ??= GetOpenAIToken;
        anthropicAdminKeyProvider ??= GetAnthropicAdminKey;
        copilotTokenProvider ??= GetCopilotToken;
        codexOAuthApiClient ??= new CodexOAuthApiClient(CreateHttpClient());
        claudeOAuthApiClient ??= new ClaudeOAuthApiClient(CreateHttpClient());
        openAIApiClient ??= new OpenAIApiClient(CreateHttpClient());
        anthropicAdminApiClient ??= new AnthropicAdminApiClient(CreateHttpClient());
        copilotApiClient ??= new CopilotApiClient(CreateHttpClient());

        return
        [
            new CodexUsageProvider(
                new ProviderDescriptor(
                    ProviderId.Codex,
                    "Codex",
                    "#111111",
                    DefaultEnabled: true,
                    [ProviderSourceMode.Auto, ProviderSourceMode.OAuth, ProviderSourceMode.Cli]),
                codexOAuthCredentialsProvider,
                codexOAuthApiClient,
                now),
            new ClaudeUsageProvider(
                new ProviderDescriptor(
                    ProviderId.Claude,
                    "Claude",
                    "#DA7756",
                    DefaultEnabled: true,
                    [ProviderSourceMode.Auto, ProviderSourceMode.OAuth, ProviderSourceMode.Cli]),
                claudeOAuthTokenProvider,
                claudeOAuthApiClient),
            new CopilotUsageProvider(
                new ProviderDescriptor(
                    ProviderId.Copilot,
                    "GitHub Copilot",
                    "#24292F",
                    DefaultEnabled: true,
                    [ProviderSourceMode.Auto, ProviderSourceMode.Api]),
                copilotTokenProvider,
                copilotApiClient),
            new OpenAIUsageProvider(
                new ProviderDescriptor(
                    ProviderId.OpenAIApi,
                    "OpenAI API",
                    "#111111",
                    DefaultEnabled: false,
                    [ProviderSourceMode.Auto, ProviderSourceMode.Api]),
                openAITokenProvider,
                openAIApiClient,
                now),
            new AnthropicUsageProvider(
                new ProviderDescriptor(
                    ProviderId.AnthropicApi,
                    "Anthropic API",
                    "#DA7756",
                    DefaultEnabled: false,
                    [ProviderSourceMode.Auto, ProviderSourceMode.Api]),
                anthropicAdminKeyProvider,
                anthropicAdminApiClient,
                now)
        ];
    }

    private static CodexOAuthCredentials? GetCodexOAuthCredentials()
    {
        var accessToken = Environment.GetEnvironmentVariable("CODEX_ACCESS_TOKEN")
            ?? AppSettingsKeys.Value.CodexAccessToken;
        var accountId = Environment.GetEnvironmentVariable("CODEX_ACCOUNT_ID")
            ?? AppSettingsKeys.Value.CodexAccountId;

        return string.IsNullOrWhiteSpace(accessToken)
            ? CodexOAuthCredentialStore.TryReadDefaultCredentials()
            : new CodexOAuthCredentials(accessToken, accountId);
    }

    private static string? GetClaudeOAuthToken()
    {
        return Environment.GetEnvironmentVariable("CLAUDE_CODE_OAUTH_TOKEN")
            ?? AppSettingsKeys.Value.ClaudeCodeOAuthToken
            ?? ClaudeOAuthCredentialStore.TryReadDefaultAccessToken();
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
