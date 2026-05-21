using FluentAssertions;
using TokenBar.Core.Configuration;

namespace TokenBar.Core.Tests.Configuration;

public sealed class AppSettingsApiKeyStoreTests
{
    [Fact]
    public void LoadFromDirectoryReadsTokenBarApiKeysSection()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            Path.Combine(temp.Path, "appsettings.local.json"),
            """
            {
              "TokenBar": {
                "ApiKeys": {
                  "OpenAIAdminKey": "openai-local",
                  "AnthropicAdminKey": "anthropic-local",
                  "GitHubCopilotToken": "copilot-local"
                }
              }
            }
            """);

        var keys = AppSettingsApiKeyStore.LoadFromDirectory(temp.Path);

        keys.OpenAIAdminKey.Should().Be("openai-local");
        keys.AnthropicAdminKey.Should().Be("anthropic-local");
        keys.GitHubCopilotToken.Should().Be("copilot-local");
    }

    [Fact]
    public void LoadFromDirectorySupportsExactEnvironmentVariableNames()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            Path.Combine(temp.Path, "appsettings.json"),
            """
            {
              "OPENAI_ADMIN_KEY": "openai-root",
              "ANTHROPIC_ADMIN_KEY": "anthropic-root",
              "GITHUB_COPILOT_TOKEN": "copilot-root"
            }
            """);

        var keys = AppSettingsApiKeyStore.LoadFromDirectory(temp.Path);

        keys.OpenAIAdminKey.Should().Be("openai-root");
        keys.AnthropicAdminKey.Should().Be("anthropic-root");
        keys.GitHubCopilotToken.Should().Be("copilot-root");
    }

    [Fact]
    public void LocalSettingsOverrideBaseSettings()
    {
        using var temp = new TempDirectory();
        File.WriteAllText(
            Path.Combine(temp.Path, "appsettings.json"),
            """{ "TokenBar": { "ApiKeys": { "OpenAIAdminKey": "openai-base" } } }""");
        File.WriteAllText(
            Path.Combine(temp.Path, "appsettings.local.json"),
            """{ "TokenBar": { "ApiKeys": { "OpenAIAdminKey": "openai-local" } } }""");

        var keys = AppSettingsApiKeyStore.LoadFromDirectory(temp.Path);

        keys.OpenAIAdminKey.Should().Be("openai-local");
    }

    [Fact]
    public void LoadFromDirectoriesSearchesParentDirectories()
    {
        using var temp = new TempDirectory();
        var child = Path.Combine(temp.Path, "src", "TokenBar.App", "bin", "Debug", "net10.0");
        Directory.CreateDirectory(child);
        File.WriteAllText(
            Path.Combine(temp.Path, "appsettings.local.json"),
            """{ "TokenBar": { "ApiKeys": { "AnthropicAdminKey": "anthropic-root" } } }""");

        var keys = AppSettingsApiKeyStore.LoadFromDirectories([child]);

        keys.AnthropicAdminKey.Should().Be("anthropic-root");
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
