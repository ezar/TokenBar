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
            .Should().Equal(ProviderId.Codex, ProviderId.Claude, ProviderId.Copilot);

        providers.SelectMany(provider => provider.Descriptor.SupportedSourceModes)
            .Should().Contain(ProviderSourceMode.Api);
    }

    [Fact]
    public async Task OpenAiAndAnthropicProvidersFetchUsageFromApis()
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
            openAITokenProvider: () => "openai-admin",
            anthropicAdminKeyProvider: () => "anthropic-admin",
            copilotTokenProvider: () => null,
            openAIApiClient: openAiClient,
            anthropicAdminApiClient: anthropicClient,
            copilotApiClient: new StubCopilotApiClient("{}"),
            now: () => new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var codex = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Codex)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);
        var claude = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Claude)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);

        codex.Status.Should().Be(UsageStatus.Available);
        codex.Source.Should().Be("Api");
        codex.PrimaryWindow.Used.Should().Be(16);
        claude.Status.Should().Be(UsageStatus.Available);
        claude.Source.Should().Be("Api");
        claude.PrimaryWindow.Used.Should().Be(60);
        openAiClient.Calls.Should().Be(1);
        anthropicClient.Calls.Should().Be(1);
    }

    [Fact]
    public async Task OpenAiAndAnthropicProvidersReturnStaleSnapshotsWhenApiKeysAreMissing()
    {
        var providers = BuiltInProviderFactory.CreateProviders(
            openAITokenProvider: () => null,
            anthropicAdminKeyProvider: () => null,
            copilotTokenProvider: () => null,
            openAIApiClient: new StubOpenAIApiClient("{}"),
            anthropicAdminApiClient: new StubAnthropicAdminApiClient("{}"),
            copilotApiClient: new StubCopilotApiClient("{}"));

        var codex = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Codex)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);
        var claude = await providers.Single(provider => provider.Descriptor.ProviderId == ProviderId.Claude)
            .FetchAsync(ProviderSourceMode.Auto, CancellationToken.None);

        codex.Status.Should().Be(UsageStatus.Stale);
        codex.Source.Should().Be("Api");
        codex.Message.Should().Contain("OPENAI_ADMIN_KEY");
        claude.Status.Should().Be(UsageStatus.Stale);
        claude.Source.Should().Be("Api");
        claude.Message.Should().Contain("ANTHROPIC_ADMIN_KEY");
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
