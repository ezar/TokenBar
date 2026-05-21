using System.Globalization;
using System.Net.Http.Headers;
using System.Web;

namespace TokenBar.Core.Providers.OpenAI;

public sealed class OpenAIApiClient(HttpClient httpClient) : IOpenAIApiClient
{
    public async Task<string> GetCompletionsUsageJsonAsync(
        string token,
        DateTimeOffset start,
        DateTimeOffset end,
        CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Get, BuildUsageUri(start, end));
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        using var response = await httpClient.SendAsync(request, cancellationToken);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync(cancellationToken);
    }

    private static Uri BuildUsageUri(DateTimeOffset start, DateTimeOffset end)
    {
        var builder = new UriBuilder("https://api.openai.com/v1/organization/usage/completions");
        var query = HttpUtility.ParseQueryString(string.Empty);
        query["start_time"] = start.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        query["end_time"] = end.ToUnixTimeSeconds().ToString(CultureInfo.InvariantCulture);
        query["bucket_width"] = "1d";
        builder.Query = query.ToString();
        return builder.Uri;
    }
}
