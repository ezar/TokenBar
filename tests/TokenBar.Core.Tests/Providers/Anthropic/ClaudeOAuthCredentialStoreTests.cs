using FluentAssertions;
using TokenBar.Core.Providers.Anthropic;

namespace TokenBar.Core.Tests.Providers.Anthropic;

public sealed class ClaudeOAuthCredentialStoreTests
{
    [Fact]
    public void TryReadAccessTokenReadsNestedClaudeCodeCredentialFile()
    {
        using var temp = new TempDirectory();
        var credentialsFile = Path.Combine(temp.Path, ".credentials.json");
        File.WriteAllText(
            credentialsFile,
            """
            {
              "claudeAiOauth": {
                "accessToken": "sk-ant-oat01-nested",
                "refreshToken": "sk-ant-ort01-nested",
                "scopes": ["user:inference", "user:profile"]
              }
            }
            """);

        var token = ClaudeOAuthCredentialStore.TryReadAccessToken(credentialsFile);

        token.Should().Be("sk-ant-oat01-nested");
    }

    [Fact]
    public void TryReadAccessTokenReadsLegacyFlatCredentialFile()
    {
        using var temp = new TempDirectory();
        var credentialsFile = Path.Combine(temp.Path, ".credentials.json");
        File.WriteAllText(
            credentialsFile,
            """{ "accessToken": "sk-ant-oat01-flat" }""");

        var token = ClaudeOAuthCredentialStore.TryReadAccessToken(credentialsFile);

        token.Should().Be("sk-ant-oat01-flat");
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
