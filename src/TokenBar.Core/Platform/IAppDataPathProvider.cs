namespace TokenBar.Core.Platform;

public interface IAppDataPathProvider
{
    string AppDataDirectory { get; }
}
