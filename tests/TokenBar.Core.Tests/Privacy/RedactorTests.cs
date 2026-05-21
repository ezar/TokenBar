using FluentAssertions;
using TokenBar.Core.Privacy;

namespace TokenBar.Core.Tests.Privacy;

public sealed class RedactorTests
{
    [Fact]
    public void RedactMasksBearerTokensApiKeysAndEmails()
    {
        const string input = "Authorization: Bearer sk-test-123\napi_key=abc123\nuser=a@example.com";

        var output = Redactor.Redact(input);

        output.Should().NotContain("sk-test-123");
        output.Should().NotContain("abc123");
        output.Should().NotContain("a@example.com");
        output.Should().Contain("Authorization: Bearer [redacted]");
        output.Should().Contain("api_key=[redacted]");
        output.Should().Contain("[redacted-email]");
    }
}
