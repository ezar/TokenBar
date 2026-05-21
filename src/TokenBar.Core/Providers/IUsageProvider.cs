using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers;

public interface IUsageProvider
{
    ProviderDescriptor Descriptor { get; }

    Task<UsageSnapshot> FetchAsync(ProviderSourceMode sourceMode, CancellationToken cancellationToken);
}
