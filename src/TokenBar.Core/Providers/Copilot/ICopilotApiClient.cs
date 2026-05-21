namespace TokenBar.Core.Providers.Copilot;

public interface ICopilotApiClient
{
    Task<string> GetUserUsageJsonAsync(
        string token,
        string? enterpriseHost,
        CancellationToken cancellationToken);
}
