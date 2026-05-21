using FluentAssertions;
using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Refresh;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Refresh;

public sealed class RefreshServiceTests
{
    [Fact]
    public async Task RefreshOnceAsyncFetchesEnabledProvidersInConfigOrder()
    {
        var claude = new StubProvider("claude");
        var codex = new StubProvider("codex");
        var service = new RefreshService(new ProviderRegistry([codex, claude]));
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [
                new ProviderConfig("claude", true, ProviderSourceMode.Logs),
                new ProviderConfig("codex", true, ProviderSourceMode.Cli)
            ]);

        var result = await service.RefreshOnceAsync(config, CancellationToken.None);

        result.Snapshots.Select(snapshot => snapshot.ProviderId)
            .Should().Equal("claude", "codex");
        claude.LastSourceMode.Should().Be(ProviderSourceMode.Logs);
        codex.LastSourceMode.Should().Be(ProviderSourceMode.Cli);
    }

    [Fact]
    public async Task RefreshOnceAsyncReturnsErrorSnapshotWhenProviderFails()
    {
        var provider = new ThrowingProvider("codex");
        var service = new RefreshService(new ProviderRegistry([provider]));
        var config = new TokenBarConfig(
            TimeSpan.FromMinutes(5),
            [new ProviderConfig("codex", true, ProviderSourceMode.Auto)]);

        var result = await service.RefreshOnceAsync(config, CancellationToken.None);

        result.Snapshots.Should().ContainSingle();
        result.Snapshots[0].Status.Should().Be(UsageStatus.Error);
        result.Snapshots[0].Message.Should().Be("provider exploded");
    }

    private sealed class StubProvider(string providerId) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = new(providerId, providerId, "#000000", true, [ProviderSourceMode.Auto]);

        public ProviderSourceMode? LastSourceMode { get; private set; }

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            LastSourceMode = sourceMode;
            return Task.FromResult(new UsageSnapshot(
                providerId,
                UsageWindow.Unknown("Session"),
                null,
                sourceMode.ToString(),
                UsageStatus.Available,
                DateTimeOffset.UnixEpoch));
        }
    }

    private sealed class ThrowingProvider(string providerId) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = new(providerId, providerId, "#000000", true, [ProviderSourceMode.Auto]);

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("provider exploded");
        }
    }
}
