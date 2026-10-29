using ClaudeQueue.Processes;

namespace ClaudeQueue.Tests.Processes;

public class ProcessRunnerTests
{
    [Fact] public async Task Run_captures_exit_and_output()
    {
        var r = await ProcessRunner.RunAsync("pwsh", ["-NoProfile", "-Command", "Write-Output hi; exit 3"], Path.GetTempPath(), default);
        Assert.Equal(3, r.ExitCode); Assert.Equal("hi", r.StdOut.Trim());
    }

    [Fact] public async Task Stream_delivers_lines_as_they_arrive()
    {
        var lines = new List<string>();
        var code = await ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", "'a'; 'b'"], Path.GetTempPath(), lines.Add, TimeSpan.FromSeconds(30), default);
        Assert.Equal(0, code); Assert.Equal(["a", "b"], lines);
    }

    [Fact] public async Task Stream_kills_process_on_timeout() =>
        await Assert.ThrowsAsync<TimeoutException>(() => ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", "Start-Sleep 60"], Path.GetTempPath(), _ => { }, TimeSpan.FromSeconds(2), default));

    [Fact] public async Task Stream_kills_process_on_cancellation()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", "Start-Sleep 60"], Path.GetTempPath(), _ => { }, TimeSpan.FromSeconds(30), cts.Token));
    }

    [Fact] public async Task Stream_returns_when_process_exits_even_if_grandchild_holds_stdout()
    {
        var lines = new List<string>();
        const string script = "Start-Process pwsh -ArgumentList '-NoProfile','-Command','Start-Sleep 30' -NoNewWindow | Out-Null; 'done'; exit 0";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var code = await ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", script], Path.GetTempPath(), lines.Add, TimeSpan.FromSeconds(20), default);
        Assert.Equal(0, code); Assert.Contains("done", lines); Assert.True(sw.Elapsed < TimeSpan.FromSeconds(10), sw.Elapsed.ToString());
    }

    [Fact] public async Task Stream_applies_environment()
    {
        var lines = new List<string>();
        await ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", "$env:CQ_TEST_VAR"], Path.GetTempPath(), lines.Add, TimeSpan.FromSeconds(30), default,
            new Dictionary<string, string> { ["CQ_TEST_VAR"] = "xyz" });
        Assert.Equal(["xyz"], lines);
    }
}
