using System.Text.Json;
using System.Text.Json.Serialization;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.Anthropic;

public sealed class AnthropicAdminUsageFetcher(
    string adminKey,
    IAnthropicAdminApiClient apiClient,
    string providerId = ProviderId.AnthropicApi,
    Func<DateTimeOffset>? now = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var clock = now ?? (() => DateTimeOffset.UtcNow);
        var currentTime = clock();
        var window = GetSevenDayWindow(currentTime);
        var json = await apiClient.GetMessagesUsageReportJsonAsync(adminKey, window.Start, window.End, cancellationToken);
        var response = JsonSerializer.Deserialize<AnthropicUsageResponse>(json, JsonOptions)
            ?? new AnthropicUsageResponse([]);

        decimal todayTokens = 0;
        decimal sevenDayTokens = 0;
        var today = currentTime.UtcDateTime.Date;

        foreach (var bucket in response.Data ?? [])
        {
            if (!DateTimeOffset.TryParse(bucket.StartingAt, out var bucketStart))
            {
                continue;
            }

            var bucketDate = bucketStart.UtcDateTime.Date;
            if (bucketDate < window.Start.UtcDateTime.Date || bucketDate >= window.End.UtcDateTime.Date)
            {
                continue;
            }

            var bucketTokens = (bucket.Results ?? []).Sum(SumTokens);
            sevenDayTokens += bucketTokens;
            if (bucketDate == today)
            {
                todayTokens += bucketTokens;
            }
        }

        return new UsageSnapshot(
            providerId,
            UsageWindow.FromUsedAndLimit("Today", todayTokens, 0, null),
            UsageWindow.FromUsedAndLimit("7 days", sevenDayTokens, 0, null),
            "Api",
            UsageStatus.Available,
            DateTimeOffset.UtcNow,
            Message: "Anthropic Admin API");
    }

    private static decimal SumTokens(AnthropicUsageResult result)
    {
        return result.UncachedInputTokens +
            result.CacheReadInputTokens +
            result.OutputTokens +
            (result.CacheCreation?.Ephemeral1hInputTokens ?? 0) +
            (result.CacheCreation?.Ephemeral5mInputTokens ?? 0);
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetSevenDayWindow(DateTimeOffset now)
    {
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        return (today.AddDays(-6), today.AddDays(1));
    }

    private sealed record AnthropicUsageResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<AnthropicUsageBucket>? Data);

    private sealed record AnthropicUsageBucket(
        [property: JsonPropertyName("starting_at")] string? StartingAt,
        [property: JsonPropertyName("results")] IReadOnlyList<AnthropicUsageResult>? Results);

    private sealed record AnthropicUsageResult(
        [property: JsonPropertyName("uncached_input_tokens")] decimal UncachedInputTokens,
        [property: JsonPropertyName("cache_creation")] AnthropicCacheCreation? CacheCreation,
        [property: JsonPropertyName("cache_read_input_tokens")] decimal CacheReadInputTokens,
        [property: JsonPropertyName("output_tokens")] decimal OutputTokens);

    private sealed record AnthropicCacheCreation(
        [property: JsonPropertyName("ephemeral_1h_input_tokens")] decimal Ephemeral1hInputTokens,
        [property: JsonPropertyName("ephemeral_5m_input_tokens")] decimal Ephemeral5mInputTokens);
}
