namespace TokenBar.App.ViewModels;

public sealed record ProviderRowViewModel(
    string ProviderId,
    string DisplayName,
    string BrandColor,
    string Status,
    string StatusColor,
    string Source,
    string Primary,
    string Secondary,
    string Message,
    bool HasMessage);
