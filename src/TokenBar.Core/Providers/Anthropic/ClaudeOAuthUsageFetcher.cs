using System.Text.Json;
using System.Text.Json.Serialization;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.Anthropic;

public sealed class ClaudeOAuthUsageFetcher(
    string accessToken,
    IClaudeOAuthApiClient apiClient)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var json = await apiClient.GetUsageJsonAsync(accessToken, cancellationToken);
        var response = JsonSerializer.Deserialize<ClaudeOAuthUsageResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Claude OAuth usage response was empty.");

        var session = MakePercentWindow("Current session", response.FiveHour);
        var weekly = MakePercentWindow("Weekly limits", response.SevenDay);

        return new UsageSnapshot(
            ProviderId.Claude,
            session ?? UsageWindow.Unknown("Current session"),
            weekly,
            "OAuth",
            UsageStatus.Available,
            DateTimeOffset.UtcNow,
            Message: $"{ToTitleCase(response.SubscriptionType)} plan usage limits");
    }

    private static UsageWindow? MakePercentWindow(string label, ClaudeOAuthUsageWindow? window)
    {
        if (window?.Utilization is null)
        {
            return null;
        }

        var percentUsed = Math.Clamp(window.Utilization.Value, 0m, 100m);
        return UsageWindow.FromUsedAndLimit(label, percentUsed, 100, TryParseResetAt(window.ResetsAt));
    }

    private static DateTimeOffset? TryParseResetAt(string? value)
    {
        return DateTimeOffset.TryParse(value, out var resetAt) ? resetAt : null;
    }

    private static string ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Claude";
        }

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    private sealed record ClaudeOAuthUsageResponse(
        [property: JsonPropertyName("five_hour")] ClaudeOAuthUsageWindow? FiveHour,
        [property: JsonPropertyName("seven_day")] ClaudeOAuthUsageWindow? SevenDay,
        [property: JsonPropertyName("subscriptionType")] string? SubscriptionType);

    private sealed record ClaudeOAuthUsageWindow(
        [property: JsonPropertyName("utilization")] decimal? Utilization,
        [property: JsonPropertyName("resets_at")] string? ResetsAt);
}
