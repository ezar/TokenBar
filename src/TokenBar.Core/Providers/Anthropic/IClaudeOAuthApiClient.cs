namespace TokenBar.Core.Providers.Anthropic;

public interface IClaudeOAuthApiClient
{
    Task<string> GetUsageJsonAsync(string accessToken, CancellationToken cancellationToken);
}
