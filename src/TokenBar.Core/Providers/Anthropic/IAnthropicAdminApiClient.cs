namespace TokenBar.Core.Providers.Anthropic;

public interface IAnthropicAdminApiClient
{
    Task<string> GetMessagesUsageReportJsonAsync(
        string adminKey,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken);
}
