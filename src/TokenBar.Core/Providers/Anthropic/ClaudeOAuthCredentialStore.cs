using System.Text.Json;

namespace TokenBar.Core.Providers.Anthropic;

public static class ClaudeOAuthCredentialStore
{
    public static string? TryReadDefaultAccessToken()
    {
        foreach (var file in GetDefaultCredentialFiles())
        {
            var token = TryReadAccessToken(file);
            if (!string.IsNullOrWhiteSpace(token))
            {
                return token;
            }
        }

        return null;
    }

    public static string? TryReadAccessToken(string credentialsFile)
    {
        if (!File.Exists(credentialsFile))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(credentialsFile));
            var root = document.RootElement;
            var rootToken = FindString(root, "accessToken", "access_token");
            if (!string.IsNullOrWhiteSpace(rootToken))
            {
                return rootToken;
            }

            var claudeAiOauth = TryGetObject(root, "claudeAiOauth");
            return claudeAiOauth is null
                ? null
                : FindString(claudeAiOauth.Value, "accessToken", "access_token");
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static IEnumerable<string> GetDefaultCredentialFiles()
    {
        var explicitConfigDir = Environment.GetEnvironmentVariable("CLAUDE_CONFIG_DIR");
        if (!string.IsNullOrWhiteSpace(explicitConfigDir))
        {
            yield return Path.Combine(explicitConfigDir, ".credentials.json");
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            yield return Path.Combine(userProfile, ".claude", ".credentials.json");
            yield return Path.Combine(userProfile, ".config", "claude", ".credentials.json");
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
}
