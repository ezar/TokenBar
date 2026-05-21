using System.Text.Json;
using TokenBar.Core.Platform;

namespace TokenBar.Core.Configuration;

public sealed class JsonConfigStore(IAppDataPathProvider pathProvider) : IConfigStore
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true
    };

    private string ConfigPath => Path.Combine(pathProvider.AppDataDirectory, "config.json");

    public async Task<TokenBarConfig> LoadAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(ConfigPath))
        {
            return TokenBarConfig.CreateDefault();
        }

        await using var stream = File.OpenRead(ConfigPath);
        var config = await JsonSerializer.DeserializeAsync<TokenBarConfig>(
            stream,
            SerializerOptions,
            cancellationToken);

        return config ?? TokenBarConfig.CreateDefault();
    }

    public async Task SaveAsync(TokenBarConfig config, CancellationToken cancellationToken)
    {
        Directory.CreateDirectory(pathProvider.AppDataDirectory);
        await using var stream = File.Create(ConfigPath);
        await JsonSerializer.SerializeAsync(stream, config, SerializerOptions, cancellationToken);
    }
}
