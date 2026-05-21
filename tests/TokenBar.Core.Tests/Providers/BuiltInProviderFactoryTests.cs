using FluentAssertions;
using TokenBar.Core.Providers;
using TokenBar.Core.Providers.Anthropic;
using TokenBar.Core.Providers.BuiltIn;
using TokenBar.Core.Providers.Copilot;
using TokenBar.Core.Providers.OpenAI;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers;

public sealed class BuiltInProviderFactoryTests
{
    [Fact]
    public void CreateProvidersReturnsBuiltInProviders()
    {
        var providers = BuiltInProviderFactory.CreateProviders();

        providers.Select(provider => provider.Descriptor.ProviderId)
            .Should().Equal(
                ProviderId.Codex,
                ProviderId.Claude,
                ProviderId.Copilot,
                ProviderId.OpenAIApi,
                ProviderId.AnthropicApi);

        providers.SelectMany(provider => provider.Descriptor.SupportedSourceModes)
            .Should().Contain(ProviderSourceMode.Api);
    }

    [Fact]
    public async Task OpenAiApiAndAnthropicApiProvidersFetchUsageFromAdminApis()
    {
        const string openAiJson = """
        {
          "data": [
            {
              "start_time": 1779235200,
              "results": [
                { "input_tokens": 10, "output_tokens": 5, "input_cached_tokens": 1, "num_model_requests": 2 }
              ]
            }
          ]
        }
        """;
        const string anthropicJson = """
        {
          "data": [
            {
              "starting_at": "2026-05-20T00:00:00Z",
              "results": [
                {
                  "uncached_input_tokens": 40,
                  "cache_creation": { "ephemeral_1h_input_tokens": 5, "ephemeral_5m_input_tokens": 3 },
                  "cache_read_input_tokens": 2,
                  "output_tokens": 10
                }
              ]
            }
          ]
        }
        """;
        var openAiClient = new StubOpenAIApiClient(openAiJson);
        var anthropicClient = new StubAnthropicAdminApiClient(anthropicJson);
        var providers = BuiltInProviderFactory.CreateProviders(
            codexOAuthCredentialsProvider: () => null,
            claudeOAuthTokenProvider: () => null,
            openAITokenProvider: () => "openai-admin",
            anthropicAdminKeyProvider: () => "anthropic-admin",
            copilotTokenProvider: () => null,
            codexOAuthApiClient: new StubCodexOAuthApiClient("{}"),
            claudeOAuthApiClient: new StubClaudeOAuthApiClient("{}"),
            openAIApiClient: openAiClient,
            anthropicAdminApiClient: anthropicClient,
            copilotApiClient: new StubCopilotApiClient("{}"),
            now: () => new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var openAiApi = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.OpenAIApi)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);
        var anthropicApi = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.AnthropicApi)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);

        openAiApi.Status.Should().Be(UsageStatus.Available);
        openAiApi.Source.Should().Be("Api");
        openAiApi.PrimaryWindow.Used.Should().Be(16);
        anthropicApi.Status.Should().Be(UsageStatus.Available);
        anthropicApi.Source.Should().Be("Api");
        anthropicApi.PrimaryWindow.Used.Should().Be(60);
        openAiClient.Calls.Should().Be(1);
        anthropicClient.Calls.Should().Be(1);
    }

    [Fact]
    public async Task CodexAndClaudeProvidersReturnStaleSnapshotsWhenOAuthCredentialsAreMissing()
    {
        var providers = BuiltInProviderFactory.CreateProviders(
            codexOAuthCredentialsProvider: () => null,
            claudeOAuthTokenProvider: () => null,
            openAITokenProvider: () => null,
            anthropicAdminKeyProvider: () => null,
            copilotTokenProvider: () => null,
            codexOAuthApiClient: new StubCodexOAuthApiClient("{}"),
            claudeOAuthApiClient: new StubClaudeOAuthApiClient("{}"),
            openAIApiClient: new StubOpenAIApiClient("{}"),
            anthropicAdminApiClient: new StubAnthropicAdminApiClient("{}"),
            copilotApiClient: new StubCopilotApiClient("{}"));

        var codex = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Codex)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);
        var claude = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Claude)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);

        codex.Status.Should().Be(UsageStatus.Stale);
        codex.Source.Should().Be("OAuth");
        codex.Message.Should().Contain("CODEX_ACCESS_TOKEN");
        claude.Status.Should().Be(UsageStatus.Stale);
        claude.Source.Should().Be("OAuth");
        claude.Message.Should().Contain("CLAUDE_CODE_OAUTH_TOKEN");
    }

    [Fact]
    public async Task CodexProviderUsesOAuthPlanUsage()
    {
        const string codexUsageJson = """
        {
          "plan_type": "Pro",
          "rate_limit": {
            "primary_window": { "used_percent": 89, "reset_at": 1779308700 },
            "secondary_window": { "used_percent": 9, "reset_after_seconds": 432000 }
          }
        }
        """;
        var codexOAuthClient = new StubCodexOAuthApiClient(codexUsageJson);
        var providers = BuiltInProviderFactory.CreateProviders(
            codexOAuthCredentialsProvider: () => new CodexOAuthCredentials("codex-token", "account-123"),
            claudeOAuthTokenProvider: () => null,
            openAITokenProvider: () => null,
            anthropicAdminKeyProvider: () => null,
            copilotTokenProvider: () => null,
            codexOAuthApiClient: codexOAuthClient,
            claudeOAuthApiClient: new StubClaudeOAuthApiClient("{}"),
            openAIApiClient: new StubOpenAIApiClient("{}"),
            anthropicAdminApiClient: new StubAnthropicAdminApiClient("{}"),
            copilotApiClient: new StubCopilotApiClient("{}"),
            now: () => new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var codex = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Codex)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);

        codex.Source.Should().Be("OAuth");
        codex.PrimaryWindow.Label.Should().Be("5h");
        codex.PrimaryWindow.PercentUsed.Should().Be(89);
        codex.SecondaryWindow!.Label.Should().Be("Weekly");
        codex.SecondaryWindow.PercentUsed.Should().Be(9);
        codexOAuthClient.Calls.Should().Be(1);
    }

    [Fact]
    public async Task ClaudeProviderPrefersOAuthPlanUsageOverAnthropicAdminUsage()
    {
        const string claudeUsageJson = """
        {
          "five_hour": { "utilization": 53, "resets_at": "2026-05-21T14:30:00Z" },
          "seven_day": { "utilization": 100, "resets_at": "2026-05-23T11:00:00Z" },
          "subscriptionType": "pro"
        }
        """;
        var claudeOAuthClient = new StubClaudeOAuthApiClient(claudeUsageJson);
        var anthropicAdminClient = new StubAnthropicAdminApiClient("{}");
        var providers = BuiltInProviderFactory.CreateProviders(
            codexOAuthCredentialsProvider: () => null,
            claudeOAuthTokenProvider: () => "sk-ant-oat01",
            openAITokenProvider: () => null,
            anthropicAdminKeyProvider: () => "anthropic-admin",
            copilotTokenProvider: () => null,
            codexOAuthApiClient: new StubCodexOAuthApiClient("{}"),
            claudeOAuthApiClient: claudeOAuthClient,
            openAIApiClient: new StubOpenAIApiClient("{}"),
            anthropicAdminApiClient: anthropicAdminClient,
            copilotApiClient: new StubCopilotApiClient("{}"));

        var claude = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Claude)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);

        claude.Source.Should().Be("OAuth");
        claude.PrimaryWindow.Label.Should().Be("Current session");
        claude.PrimaryWindow.PercentUsed.Should().Be(53);
        claude.SecondaryWindow!.Label.Should().Be("Weekly limits");
        claude.SecondaryWindow.PercentUsed.Should().Be(100);
        claudeOAuthClient.Calls.Should().Be(1);
        anthropicAdminClient.Calls.Should().Be(0);
    }

    private sealed class StubOpenAIApiClient(string usageJson) : IOpenAIApiClient
    {
        public int Calls { get; private set; }

        public Task<string> GetCompletionsUsageJsonAsync(
            string token,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            token.Should().Be("openai-admin");
            Calls++;
            return Task.FromResult(usageJson);
        }
    }

    private sealed class StubCodexOAuthApiClient(string usageJson) : ICodexOAuthApiClient
    {
        public int Calls { get; private set; }

        public Task<string> GetUsageJsonAsync(
            string accessToken,
            string? accountId,
            CancellationToken cancellationToken)
        {
            accessToken.Should().Be("codex-token");
            accountId.Should().Be("account-123");
            Calls++;
            return Task.FromResult(usageJson);
        }
    }

    private sealed class StubAnthropicAdminApiClient(string usageJson) : IAnthropicAdminApiClient
    {
        public int Calls { get; private set; }

        public Task<string> GetMessagesUsageReportJsonAsync(
            string adminKey,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            adminKey.Should().Be("anthropic-admin");
            Calls++;
            return Task.FromResult(usageJson);
        }
    }

    private sealed class StubClaudeOAuthApiClient(string usageJson) : IClaudeOAuthApiClient
    {
        public int Calls { get; private set; }

        public Task<string> GetUsageJsonAsync(string accessToken, CancellationToken cancellationToken)
        {
            accessToken.Should().Be("sk-ant-oat01");
            Calls++;
            return Task.FromResult(usageJson);
        }
    }

    private sealed class StubCopilotApiClient(string usageJson) : ICopilotApiClient
    {
        public Task<string> GetUserUsageJsonAsync(
            string token,
            string? enterpriseHost,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(usageJson);
        }
    }
}
