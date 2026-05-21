using TokenBar.Core.Configuration;
using TokenBar.Core.Providers;
using TokenBar.Core.Providers.BuiltIn;
using TokenBar.Core.Refresh;
using TokenBar.Core.Usage;

namespace TokenBar.Cli;

public static class CliApplication
{
    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        CancellationToken cancellationToken)
    {
        return await RunAsync(
            args,
            output,
            cancellationToken,
            () => BuiltInProviderFactory.CreateProviders());
    }

    public static async Task<int> RunAsync(
        IReadOnlyList<string> args,
        TextWriter output,
        CancellationToken cancellationToken,
        Func<IReadOnlyList<IUsageProvider>> providerFactory)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (args.Count == 0 || IsHelp(args[0]))
        {
            await WriteHelpAsync(output);
            return 0;
        }

        return args[0].ToLowerInvariant() switch
        {
            "status" => await WriteStatusAsync(output),
            "usage" => await WriteUsageAsync(output, cancellationToken, providerFactory),
            _ => await WriteUnknownCommandAsync(output, args[0])
        };
    }

    private static async Task<int> WriteStatusAsync(TextWriter output)
    {
        var config = TokenBarConfig.CreateDefault();

        await output.WriteLineAsync("TokenBar status");
        await output.WriteLineAsync($"Refresh interval: {config.RefreshInterval}");
        await output.WriteLineAsync("Providers:");

        foreach (var provider in config.Providers)
        {
            var state = provider.Enabled ? "enabled" : "disabled";
            await output.WriteLineAsync($"- {provider.ProviderId}: {state}, source={provider.SourceMode}");
        }

        return 0;
    }

    private static async Task<int> WriteUsageAsync(
        TextWriter output,
        CancellationToken cancellationToken,
        Func<IReadOnlyList<IUsageProvider>> providerFactory)
    {
        var config = TokenBarConfig.CreateDefault();
        var registry = new ProviderRegistry(providerFactory());
        var refreshService = new RefreshService(registry);
        var result = await refreshService.RefreshOnceAsync(config, cancellationToken);

        await output.WriteLineAsync("TokenBar usage");

        foreach (var snapshot in result.Snapshots)
        {
            await output.WriteLineAsync(FormatSnapshot(snapshot));
        }

        return 0;
    }

    private static string FormatSnapshot(UsageSnapshot snapshot)
    {
        var status = snapshot.Status.ToString().ToLowerInvariant();
        var message = string.IsNullOrWhiteSpace(snapshot.Message)
            ? string.Empty
            : $" - {snapshot.Message}";

        var secondary = snapshot.SecondaryWindow is null
            ? string.Empty
            : $", secondary={FormatUsageWindow(snapshot.SecondaryWindow)}";

        return $"- {snapshot.ProviderId}: {status}, source={snapshot.Source}, primary={FormatUsageWindow(snapshot.PrimaryWindow)}{secondary}{message}";
    }

    private static string FormatUsageWindow(UsageWindow window)
    {
        if (window.PercentUsed is not null)
        {
            var reset = window.ResetAt is null
                ? string.Empty
                : $" resets={window.ResetAt.Value.ToLocalTime():MMM d HH:mm}";
            return $"{window.Label}: {window.PercentUsed:N0}% used{reset}";
        }

        return window.Used is null
            ? window.Label
            : $"{window.Label}: {window.Used:N0} tokens";
    }

    private static async Task<int> WriteUnknownCommandAsync(TextWriter output, string command)
    {
        await output.WriteLineAsync($"Unknown command: {command}");
        await WriteHelpAsync(output);
        return 1;
    }

    private static async Task WriteHelpAsync(TextWriter output)
    {
        await output.WriteLineAsync("TokenBar");
        await output.WriteLineAsync();
        await output.WriteLineAsync("Commands:");
        await output.WriteLineAsync("  status   Show local TokenBar configuration status");
        await output.WriteLineAsync("  usage    Show provider usage summary");
    }

    private static bool IsHelp(string arg)
    {
        return arg is "-h" or "--help" or "help";
    }
}
