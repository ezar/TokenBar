using FluentAssertions;
using TokenBar.Core.Providers.OpenAI;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers.OpenAI;

public sealed class CodexOAuthUsageFetcherTests
{
    [Fact]
    public async Task FetchAsyncMapsPrimaryAndSecondaryRateLimitWindows()
    {
        const string usageJson = """
        {
          "plan_type": "Pro",
          "rate_limit": {
            "primary_window": {
              "used_percent": 89,
              "limit_window_seconds": 18000,
              "reset_at": 1779308700
            },
            "secondary_window": {
              "used_percent": 9,
              "limit_window_seconds": 604800,
              "reset_after_seconds": 432000
            }
          },
          "credits": {
            "has_credits": true,
            "unlimited": false,
            "balance": "12.50"
          }
        }
        """;
        var fetcher = new CodexOAuthUsageFetcher(
            new CodexOAuthCredentials("codex-token", "account-123"),
            new StubCodexOAuthApiClient(usageJson),
            () => new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var snapshot = await fetcher.FetchAsync(CancellationToken.None);

        snapshot.ProviderId.Should().Be("codex");
        snapshot.Source.Should().Be("OAuth");
        snapshot.Status.Should().Be(UsageStatus.Available);
        snapshot.PrimaryWindow.Label.Should().Be("5h");
        snapshot.PrimaryWindow.PercentUsed.Should().Be(89);
        snapshot.PrimaryWindow.ResetAt.Should().Be(DateTimeOffset.FromUnixTimeSeconds(1779308700));
        snapshot.SecondaryWindow.Should().NotBeNull();
        snapshot.SecondaryWindow!.Label.Should().Be("Weekly");
        snapshot.SecondaryWindow.PercentUsed.Should().Be(9);
        snapshot.SecondaryWindow.ResetAt.Should().Be(new DateTimeOffset(2026, 5, 25, 12, 0, 0, TimeSpan.Zero));
        snapshot.Message.Should().Be("Pro plan, credits 12.50");
    }

    private sealed class StubCodexOAuthApiClient(string usageJson) : ICodexOAuthApiClient
    {
        public Task<string> GetUsageJsonAsync(
            string accessToken,
            string? accountId,
            CancellationToken cancellationToken)
        {
            accessToken.Should().Be("codex-token");
            accountId.Should().Be("account-123");
            return Task.FromResult(usageJson);
        }
    }
}
