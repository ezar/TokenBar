using System.Text.Json;
using System.Text.Json.Serialization;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.OpenAI;

public sealed class OpenAIUsageFetcher(
    string token,
    IOpenAIApiClient apiClient,
    string providerId = ProviderId.OpenAIApi,
    Func<DateTimeOffset>? now = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var clock = now ?? (() => DateTimeOffset.UtcNow);
        var currentTime = clock();
        var window = GetSevenDayWindow(currentTime);
        var json = await apiClient.GetCompletionsUsageJsonAsync(token, window.Start, window.End, cancellationToken);
        var response = JsonSerializer.Deserialize<OpenAIUsageResponse>(json, JsonOptions)
            ?? new OpenAIUsageResponse([]);

        decimal todayTokens = 0;
        decimal sevenDayTokens = 0;
        decimal requests = 0;
        var today = currentTime.UtcDateTime.Date;

        foreach (var bucket in response.Data ?? [])
        {
            if (bucket.StartTime is null)
            {
                continue;
            }

            var bucketDate = DateTimeOffset.FromUnixTimeSeconds(bucket.StartTime.Value).UtcDateTime.Date;
            if (bucketDate < window.Start.UtcDateTime.Date || bucketDate >= window.End.UtcDateTime.Date)
            {
                continue;
            }

            var bucketTokens = 0m;
            foreach (var result in bucket.Results ?? [])
            {
                bucketTokens += result.InputTokens + result.OutputTokens + result.InputCachedTokens;
                requests += result.NumModelRequests;
            }

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
            Message: $"{requests:0} requests");
    }

    private static (DateTimeOffset Start, DateTimeOffset End) GetSevenDayWindow(DateTimeOffset now)
    {
        var today = new DateTimeOffset(now.UtcDateTime.Date, TimeSpan.Zero);
        return (today.AddDays(-6), today.AddDays(1));
    }

    private sealed record OpenAIUsageResponse(
        [property: JsonPropertyName("data")] IReadOnlyList<OpenAIUsageBucket>? Data);

    private sealed record OpenAIUsageBucket(
        [property: JsonPropertyName("start_time")] long? StartTime,
        [property: JsonPropertyName("results")] IReadOnlyList<OpenAIUsageResult>? Results);

    private sealed record OpenAIUsageResult(
        [property: JsonPropertyName("input_tokens")] decimal InputTokens,
        [property: JsonPropertyName("output_tokens")] decimal OutputTokens,
        [property: JsonPropertyName("input_cached_tokens")] decimal InputCachedTokens,
        [property: JsonPropertyName("num_model_requests")] decimal NumModelRequests);
}
