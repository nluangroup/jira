using System.Diagnostics;
using ClaudeQueue.Runs;

namespace ClaudeQueue.Tests.Runs;

public class KeepAwakeTests
{
    [Fact] public void KeepAwake_acquire_and_dispose_do_not_throw()
    {
        var awake = KeepAwake.Acquire();
        var sw = Stopwatch.StartNew();
        awake.Dispose();
        if (OperatingSystem.IsWindows()) Assert.True(sw.Elapsed < TimeSpan.FromSeconds(2));
    }
}
