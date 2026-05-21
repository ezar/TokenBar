using FluentAssertions;
using TokenBar.Core.Providers.OpenAI;

namespace TokenBar.Core.Tests.Providers.OpenAI;

public sealed class CodexOAuthCredentialStoreTests
{
    [Fact]
    public void TryReadCredentialsReadsCodexAuthJson()
    {
        using var temp = new TempDirectory();
        var authFile = Path.Combine(temp.Path, "auth.json");
        File.WriteAllText(
            authFile,
            """
            {
              "tokens": {
                "access_token": "codex-access-token"
              },
              "account_id": "account-123"
            }
            """);

        var credentials = CodexOAuthCredentialStore.TryReadCredentials(authFile);

        credentials.Should().NotBeNull();
        credentials!.AccessToken.Should().Be("codex-access-token");
        credentials.AccountId.Should().Be("account-123");
    }

    [Fact]
    public void TryReadCredentialsReadsNestedAccountId()
    {
        using var temp = new TempDirectory();
        var authFile = Path.Combine(temp.Path, "auth.json");
        File.WriteAllText(
            authFile,
            """
            {
              "tokens": {
                "access_token": "codex-access-token",
                "account_id": "nested-account"
              }
            }
            """);

        var credentials = CodexOAuthCredentialStore.TryReadCredentials(authFile);

        credentials.Should().NotBeNull();
        credentials!.AccountId.Should().Be("nested-account");
    }

    private sealed class TempDirectory : IDisposable
    {
        public TempDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
