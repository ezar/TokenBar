namespace TokenBar.Core.Providers.Copilot;

public sealed class CopilotApiClient(HttpClient httpClient) : ICopilotApiClient
{
    public async Task<string> GetUserUsageJsonAsync(
        string token,
        string? enterpriseHost,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(
            HttpMethod.Get,
            BuildUsageUri(enterpriseHost));

        request.Headers.TryAddWithoutValidation("Authorization", $"token {token}");
        request.Headers.TryAddWithoutValidation("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("Editor-Version", "vscode/1.96.2");
        request.Headers.TryAddWithoutValidation("Editor-Plugin-Version", "copilot-chat/0.26.7");
        request.Headers.TryAddWithoutValidation("User-Agent", "GitHubCopilotChat/0.26.7");
        request.Headers.TryAddWithoutValidation("X-Github-Api-Version", "2025-04-01");

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Uri BuildUsageUri(string? enterpriseHost)
    {
        var host = NormalizeHost(enterpriseHost);
        var apiHost = host == "github.com" || host == "api.github.com"
            ? "api.github.com"
            : host.StartsWith("api.", StringComparison.OrdinalIgnoreCase) ? host : $"api.{host}";

        return new Uri($"https://{apiHost}/copilot_internal/user");
    }

    private static string NormalizeHost(string? raw)
    {
        if (string.IsNullOrWhiteSpace(raw))
        {
            return "github.com";
        }

        var value = raw.Trim();
        if (Uri.TryCreate(value.Contains("://") ? value : $"https://{value}", UriKind.Absolute, out var uri) &&
            !string.IsNullOrWhiteSpace(uri.Host))
        {
            return uri.IsDefaultPort ? uri.Host.ToLowerInvariant() : $"{uri.Host}:{uri.Port}".ToLowerInvariant();
        }

        return value
            .Replace("https://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace("http://", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Split('/')[0]
            .Trim('.')
            .ToLowerInvariant();
    }
}
