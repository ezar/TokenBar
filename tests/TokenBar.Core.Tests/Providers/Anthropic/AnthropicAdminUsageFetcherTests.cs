using FluentAssertions;
using TokenBar.Core.Providers;
using TokenBar.Core.Providers.Anthropic;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers.Anthropic;

public sealed class AnthropicAdminUsageFetcherTests
{
    [Fact]
    public async Task FetchAsyncMapsTodayAndSevenDayMessageUsage()
    {
        const string usageJson = """
        {
          "data": [
            {
              "starting_at": "2026-05-20T00:00:00Z",
              "results": [
                {
                  "uncached_input_tokens": 1000,
                  "cache_creation": { "ephemeral_1h_input_tokens": 200, "ephemeral_5m_input_tokens": 50 },
                  "cache_read_input_tokens": 500,
                  "output_tokens": 250
                }
              ]
            },
            {
              "starting_at": "2026-05-19T00:00:00Z",
              "results": [
                {
                  "uncached_input_tokens": 100,
                  "cache_read_input_tokens": 50,
                  "output_tokens": 25
                }
              ]
            }
          ]
        }
        """;
        var fetcher = new AnthropicAdminUsageFetcher(
            "sk-ant-admin",
            new StubAnthropicAdminApiClient(usageJson),
            ProviderId.AnthropicApi,
            () => new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var snapshot = await fetcher.FetchAsync(CancellationToken.None);

        snapshot.ProviderId.Should().Be("anthropic-api");
        snapshot.Source.Should().Be("Api");
        snapshot.Status.Should().Be(UsageStatus.Available);
        snapshot.PrimaryWindow.Label.Should().Be("Today");
        snapshot.PrimaryWindow.Used.Should().Be(2000);
        snapshot.SecondaryWindow.Should().NotBeNull();
        snapshot.SecondaryWindow!.Label.Should().Be("7 days");
        snapshot.SecondaryWindow.Used.Should().Be(2175);
        snapshot.Message.Should().Be("Anthropic Admin API");
    }

    private sealed class StubAnthropicAdminApiClient(string usageJson) : IAnthropicAdminApiClient
    {
        public Task<string> GetMessagesUsageReportJsonAsync(
            string adminKey,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            adminKey.Should().Be("sk-ant-admin");
            start.Should().Be(new DateTimeOffset(2026, 5, 14, 0, 0, 0, TimeSpan.Zero));
            end.Should().Be(new DateTimeOffset(2026, 5, 21, 0, 0, 0, TimeSpan.Zero));
            return Task.FromResult(usageJson);
        }
    }
}
