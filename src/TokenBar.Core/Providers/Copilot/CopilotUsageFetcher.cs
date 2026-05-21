using System.Text.Json;
using System.Text.Json.Serialization;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.Copilot;

public sealed class CopilotUsageFetcher(
    string token,
    ICopilotApiClient apiClient,
    string? enterpriseHost = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var fetchedAt = DateTimeOffset.UtcNow;
        var json = await apiClient.GetUserUsageJsonAsync(token, enterpriseHost, cancellationToken);
        var usage = JsonSerializer.Deserialize<CopilotUsageResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Copilot usage response was empty.");

        var premium = MakeWindow("Premium", usage.QuotaSnapshots?.PremiumInteractions, fetchedAt);
        var chat = MakeWindow("Chat", usage.QuotaSnapshots?.Chat, fetchedAt);

        var primary = premium ?? chat ?? throw new InvalidOperationException("Copilot usage response did not contain quota snapshots.");
        var secondary = premium is not null ? chat : null;

        return new UsageSnapshot(
            ProviderId.Copilot,
            primary,
            secondary,
            "Api",
            UsageStatus.Available,
            fetchedAt,
            Message: ToTitleCase(usage.CopilotPlan));
    }

    private static UsageWindow? MakeWindow(string label, CopilotQuotaSnapshot? snapshot, DateTimeOffset fetchedAt)
    {
        if (snapshot is null || snapshot.IsPlaceholder || snapshot.PercentRemaining is null)
        {
            return null;
        }

        var percentUsed = Math.Clamp(100m - snapshot.PercentRemaining.Value, 0m, 100m);
        return UsageWindow.FromUsedAndLimit(label, percentUsed, 100, GetResetAt(snapshot, fetchedAt));
    }

    private static DateTimeOffset? GetResetAt(CopilotQuotaSnapshot snapshot, DateTimeOffset fetchedAt)
    {
        if (snapshot.ResetAfterSeconds is not null)
        {
            return fetchedAt.AddSeconds(snapshot.ResetAfterSeconds.Value);
        }

        if (snapshot.ResetAtUnixSeconds is not null)
        {
            return DateTimeOffset.FromUnixTimeSeconds(snapshot.ResetAtUnixSeconds.Value);
        }

        if (DateTimeOffset.TryParse(
            snapshot.ResetAt ?? snapshot.ResetsAt ?? snapshot.ResetDate,
            out var resetAt))
        {
            return resetAt;
        }

        return null;
    }

    private static string ToTitleCase(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "Copilot";
        }

        return char.ToUpperInvariant(value[0]) + value[1..].ToLowerInvariant();
    }

    private sealed record CopilotUsageResponse(
        [property: JsonPropertyName("copilot_plan")] string? CopilotPlan,
        [property: JsonPropertyName("quota_snapshots")] CopilotQuotaSnapshots? QuotaSnapshots);

    private sealed record CopilotQuotaSnapshots(
        [property: JsonPropertyName("premium_interactions")] CopilotQuotaSnapshot? PremiumInteractions,
        [property: JsonPropertyName("chat")] CopilotQuotaSnapshot? Chat);

    private sealed record CopilotQuotaSnapshot(
        [property: JsonPropertyName("percent_remaining")] decimal? PercentRemaining,
        [property: JsonPropertyName("reset_after_seconds")] double? ResetAfterSeconds,
        [property: JsonPropertyName("reset_at_seconds")] long? ResetAtUnixSeconds,
        [property: JsonPropertyName("reset_at")] string? ResetAt,
        [property: JsonPropertyName("resets_at")] string? ResetsAt,
        [property: JsonPropertyName("reset_date")] string? ResetDate,
        [property: JsonPropertyName("is_placeholder")] bool IsPlaceholder);
}
