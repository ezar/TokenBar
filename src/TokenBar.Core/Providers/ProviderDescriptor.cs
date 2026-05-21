namespace TokenBar.Core.Providers;

public sealed record ProviderDescriptor(
    string ProviderId,
    string DisplayName,
    string BrandColor,
    bool DefaultEnabled,
    IReadOnlyList<ProviderSourceMode> SupportedSourceModes);
