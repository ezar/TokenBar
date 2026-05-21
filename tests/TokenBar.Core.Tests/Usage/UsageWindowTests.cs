using FluentAssertions;
using TokenBar.Core.Usage;

namespace TokenBar.Core.Tests.Usage;

public sealed class UsageWindowTests
{
    [Fact]
    public void FromUsedAndLimitCalculatesPercentUsed()
    {
        var resetAt = new DateTimeOffset(2026, 5, 19, 12, 0, 0, TimeSpan.Zero);

        var window = UsageWindow.FromUsedAndLimit("Session", 750, 1000, resetAt);

        window.Label.Should().Be("Session");
        window.Used.Should().Be(750);
        window.Limit.Should().Be(1000);
        window.PercentUsed.Should().Be(75);
        window.PercentRemaining.Should().Be(25);
        window.ResetAt.Should().Be(resetAt);
    }

    [Fact]
    public void UnknownWindowHasNoPercentages()
    {
        var window = UsageWindow.Unknown("Weekly");

        window.Label.Should().Be("Weekly");
        window.Used.Should().BeNull();
        window.Limit.Should().BeNull();
        window.PercentUsed.Should().BeNull();
        window.PercentRemaining.Should().BeNull();
        window.ResetAt.Should().BeNull();
    }
}
