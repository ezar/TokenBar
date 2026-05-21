namespace TokenBar.Core.Platform;

public sealed record ProcessRunResult(
    int? ExitCode,
    string StandardOutput,
    string StandardError,
    bool TimedOut);
