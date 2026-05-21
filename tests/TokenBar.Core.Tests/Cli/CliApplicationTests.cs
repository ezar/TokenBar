using FluentAssertions;
using TokenBar.Cli;
using TokenBar.Core.Providers;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Cli;

public sealed class CliApplicationTests
{
    [Fact]
    public async Task RunAsyncShowsHelpWhenNoArgumentsAreProvided()
    {
        using var output = new StringWriter();

        var exitCode = await CliApplication.RunAsync([], output, CancellationToken.None);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("TokenBar");
        output.ToString().Should().Contain("usage");
        output.ToString().Should().Contain("status");
    }

    [Fact]
    public async Task RunAsyncStatusShowsConfiguredProviders()
    {
        using var output = new StringWriter();

        var exitCode = await CliApplication.RunAsync(["status"], output, CancellationToken.None);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("Refresh interval: 00:05:00");
        output.ToString().Should().Contain("codex");
        output.ToString().Should().Contain("claude");
        output.ToString().Should().Contain("copilot");
    }

    [Fact]
    public async Task RunAsyncUsageShowsProviderSnapshots()
    {
        using var output = new StringWriter();

        var exitCode = await CliApplication.RunAsync(
            ["usage"],
            output,
            CancellationToken.None,
            () =>
            [
                new StubUsageProvider(ProviderId.Codex),
                new StubUsageProvider(ProviderId.Claude),
                new StubUsageProvider(ProviderId.Copilot)
            ]);

        exitCode.Should().Be(0);
        output.ToString().Should().Contain("TokenBar usage");
        output.ToString().Should().Contain("codex");
        output.ToString().Should().Contain("claude");
        output.ToString().Should().Contain("copilot");
        output.ToString().Should().Contain("source=Api");
        output.ToString().Should().Contain("primary=");
    }

    private sealed class StubUsageProvider(string providerId) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = new(
            providerId,
            providerId,
            "#000000",
            DefaultEnabled: true,
            [ProviderSourceMode.Auto, ProviderSourceMode.Api]);

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UsageSnapshot(
                providerId,
                UsageWindow.FromUsedAndLimit("Today", 42, 0, null),
                UsageWindow.FromUsedAndLimit("7 days", 100, 0, null),
                "Api",
                UsageStatus.Available,
                DateTimeOffset.UnixEpoch,
                Message: "test"));
        }
    }
}
