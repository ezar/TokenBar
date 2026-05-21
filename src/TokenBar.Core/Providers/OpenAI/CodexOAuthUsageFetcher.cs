using System.Text.Json;
using System.Text.Json.Serialization;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers.OpenAI;

public sealed class CodexOAuthUsageFetcher(
    CodexOAuthCredentials credentials,
    ICodexOAuthApiClient apiClient,
    Func<DateTimeOffset>? now = null)
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken)
    {
        var clock = now ?? (() => DateTimeOffset.UtcNow);
        var json = await apiClient.GetUsageJsonAsync(credentials.AccessToken, credentials.AccountId, cancellationToken);
        var response = JsonSerializer.Deserialize<CodexOAuthUsageResponse>(json, JsonOptions)
            ?? throw new InvalidOperationException("Codex OAuth usage response was empty.");

        var primaryWindow = MakeWindow("5h", response.RateLimit?.PrimaryWindow, clock());
        var secondaryWindow = MakeWindow("Weekly", response.RateLimit?.SecondaryWindow, clock());

        return new UsageSnapshot(
            ProviderId.Codex,
            primaryWindow ?? UsageWindow.Unknown("5h"),
            secondaryWindow,
            "OAuth",
            UsageStatus.Available,
            DateTimeOffset.UtcNow,
            Message: FormatMessage(response));
    }

    private static UsageWindow? MakeWindow(string label, CodexUsageWindow? window, DateTimeOffset now)
    {
        if (window?.UsedPercent is null)
        {
            return null;
        }

        var percentUsed = Math.Clamp(window.UsedPercent.Value, 0m, 100m);
        return UsageWindow.FromUsedAndLimit(label, percentUsed, 100, GetResetAt(window, now));
    }

    private static DateTimeOffset? GetResetAt(CodexUsageWindow window, DateTimeOffset now)
    {
        if (window.ResetAt is not null)
        {
            return DateTimeOffset.FromUnixTimeSeconds(window.ResetAt.Value);
        }

        return window.ResetAfterSeconds is null
            ? null
            : now.AddSeconds(window.ResetAfterSeconds.Value);
    }

    private static string FormatMessage(CodexOAuthUsageResponse response)
    {
        var plan = string.IsNullOrWhiteSpace(response.PlanType) ? "Codex" : response.PlanType;
        var credits = response.Credits;

        if (credits?.Unlimited == true)
        {
            return $"{plan} plan, unlimited credits";
        }

        if (credits?.HasCredits == true && !string.IsNullOrWhiteSpace(credits.Balance))
        {
            return $"{plan} plan, credits {credits.Balance}";
        }

        return $"{plan} plan usage limits";
    }

    private sealed record CodexOAuthUsageResponse(
        [property: JsonPropertyName("plan_type")] string? PlanType,
        [property: JsonPropertyName("rate_limit")] CodexRateLimit? RateLimit,
        [property: JsonPropertyName("credits")] CodexCredits? Credits);

    private sealed record CodexRateLimit(
        [property: JsonPropertyName("primary_window")] CodexUsageWindow? PrimaryWindow,
        [property: JsonPropertyName("secondary_window")] CodexUsageWindow? SecondaryWindow);

    private sealed record CodexUsageWindow(
        [property: JsonPropertyName("used_percent")] decimal? UsedPercent,
        [property: JsonPropertyName("limit_window_seconds")] decimal? LimitWindowSeconds,
        [property: JsonPropertyName("reset_at")] long? ResetAt,
        [property: JsonPropertyName("reset_after_seconds")] double? ResetAfterSeconds);

    private sealed record CodexCredits(
        [property: JsonPropertyName("has_credits")] bool? HasCredits,
        [property: JsonPropertyName("unlimited")] bool? Unlimited,
        [property: JsonPropertyName("balance")] string? Balance);
}
