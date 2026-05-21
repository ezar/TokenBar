using FluentAssertions;
using TokenBar.Core.Providers;
using TokenBar.Core.Providers.OpenAI;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers.OpenAI;

public sealed class OpenAIUsageFetcherTests
{
    [Fact]
    public async Task FetchAsyncMapsTodayAndSevenDayCompletionUsage()
    {
        const string usageJson = """
        {
          "data": [
            {
              "start_time": 1779235200,
              "results": [
                { "input_tokens": 1000, "output_tokens": 250, "input_cached_tokens": 100, "num_model_requests": 3 }
              ]
            },
            {
              "start_time": 1779148800,
              "results": [
                { "input_tokens": 500, "output_tokens": 100, "input_cached_tokens": 50, "num_model_requests": 1 }
              ]
            },
            {
              "start_time": 1777507200,
              "results": [
                { "input_tokens": 999, "output_tokens": 999, "num_model_requests": 9 }
              ]
            }
          ]
        }
        """;
        var fetcher = new OpenAIUsageFetcher(
            "sk-admin",
            new StubOpenAIApiClient(usageJson),
            ProviderId.OpenAIApi,
            () => new DateTimeOffset(2026, 5, 20, 12, 0, 0, TimeSpan.Zero));

        var snapshot = await fetcher.FetchAsync(CancellationToken.None);

        snapshot.ProviderId.Should().Be("openai-api");
        snapshot.Source.Should().Be("Api");
        snapshot.Status.Should().Be(UsageStatus.Available);
        snapshot.PrimaryWindow.Label.Should().Be("Today");
        snapshot.PrimaryWindow.Used.Should().Be(1350);
        snapshot.SecondaryWindow.Should().NotBeNull();
        snapshot.SecondaryWindow!.Label.Should().Be("7 days");
        snapshot.SecondaryWindow.Used.Should().Be(2000);
        snapshot.Message.Should().Be("4 requests");
    }

    private sealed class StubOpenAIApiClient(string usageJson) : IOpenAIApiClient
    {
        public Task<string> GetCompletionsUsageJsonAsync(
            string token,
            DateTimeOffset start,
            DateTimeOffset end,
            CancellationToken cancellationToken)
        {
            token.Should().Be("sk-admin");
            start.Should().Be(new DateTimeOffset(2026, 5, 14, 0, 0, 0, TimeSpan.Zero));
            end.Should().Be(new DateTimeOffset(2026, 5, 21, 0, 0, 0, TimeSpan.Zero));
            return Task.FromResult(usageJson);
        }
    }
}
