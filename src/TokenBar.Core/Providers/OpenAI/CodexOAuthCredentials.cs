namespace TokenBar.Core.Providers.OpenAI;

public sealed record CodexOAuthCredentials(
    string AccessToken,
    string? AccountId);
