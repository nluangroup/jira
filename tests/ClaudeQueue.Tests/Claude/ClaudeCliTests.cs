using ClaudeQueue.Claude;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Tests.Claude;

public class ClaudeCliTests
{
    private static readonly string Script = Path.Combine(AppContext.BaseDirectory, "Scripts", "fake-claude.ps1");

    private static ClaudeCli Cli(string mode, TimeSpan? timeout = null) =>
        new(Options.Create(new QueueOptions
        {
            ClaudeExecutable = "pwsh",
            ClaudeExecutableArgs = ["-NoProfile", "-File", Script],
            ClaudeTimeout = timeout ?? TimeSpan.FromSeconds(60),
        }), NullLogger<ClaudeCli>.Instance)
        { ExtraEnvironment = new Dictionary<string, string> { ["FAKE_CLAUDE_MODE"] = mode } };

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "cq-claude-" + Guid.NewGuid().ToString("N"), "work");
        Directory.CreateDirectory(dir);
        return dir;
    }

    [Fact] public void Args_match_spec() =>
        Assert.Equal(["-p", "PROMPT", "--output-format", "stream-json", "--verbose", "--permission-mode", "dontAsk",
                      "--max-budget-usd", "5", "--disallowedTools", "Bash(git push:*)"],
            ClaudeCli.BuildArgs("PROMPT", 5m));

    [Fact] public async Task Success_stream_yields_events_cost_and_final_message()
    {
        var events = new List<StreamEvent>();
        var r = await Cli("success").RunAsync(TempDir(), "p", events.Add, default);
        Assert.Equal(ClaudeExit.Succeeded, r.Exit); Assert.Equal(0.42m, r.CostUsd); Assert.Equal("All done", r.FinalMessage);
        Assert.Contains(new SubagentStarted("Implement Task 1: Health endpoint"), events);
    }

    [Fact] public async Task Error_result_is_Failed() =>
        Assert.Equal(ClaudeExit.Failed, (await Cli("error").RunAsync(TempDir(), "p", _ => { }, default)).Exit);

    [Fact] public async Task Hang_past_timeout_is_TimedOut() =>
        Assert.Equal(ClaudeExit.TimedOut, (await Cli("hang", TimeSpan.FromSeconds(2)).RunAsync(TempDir(), "p", _ => { }, default)).Exit);

    [Fact] public async Task Raw_lines_are_saved_next_to_workdir()
    {
        var dir = TempDir();
        await Cli("success").RunAsync(dir, "p", _ => { }, default);
        var log = Path.Combine(Path.GetDirectoryName(dir)!, "work-claude.jsonl");
        Assert.Contains("All done", await File.ReadAllTextAsync(log));
    }

    [Fact] public async Task Throwing_onEvent_does_not_stop_the_run()
    {
        var r = await Cli("success").RunAsync(TempDir(), "p", _ => throw new InvalidOperationException("boom"), default);
        Assert.Equal(ClaudeExit.Succeeded, r.Exit);
    }
}
