namespace TokenBar.Core.Configuration;

public interface IConfigStore
{
    Task<TokenBarConfig> LoadAsync(CancellationToken cancellationToken);

    Task SaveAsync(TokenBarConfig config, CancellationToken cancellationToken);
}
