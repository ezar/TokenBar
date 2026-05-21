namespace TokenBar.App.ViewModels;

public sealed record ProviderRowViewModel(
    string ProviderId,
    string DisplayName,
    string BrandColor,
    string Status,
    string StatusColor,
    string Source,
    string PrimaryLabel,
    string PrimaryValue,
    string PrimaryReset,
    decimal PrimaryPercentUsed,
    bool HasPrimaryPercent,
    string SecondaryLabel,
    string SecondaryValue,
    string SecondaryReset,
    decimal SecondaryPercentUsed,
    bool HasSecondaryPercent,
    bool HasSecondary,
    string Message,
    bool HasMessage);
