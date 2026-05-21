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
        var json = await apiClient.GetUserUsageJsonAsync(token, enterpriseHost, cancellationToken);
        var usage = JsonSerializer.Deserialize<CopilotUsageResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Copilot usage response was empty.");

        var premium = MakeWindow("Premium", usage.QuotaSnapshots?.PremiumInteractions);
        var chat = MakeWindow("Chat", usage.QuotaSnapshots?.Chat);

        var primary = premium ?? chat ?? throw new InvalidOperationException("Copilot usage response did not contain quota snapshots.");
        var secondary = premium is not null ? chat : null;

        return new UsageSnapshot(
            ProviderId.Copilot,
            primary,
            secondary,
            "Api",
            UsageStatus.Available,
            DateTimeOffset.UtcNow,
            Message: ToTitleCase(usage.CopilotPlan));
    }

    private static UsageWindow? MakeWindow(string label, CopilotQuotaSnapshot? snapshot)
    {
        if (snapshot is null || snapshot.IsPlaceholder || snapshot.PercentRemaining is null)
        {
            return null;
        }

        var percentUsed = Math.Clamp(100m - snapshot.PercentRemaining.Value, 0m, 100m);
        return UsageWindow.FromUsedAndLimit(label, percentUsed, 100, null);
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
        [property: JsonPropertyName("is_placeholder")] bool IsPlaceholder);
}
