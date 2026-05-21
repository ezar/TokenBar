using FluentAssertions;
using TokenBar.Core.Providers;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Providers;

public sealed class ProviderRegistryTests
{
    [Fact]
    public void ConstructorRejectsDuplicateProviderIds()
    {
        var descriptor = new ProviderDescriptor(
            ProviderId: "codex",
            DisplayName: "Codex",
            BrandColor: "#111111",
            DefaultEnabled: true,
            SupportedSourceModes: [ProviderSourceMode.Auto]);

        var providers = new IUsageProvider[]
        {
            new StubProvider(descriptor),
            new StubProvider(descriptor)
        };

        var action = () => new ProviderRegistry(providers);

        action.Should().Throw<InvalidOperationException>()
            .WithMessage("*Duplicate provider id 'codex'*");
    }

    [Fact]
    public void GetEnabledProvidersReturnsConfigOrder()
    {
        var codex = new StubProvider(new ProviderDescriptor("codex", "Codex", "#111111", true, [ProviderSourceMode.Auto]));
        var claude = new StubProvider(new ProviderDescriptor("claude", "Claude", "#DA7756", true, [ProviderSourceMode.Auto]));
        var registry = new ProviderRegistry([codex, claude]);

        var enabled = registry.GetEnabledProviders(["claude", "codex"]);

        enabled.Select(provider => provider.Descriptor.ProviderId)
            .Should().Equal("claude", "codex");
    }

    private sealed class StubProvider(ProviderDescriptor descriptor) : IUsageProvider
    {
        public ProviderDescriptor Descriptor { get; } = descriptor;

        public Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken)
        {
            return Task.FromResult(new UsageSnapshot(
                Descriptor.ProviderId,
                UsageWindow.Unknown("Session"),
                null,
                "stub",
                UsageStatus.Unknown,
                DateTimeOffset.UnixEpoch));
        }
    }
}
