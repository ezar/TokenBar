namespace TokenBar.Core.Platform;

public sealed record ProcessRunRequest(
    string FileName,
    IReadOnlyList<string> Arguments,
    TimeSpan Timeout,
    string? WorkingDirectory = null);
