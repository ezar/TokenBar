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
        viewModel.Providers[0].Primary.Should().Be("Session");
        viewModel.Providers[0].Message.Should().Be("ready");
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

    private sealed class StubProvider(string providerId, string displayName) : IUsageProvider
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
                UsageWindow.Unknown("Session"),
                UsageWindow.Unknown("Weekly"),
                "Cli",
                UsageStatus.Available,
                DateTimeOffset.UnixEpoch,
                Message: "ready"));
        }
    }
}
