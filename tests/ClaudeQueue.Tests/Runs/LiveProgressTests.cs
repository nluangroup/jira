using ClaudeQueue.Git;
using ClaudeQueue.Runs;
using ClaudeQueue.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;

namespace ClaudeQueue.Tests.Runs;

public sealed class LiveProgressTests : IDisposable
{
    private readonly List<string> dirs = [];

    public void Dispose()
    {
        foreach (var d in dirs) Directory.Delete(d, true);
    }

    private string TempDir()
    {
        var d = Path.Combine(Path.GetTempPath(), "lp-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(d);
        dirs.Add(d);
        return d;
    }

    private static void WriteLedger(string dir, string text, string run = "run1")
    {
        var p = Path.Combine(dir, ".superpowers", "sdd", run);
        Directory.CreateDirectory(p);
        File.WriteAllText(Path.Combine(p, "progress.md"), text);
    }

    private static LiveProgress.Session Start(IWorkspace ws, FakeJira jira, string dir, IReadOnlyList<string>? titles = null) =>
        new LiveProgress(jira, ws, NullLogger<LiveProgress>.Instance).StartSession("SCRUM-1", dir, "plan/x", titles ?? []);

    private static async Task Ticks(LiveProgress.Session s, int n)
    {
        for (var i = 0; i < n; i++) await s.TickAsync(CancellationToken.None);
    }

    [Fact]
    public async Task Pushes_when_head_moves()
    {
        var ws = new FakeWorkspace(["a", "a", "b", "b", "c"]);
        await Ticks(Start(ws, new FakeJira(), TempDir()), 5);
        Assert.Equal(2, ws.Pushes.Count);
    }

    [Fact]
    public async Task Comments_each_completed_task_once()
    {
        var dir = TempDir();
        WriteLedger(dir, "Task 1: complete (commits a..b, review clean)");
        var jira = new FakeJira();
        var s = Start(new FakeWorkspace(), jira, dir, ["Health endpoint", "Health test"]);
        await s.TickAsync(CancellationToken.None);
        WriteLedger(dir, "Task 1: complete (...)\nTask 2: complete (...)");
        await Ticks(s, 2);
        Assert.Equal(["Task 1/2 done — Health endpoint", "Task 2/2 done — Health test"], jira.Comments.Select(c => c.Text));
    }

    [Fact]
    public async Task Comment_without_title_or_total_uses_fallbacks()
    {
        var dir = TempDir();
        WriteLedger(dir, "Task 3: complete\n");
        var jira = new FakeJira();
        await Ticks(Start(new FakeWorkspace(), jira, dir, ["only"]), 1);
        Assert.Equal(["Task 3/1 done"], jira.Comments.Select(c => c.Text));

        var jira2 = new FakeJira();
        await Ticks(Start(new FakeWorkspace(), jira2, dir), 1);
        Assert.Equal(["Task 3/? done"], jira2.Comments.Select(c => c.Text));
    }

    [Fact]
    public async Task Missing_ledger_directory_is_tolerated()
    {
        var jira = new FakeJira();
        await Ticks(Start(new FakeWorkspace(), jira, TempDir()), 2);
        Assert.Empty(jira.Comments);
    }

    [Fact]
    public async Task Push_failure_is_logged_and_retried_next_tick()
    {
        var ws = new FakeWorkspace(["a", "b", "b"]) { FailNextPush = true };
        await Ticks(Start(ws, new FakeJira(), TempDir()), 3);
        Assert.Single(ws.Pushes);
    }

    [Fact]
    public async Task Failed_comment_is_retried_next_tick()
    {
        var dir = TempDir();
        WriteLedger(dir, "Task 1: complete");
        var jira = new FakeJira { FailComments = true };
        var s = Start(new FakeWorkspace(), jira, dir, ["One"]);
        await s.TickAsync(CancellationToken.None);
        jira.FailComments = false;
        await s.TickAsync(CancellationToken.None);
        Assert.Equal(["Task 1/1 done — One"], jira.Comments.Select(c => c.Text));
    }

    [Fact]
    public async Task Jira_failure_does_not_stop_pushing()
    {
        var dir = TempDir();
        WriteLedger(dir, "Task 1: complete");
        var ws = new FakeWorkspace(["a", "b"]);
        var s = Start(ws, new FakeJira { FailComments = true }, dir, ["One"]);
        await Ticks(s, 2);
        Assert.Single(ws.Pushes);
    }

    [Fact]
    public async Task Cancellation_returns_without_throwing()
    {
        using var cts = new CancellationTokenSource();
        var lp = new LiveProgress(new FakeJira(), new FakeWorkspace(["a"]), NullLogger<LiveProgress>.Instance);
        var run = lp.RunAsync("SCRUM-1", TempDir(), "plan/x", [], TimeSpan.FromMilliseconds(20), cts.Token);
        await Task.Delay(100);
        await cts.CancelAsync();
        await run;
        Assert.True(run.IsCompletedSuccessfully);
    }
}
