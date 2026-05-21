using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using TokenBar.Core.Configuration;
using TokenBar.Core.Refresh;

namespace TokenBar.App.ViewModels;

public sealed class MainWindowViewModel(RefreshService refreshService, TokenBarConfig config) : INotifyPropertyChanged
{
    private bool isRefreshing;
    private string? lastUpdated;
    private string nextRefreshIn = "Not scheduled";

    public event PropertyChangedEventHandler? PropertyChanged;

    public ObservableCollection<ProviderRowViewModel> Providers { get; } = [];

    public string RefreshIntervalText => $"Every {config.RefreshInterval.TotalMinutes:0} minutes";

    public DateTimeOffset? NextRefreshDueAt { get; private set; }

    public bool IsRefreshing
    {
        get => isRefreshing;
        private set
        {
            if (isRefreshing == value)
            {
                return;
            }

            isRefreshing = value;
            OnPropertyChanged();
        }
    }

    public string? LastUpdated
    {
        get => lastUpdated;
        private set
        {
            if (lastUpdated == value)
            {
                return;
            }

            lastUpdated = value;
            OnPropertyChanged();
        }
    }

    public string NextRefreshIn
    {
        get => nextRefreshIn;
        private set
        {
            if (nextRefreshIn == value)
            {
                return;
            }

            nextRefreshIn = value;
            OnPropertyChanged();
        }
    }

    public async Task RefreshAsync(CancellationToken cancellationToken)
    {
        IsRefreshing = true;

        try
        {
            var result = await refreshService.RefreshOnceAsync(config, cancellationToken);

            Providers.Clear();
            foreach (var snapshot in result.Snapshots)
            {
                var message = snapshot.Message ?? string.Empty;
                var primary = FormatUsageWindow(snapshot.PrimaryWindow, result.RefreshedAt.ToLocalTime());
                var secondary = snapshot.SecondaryWindow is null
                    ? FormattedUsageWindow.Empty
                    : FormatUsageWindow(snapshot.SecondaryWindow, result.RefreshedAt.ToLocalTime());
                Providers.Add(new ProviderRowViewModel(
                    snapshot.ProviderId,
                    ToDisplayName(snapshot.ProviderId),
                    ToBrandColor(snapshot.ProviderId),
                    snapshot.Status.ToString().ToLowerInvariant(),
                    ToStatusColor(snapshot.Status),
                    snapshot.Source,
                    primary.Label,
                    primary.Value,
                    primary.Reset,
                    primary.PercentUsed,
                    primary.HasPercent,
                    secondary.Label,
                    secondary.Value,
                    secondary.Reset,
                    secondary.PercentUsed,
                    secondary.HasPercent,
                    snapshot.SecondaryWindow is not null,
                    message,
                    !string.IsNullOrWhiteSpace(message)));
            }

            LastUpdated = result.RefreshedAt.ToLocalTime().ToString("HH:mm:ss");
            SetNextRefreshDueAt(DateTimeOffset.UtcNow.Add(config.RefreshInterval));
        }
        finally
        {
            IsRefreshing = false;
        }
    }

    public void SetNextRefreshDueAt(DateTimeOffset dueAt)
    {
        NextRefreshDueAt = dueAt;
        UpdateCountdown(DateTimeOffset.UtcNow);
    }

    public void UpdateCountdown(DateTimeOffset now)
    {
        if (NextRefreshDueAt is null)
        {
            NextRefreshIn = "Not scheduled";
            return;
        }

        var remaining = NextRefreshDueAt.Value - now;
        if (remaining <= TimeSpan.Zero)
        {
            NextRefreshIn = "Refresh due";
            return;
        }

        NextRefreshIn = $"Next refresh in {remaining:mm\\:ss}";
    }

    private static FormattedUsageWindow FormatUsageWindow(
        TokenBar.Core.Usage.UsageWindow window,
        DateTimeOffset now)
    {
        var value = window.PercentUsed is not null
            ? $"{window.PercentUsed.Value:N0}% used"
            : window.Used is null
                ? "Unavailable"
                : $"{window.Used:N0} tokens";

        return new FormattedUsageWindow(
            window.Label,
            value,
            FormatReset(window.ResetAt, now),
            window.PercentUsed ?? 0,
            window.PercentUsed is not null);
    }

    private static string FormatReset(DateTimeOffset? resetAt, DateTimeOffset now)
    {
        if (resetAt is null)
        {
            return string.Empty;
        }

        var localResetAt = resetAt.Value.ToLocalTime();
        var remaining = localResetAt - now;
        if (remaining > TimeSpan.Zero && remaining < TimeSpan.FromDays(1))
        {
            var hours = (int)Math.Floor(remaining.TotalHours);
            return $"Resets in {hours} h {remaining.Minutes:00} min";
        }

        return $"Resets {localResetAt:MMM d HH:mm}";
    }

    private static string ToDisplayName(string providerId)
    {
        return providerId switch
        {
            "codex" => "Codex",
            "claude" => "Claude",
            "copilot" => "GitHub Copilot",
            "openai-api" => "OpenAI API",
            "anthropic-api" => "Anthropic API",
            _ => providerId
        };
    }

    private static string ToBrandColor(string providerId)
    {
        return providerId switch
        {
            "codex" => "#111827",
            "claude" => "#B85C38",
            "copilot" => "#1F883D",
            "openai-api" => "#111827",
            "anthropic-api" => "#B85C38",
            _ => "#4B5563"
        };
    }

    private static string ToStatusColor(TokenBar.Core.Usage.UsageStatus status)
    {
        return status switch
        {
            TokenBar.Core.Usage.UsageStatus.Available => "#1F883D",
            TokenBar.Core.Usage.UsageStatus.Stale => "#9A6700",
            TokenBar.Core.Usage.UsageStatus.Error => "#CF222E",
            _ => "#57606A"
        };
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    private sealed record FormattedUsageWindow(
        string Label,
        string Value,
        string Reset,
        decimal PercentUsed,
        bool HasPercent)
    {
        public static FormattedUsageWindow Empty { get; } = new(string.Empty, string.Empty, string.Empty, 0, false);
    }
}
