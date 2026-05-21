namespace TokenBar.Core.Providers.OpenAI;

public interface ICodexOAuthApiClient
{
    Task<string> GetUsageJsonAsync(
        string accessToken,
        string? accountId,
        CancellationToken cancellationToken);
}
