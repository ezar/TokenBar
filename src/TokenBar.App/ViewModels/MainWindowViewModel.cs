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
                Providers.Add(new ProviderRowViewModel(
                    snapshot.ProviderId,
                    ToDisplayName(snapshot.ProviderId),
                    ToBrandColor(snapshot.ProviderId),
                    snapshot.Status.ToString().ToLowerInvariant(),
                    ToStatusColor(snapshot.Status),
                    snapshot.Source,
                    FormatUsageWindow(snapshot.PrimaryWindow),
                    snapshot.SecondaryWindow is null ? string.Empty : FormatUsageWindow(snapshot.SecondaryWindow),
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

    private static string FormatUsageWindow(TokenBar.Core.Usage.UsageWindow window)
    {
        return window.Used is null
            ? window.Label
            : $"{window.Label}: {window.Used:N0} tokens";
    }

    private static string ToDisplayName(string providerId)
    {
        return providerId switch
        {
            "codex" => "Codex",
            "claude" => "Claude",
            "copilot" => "GitHub Copilot",
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
}
