using System.Text.RegularExpressions;

namespace TokenBar.Core.Privacy;

public static partial class Redactor
{
    public static string Redact(string input)
    {
        var output = BearerTokenRegex().Replace(input, "Authorization: Bearer [redacted]");
        output = ApiKeyRegex().Replace(output, "$1=[redacted]");
        output = EmailRegex().Replace(output, "[redacted-email]");
        return output;
    }

    [GeneratedRegex("Authorization:\\s*Bearer\\s+[^\\r\\n]+", RegexOptions.IgnoreCase)]
    private static partial Regex BearerTokenRegex();

    [GeneratedRegex("(api[_-]?key)\\s*=\\s*[^\\s&]+", RegexOptions.IgnoreCase)]
    private static partial Regex ApiKeyRegex();

    [GeneratedRegex("[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}", RegexOptions.IgnoreCase)]
    private static partial Regex EmailRegex();
}
