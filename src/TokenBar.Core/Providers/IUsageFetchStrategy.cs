using TokenBar.Core.Usage;

namespace TokenBar.Core.Providers;

public interface IUsageFetchStrategy
{
    string SourceName { get; }

    ProviderSourceMode SourceMode { get; }

    Task<bool> IsAvailableAsync(CancellationToken cancellationToken);

    Task<UsageSnapshot> FetchAsync(CancellationToken cancellationToken);
}
