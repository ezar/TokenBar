using FluentAssertions;
using TokenBar.Core.Providers.Anthropic;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers.Anthropic;

public sealed class ClaudeOAuthUsageFetcherTests
{
    [Fact]
    public async Task FetchAsyncMapsSessionAndWeeklyPlanLimits()
    {
        const string usageJson = """
        {
          "five_hour": {
            "utilization": 53,
            "resets_at": "2026-05-21T14:30:00Z"
          },
          "seven_day": {
            "utilization": 100,
            "resets_at": "2026-05-23T11:00:00Z"
          },
          "subscriptionType": "pro"
        }
        """;
        var fetcher = new ClaudeOAuthUsageFetcher(
            "sk-ant-oat01-test",
            new StubClaudeOAuthApiClient(usageJson));

        var snapshot = await fetcher.FetchAsync(CancellationToken.None);

        snapshot.ProviderId.Should().Be("claude");
        snapshot.Source.Should().Be("OAuth");
        snapshot.Status.Should().Be(UsageStatus.Available);
        snapshot.PrimaryWindow.Label.Should().Be("Current session");
        snapshot.PrimaryWindow.PercentUsed.Should().Be(53);
        snapshot.PrimaryWindow.ResetAt.Should().Be(new DateTimeOffset(2026, 5, 21, 14, 30, 0, TimeSpan.Zero));
        snapshot.SecondaryWindow.Should().NotBeNull();
        snapshot.SecondaryWindow!.Label.Should().Be("Weekly limits");
        snapshot.SecondaryWindow.PercentUsed.Should().Be(100);
        snapshot.SecondaryWindow.ResetAt.Should().Be(new DateTimeOffset(2026, 5, 23, 11, 0, 0, TimeSpan.Zero));
        snapshot.Message.Should().Be("Pro plan usage limits");
    }

    private sealed class StubClaudeOAuthApiClient(string usageJson) : IClaudeOAuthApiClient
    {
        public Task<string> GetUsageJsonAsync(string accessToken, CancellationToken cancellationToken)
        {
            accessToken.Should().Be("sk-ant-oat01-test");
            return Task.FromResult(usageJson);
        }
    }
}
