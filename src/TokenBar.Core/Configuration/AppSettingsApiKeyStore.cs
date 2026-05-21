using System.Text.Json;

namespace TokenBar.Core.Configuration;

public static class AppSettingsApiKeyStore
{
    private static readonly string[] SettingsFiles = ["appsettings.json", "appsettings.local.json"];

    public static AppSettingsApiKeys LoadDefault()
    {
        return LoadFromDirectories([AppContext.BaseDirectory, Directory.GetCurrentDirectory()]);
    }

    public static AppSettingsApiKeys LoadFromDirectories(IEnumerable<string> directories)
    {
        var keys = new AppSettingsApiKeys(null, null, null, null, null, null);

        foreach (var directory in GetCandidateDirectories(directories))
        {
            keys = Merge(keys, LoadFromDirectory(directory));
        }

        return keys;
    }

    public static AppSettingsApiKeys LoadFromDirectory(string directory)
    {
        var keys = new AppSettingsApiKeys(null, null, null, null, null, null);

        foreach (var fileName in SettingsFiles)
        {
            var file = Path.Combine(directory, fileName);
            if (!File.Exists(file))
            {
                continue;
            }

            keys = Merge(keys, LoadFromFile(file));
        }

        return keys;
    }

    private static AppSettingsApiKeys LoadFromFile(string file)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(file));
        var root = document.RootElement;
        var tokenBar = TryGetObject(root, "TokenBar");
        var apiKeys = tokenBar is null ? TryGetObject(root, "ApiKeys") : TryGetObject(tokenBar.Value, "ApiKeys");
        var source = apiKeys ?? tokenBar ?? root;

        return new AppSettingsApiKeys(
            FindString(source, "OPENAI_ADMIN_KEY", "OpenAIAdminKey", "OpenAIApiKey", "OpenAI"),
            FindString(source, "ANTHROPIC_ADMIN_KEY", "AnthropicAdminKey", "Anthropic"),
            FindString(source, "GITHUB_COPILOT_TOKEN", "GitHubCopilotToken", "CopilotToken", "Copilot"),
            FindString(source, "CODEX_ACCESS_TOKEN", "CodexAccessToken"),
            FindString(source, "CODEX_ACCOUNT_ID", "CodexAccountId"),
            FindString(source, "CLAUDE_CODE_OAUTH_TOKEN", "ClaudeCodeOAuthToken", "ClaudeOAuthToken"));
    }

    private static AppSettingsApiKeys Merge(AppSettingsApiKeys baseKeys, AppSettingsApiKeys overrideKeys)
    {
        return new AppSettingsApiKeys(
            FirstNotBlank(overrideKeys.OpenAIAdminKey, baseKeys.OpenAIAdminKey),
            FirstNotBlank(overrideKeys.AnthropicAdminKey, baseKeys.AnthropicAdminKey),
            FirstNotBlank(overrideKeys.GitHubCopilotToken, baseKeys.GitHubCopilotToken),
            FirstNotBlank(overrideKeys.CodexAccessToken, baseKeys.CodexAccessToken),
            FirstNotBlank(overrideKeys.CodexAccountId, baseKeys.CodexAccountId),
            FirstNotBlank(overrideKeys.ClaudeCodeOAuthToken, baseKeys.ClaudeCodeOAuthToken));
    }

    private static IReadOnlyList<string> GetCandidateDirectories(IEnumerable<string> directories)
    {
        return directories
            .Where(directory => !string.IsNullOrWhiteSpace(directory))
            .SelectMany(EnumerateDirectoryAndParents)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Reverse()
            .ToList();
    }

    private static IEnumerable<string> EnumerateDirectoryAndParents(string directory)
    {
        var current = new DirectoryInfo(directory);
        while (current is not null)
        {
            yield return current.FullName;
            current = current.Parent;
        }
    }

    private static JsonElement? TryGetObject(JsonElement source, string propertyName)
    {
        return source.TryGetProperty(propertyName, out var value) && value.ValueKind == JsonValueKind.Object
            ? value
            : null;
    }

    private static string? FindString(JsonElement source, params string[] propertyNames)
    {
        foreach (var propertyName in propertyNames)
        {
            if (source.TryGetProperty(propertyName, out var value) &&
                value.ValueKind == JsonValueKind.String &&
                !string.IsNullOrWhiteSpace(value.GetString()))
            {
                return value.GetString();
            }
        }

        return null;
    }

    private static string? FirstNotBlank(string? primary, string? secondary)
    {
        return string.IsNullOrWhiteSpace(primary) ? secondary : primary;
    }
}
