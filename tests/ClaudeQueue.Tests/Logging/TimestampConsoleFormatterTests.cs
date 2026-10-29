using ClaudeQueue.Logging;
using Microsoft.Extensions.Logging;

namespace ClaudeQueue.Tests.Logging;

public class TimestampConsoleFormatterTests
{
    [Fact]
    public void Formats_timestamp_level_and_message()
    {
        var line = TimestampConsoleFormatter.FormatLine(
            new DateTime(2026, 10, 29, 21, 5, 9), LogLevel.Warning, "Card SCRUM-7 moved");
        Assert.Equal("[2026-10-29 21:05:09] WARNING: Card SCRUM-7 moved", line);
    }

    [Theory]
    [InlineData(LogLevel.Information, "INFO")]
    [InlineData(LogLevel.Error, "ERROR")]
    [InlineData(LogLevel.Debug, "DEBUG")]
    public void Uses_short_uppercase_level_names(LogLevel level, string expected) =>
        Assert.Contains($"] {expected}: ", TimestampConsoleFormatter.FormatLine(DateTime.Now, level, "x"));
}
