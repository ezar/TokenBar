using System.Globalization;
using System.Net.Http.Headers;
using System.Web;

namespace TokenBar.Core.Providers.Anthropic;

public sealed class AnthropicAdminApiClient(HttpClient httpClient) : IAnthropicAdminApiClient
{
    public async Task<string> GetMessagesUsageReportJsonAsync(
        string adminKey,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUsageUri(start, end));
        request.Headers.TryAddWithoutValidation("x-api-key", adminKey);
        request.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Uri BuildUsageUri(DateTimeOffset start, DateTimeOffset end)
    {
        var builder = new UriBuilder("https://api.anthropic.com/v1/organizations/usage_report/messages");
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["starting_at"] = start.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        query["ending_at"] = end.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
        query["bucket_width"] = "1d";
        builder.Query = query.ToString();
        return builder.Uri;
    }
}
