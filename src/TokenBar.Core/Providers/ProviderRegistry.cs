namespace TokenBar.Core.Providers;

public sealed class ProviderRegistry
{
    private readonly IReadOnlyDictionary<string, IUsageProvider> providersById;

    public ProviderRegistry(IEnumerable<IUsageProvider> providers)
    {
        var providerList = providers.ToList();
        var duplicate = providerList
            .GroupBy(provider => provider.Descriptor.ProviderId, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicate is not null)
        {
            throw new InvalidOperationException($"Duplicate provider id '{duplicate.Key}'.");
        }

        providersById = providerList.ToDictionary(
            provider => provider.Descriptor.ProviderId,
            StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<IUsageProvider> GetEnabledProviders(IReadOnlyList<string> enabledProviderIds)
    {
        return enabledProviderIds
            .Where(providersById.ContainsKey)
            .Select(providerId => providersById[providerId])
            .ToList();
    }
}
