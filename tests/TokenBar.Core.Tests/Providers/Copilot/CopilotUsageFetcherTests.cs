using FluentAssertions;
using TokenBar.Core.Providers.Copilot;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers.Copilot;

public sealed class CopilotUsageFetcherTests
{
    [Fact]
    public async Task FetchAsyncMapsPremiumAndChatQuotaSnapshots()
    {
        const string json = """
        {
          "copilot_plan": "pro",
          "quota_snapshots": {
            "premium_interactions": {
              "percent_remaining": 75,
              "is_placeholder": false
            },
            "chat": {
              "percent_remaining": 40,
              "is_placeholder": false
            }
          }
        }
        """;
        var fetcher = new CopilotUsageFetcher(
            "gho_test",
            new StubCopilotApiClient(json));

        var snapshot = await fetcher.FetchAsync(CancellationToken.None);

        snapshot.ProviderId.Should().Be("copilot");
        snapshot.Status.Should().Be(UsageStatus.Available);
        snapshot.Source.Should().Be("Api");
        snapshot.PrimaryWindow.Label.Should().Be("Premium");
        snapshot.PrimaryWindow.PercentUsed.Should().Be(25);
        snapshot.SecondaryWindow.Should().NotBeNull();
        snapshot.SecondaryWindow!.Label.Should().Be("Chat");
        snapshot.SecondaryWindow.PercentUsed.Should().Be(60);
        snapshot.Message.Should().Be("Pro");
    }

    [Fact]
    public async Task FetchAsyncIgnoresPlaceholderSnapshots()
    {
        const string json = """
        {
          "copilot_plan": "free",
          "quota_snapshots": {
            "premium_interactions": {
              "percent_remaining": 99,
              "is_placeholder": true
            },
            "chat": {
              "percent_remaining": 20,
              "is_placeholder": false
            }
          }
        }
        """;
        var fetcher = new CopilotUsageFetcher(
            "gho_test",
            new StubCopilotApiClient(json));

        var snapshot = await fetcher.FetchAsync(CancellationToken.None);

        snapshot.PrimaryWindow.Label.Should().Be("Chat");
        snapshot.PrimaryWindow.PercentUsed.Should().Be(80);
        snapshot.SecondaryWindow.Should().BeNull();
    }

    private sealed class StubCopilotApiClient(string json) : ICopilotApiClient
    {
        public Task<string> GetUserUsageJsonAsync(
            string token,
            string? enterpriseHost,
            CancellationToken cancellationToken)
        {
            token.Should().Be("gho_test");
            enterpriseHost.Should().BeNull();
            return Task.FromResult(json);
        }
    }
}
