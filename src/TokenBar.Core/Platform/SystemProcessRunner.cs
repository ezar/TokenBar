using System.Diagnostics;

namespace TokenBar.Core.Platform;

public sealed class SystemProcessRunner : IProcessRunner
{
    public async Task<ProcessRunResult> RunAsync(ProcessRunRequest request, CancellationToken cancellationToken)
    {
        using var timeout = new CancellationTokenSource(request.Timeout);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeout.Token);

        using var process = new Process();
        process.StartInfo.FileName = request.FileName;
        process.StartInfo.RedirectStandardOutput = true;
        process.StartInfo.RedirectStandardError = true;
        process.StartInfo.UseShellExecute = false;
        process.StartInfo.CreateNoWindow = true;

        if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
        {
            process.StartInfo.WorkingDirectory = request.WorkingDirectory;
        }

        foreach (var argument in request.Arguments)
        {
            process.StartInfo.ArgumentList.Add(argument);
        }

        process.Start();
        var stdoutTask = process.StandardOutput.ReadToEndAsync(linked.Token);
        var stderrTask = process.StandardError.ReadToEndAsync(linked.Token);

        try
        {
            await process.WaitForExitAsync(linked.Token);
            var stdout = await stdoutTask;
            var stderr = await stderrTask;
            return new ProcessRunResult(process.ExitCode, stdout, stderr, TimedOut: false);
        }
        catch (OperationCanceledException) when (timeout.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            TryKill(process);
            return new ProcessRunResult(null, string.Empty, string.Empty, TimedOut: true);
        }
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
            {
                process.Kill(entireProcessTree: true);
            }
        }
        catch (InvalidOperationException)
        {
        }
    }
}
