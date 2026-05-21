using System.Text.Json;

namespace TokenBar.Core.Providers.OpenAI;

public static class CodexOAuthCredentialStore
{
    public static CodexOAuthCredentials? TryReadDefaultCredentials()
    {
        foreach (var file in GetDefaultAuthFiles())
        {
            var credentials = TryReadCredentials(file);
            if (credentials is not null)
            {
                return credentials;
            }
        }

        return null;
    }

    public static CodexOAuthCredentials? TryReadCredentials(string authFile)
    {
        if (!File.Exists(authFile))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(authFile));
            var root = document.RootElement;
            var tokens = TryGetObject(root, "tokens");
            var accessToken = tokens is null
                ? FindString(root, "accessToken", "access_token")
                : FindString(tokens.Value, "accessToken", "access_token");

            if (string.IsNullOrWhiteSpace(accessToken))
            {
                return null;
            }

            var accountId = FindString(root, "account_id", "accountId")
                ?? (tokens is null ? null : FindString(tokens.Value, "account_id", "accountId"));

            return new CodexOAuthCredentials(accessToken, accountId);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static IEnumerable<string> GetDefaultAuthFiles()
    {
        var explicitHome = Environment.GetEnvironmentVariable("CODEX_HOME");
        if (!string.IsNullOrWhiteSpace(explicitHome))
        {
            yield return Path.Combine(explicitHome, "auth.json");
        }

        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            yield return Path.Combine(userProfile, ".codex", "auth.json");
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
