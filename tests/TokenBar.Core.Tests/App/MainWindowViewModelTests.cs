using FluentAssertions;
using TokenBar.App.ViewModels;
using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Refresh;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.App;

public sealed class MainWindowViewModelTests
{
    [Fact]
    public async Task RefreshAsyncPopulatesProviderRows()
    {
        var provider = new StubProvider("codex", "Codex");
        var registry = new ProviderRegistry([provider]);
        var refreshService = new RefreshService(registry);
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [new ProviderConfig("codex", true, ProviderSourceMode.Auto)]);
        var viewModel = new MainWindowViewModel(refreshService, config);

        await viewModel.RefreshAsync(CancellationToken.None);

        viewModel.IsRefreshing.Should().BeFalse();
        viewModel.LastUpdated.Should().NotBeNull();
        viewModel.Providers.Should().ContainSingle();
        viewModel.Providers[0].ProviderId.Should().Be("codex");
        viewModel.Providers[0].DisplayName.Should().Be("Codex");
        viewModel.Providers[0].Status.Should().Be("available");
        viewModel.Providers[0].PrimaryLabel.Should().Be("Session");
        viewModel.Providers[0].PrimaryValue.Should().Be("Unavailable");
        viewModel.Providers[0].Message.Should().Be("ready");
    }

    [Fact]
    public async Task RefreshAsyncFormatsPercentUsageWindowsWithResetTimes()
    {
        var resetAt = new DateTimeOffset(2026, 5, 20, 12, 45, 0, TimeSpan.Zero);
        var provider = new StubProvider(
            "codex",
            "Codex",
            UsageWindow.FromUsedAndLimit("5h", 51, 100, resetAt),
            UsageWindow.FromUsedAndLimit("Weekly", 97, 100, resetAt.AddDays(5)));
        var registry = new ProviderRegistry([provider]);
        var refreshService = new RefreshService(registry);
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [new ProviderConfig("codex", true, ProviderSourceMode.Auto)]);
        var viewModel = new MainWindowViewModel(refreshService, config);

        await viewModel.RefreshAsync(CancellationToken.None);

        var row = viewModel.Providers[0];
        row.PrimaryLabel.Should().Be("5h");
        row.PrimaryValue.Should().Be("51% used");
        row.PrimaryPercentUsed.Should().Be(51);
        row.HasPrimaryPercent.Should().BeTrue();
        row.PrimaryReset.Should().NotBeEmpty();
        row.SecondaryLabel.Should().Be("Weekly");
        row.SecondaryValue.Should().Be("97% used");
        row.SecondaryPercentUsed.Should().Be(97);
        row.HasSecondaryPercent.Should().BeTrue();
    }

    [Fact]
    public void UpdateCountdownShowsRemainingTimeUntilNextRefresh()
    {
        var provider = new StubProvider("codex", "Codex");
        var registry = new ProviderRegistry([provider]);
        var refreshService = new RefreshService(registry);
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [new ProviderConfig("codex", true, ProviderSourceMode.Auto)]);
        var viewModel = new MainWindowViewModel(refreshService, config);

        viewModel.SetNextRefreshDueAt(new DateTimeOffset(2026, 5, 20, 12, 5, 0, TimeSpan.Zero));
        viewModel.UpdateCountdown(new DateTimeOffset(2026, 5, 20, 12, 3, 30, TimeSpan.Zero));

        viewModel.RefreshIntervalText.Should().Be("Every 5 minutes");
        viewModel.NextRefreshIn.Should().Be("Next refresh in 01:30");
    }

    private sealed class StubProvider(
        string providerId,
        string displayName,
        UsageWindow? primaryWindow = null,
        UsageWindow? secondaryWindow = null) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = new(
            providerId,
            displayName,
            "#000000",
            DefaultEnabled: true,
            [ProviderSourceMode.Auto]);

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UsageSnapshot(
                providerId,
                primaryWindow ?? UsageWindow.Unknown("Session"),
                secondaryWindow ?? UsageWindow.Unknown("Weekly"),
                "Cli",
                UsageStatus.Available,
                DateTimeOffset.UnixEpoch,
                Message: "ready"));
        }
    }
}
