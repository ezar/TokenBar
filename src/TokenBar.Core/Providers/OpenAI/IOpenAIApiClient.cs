namespace TokenBar.Core.Providers.OpenAI;

public interface IOpenAIApiClient
{
    Task<string> GetCompletionsUsageJsonAsync(
        string token,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);
}
