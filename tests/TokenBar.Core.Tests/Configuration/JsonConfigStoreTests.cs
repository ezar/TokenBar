using FluentAssertions;
using TokenBar.Core.Configuration;
using TokenBar.Core.Platform;
using TokenBar.Core.Providers;

namespace TokenBar.Core.Tests.Configuration;

public sealed class JsonConfigStoreTests
{
    [Fact]
    public async Task LoadAsyncReturnsDefaultConfigWhenFileDoesNotExist()
    {
        using var temp = new TempDirectory();
        var store = new JsonConfigStore(new StubPathProvider(temp.Path));

        var config = await store.LoadAsync(CancellationToken.None);

        config.RefreshInterval.Should().Be(TimeSpan.FromMinutes(5));
        config.Providers.Select(provider => provider.ProviderId)
            .Should().Equal(
                ProviderId.Codex,
                ProviderId.Claude,
                ProviderId.Copilot,
                ProviderId.OpenAIApi,
                ProviderId.AnthropicApi);
    }

    [Fact]
    public async Task SaveAsyncPersistsConfig()
    {
        using var temp = new TempDirectory();
        var store = new JsonConfigStore(new StubPathProvider(temp.Path));
        var config = TokenBarConfig.CreateDefault() with
        {
            RefreshInterval = TimeSpan.FromMinutes(1)
        };

        await store.SaveAsync(config, CancellationToken.None);
        var loaded = await store.LoadAsync(CancellationToken.None);

        loaded.RefreshInterval.Should().Be(TimeSpan.FromMinutes(1));
    }

    private sealed class StubPathProvider(string path) : IAppDataPathProvider
    {
        public string AppDataDirectory => path;
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
