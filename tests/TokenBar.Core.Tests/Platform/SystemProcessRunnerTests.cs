using FluentAssertions;
using TokenBar.Core.Platform;

namespace TokenBar.Core.Tests.Platform;

public sealed class SystemProcessRunnerTests
{
    [Fact]
    public async Task RunAsyncCapturesStdout()
    {
        var runner = new SystemProcessRunner();
        var request = OperatingSystem.IsWindows()
            ? new ProcessRunRequest("cmd.exe", ["/c", "echo tokenbar"], TimeSpan.FromSeconds(5))
            : new ProcessRunRequest("/bin/sh", ["-c", "printf tokenbar"], TimeSpan.FromSeconds(5));

        var result = await runner.RunAsync(request, CancellationToken.None);

        result.ExitCode.Should().Be(0);
        result.StandardOutput.Trim().Should().Be("tokenbar");
        result.TimedOut.Should().BeFalse();
    }

    [Fact]
    public async Task RunAsyncMarksTimeout()
    {
        var runner = new SystemProcessRunner();
        var request = OperatingSystem.IsWindows()
            ? new ProcessRunRequest("cmd.exe", ["/c", "ping -n 3 127.0.0.1 > nul"], TimeSpan.FromMilliseconds(50))
            : new ProcessRunRequest("/bin/sh", ["-c", "sleep 2"], TimeSpan.FromMilliseconds(50));

        var result = await runner.RunAsync(request, CancellationToken.None);

        result.TimedOut.Should().BeTrue();
    }
}
