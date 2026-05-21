namespace TokenBar.Core.Configuration;

public sealed record AppSettingsApiKeys(
    string? OpenAIAdminKey,
    string? AnthropicAdminKey,
    string? GitHubCopilotToken,
    string? CodexAccessToken,
    string? CodexAccountId,
    string? ClaudeCodeOAuthToken);
