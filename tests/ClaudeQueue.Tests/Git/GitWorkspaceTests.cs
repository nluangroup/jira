using ClaudeQueue.Cards;
using ClaudeQueue.Git;
using ClaudeQueue.Processes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Tests.Git;

public sealed class GitWorkspaceTests : IAsyncLifetime
{
    private readonly string _root = Path.Combine(Path.GetTempPath(), "cq-git-" + Guid.NewGuid().ToString("N"));
    private string _bare = "";
    private string _shaA = "";
    private GitWorkspace _ws = null!;

    public async Task InitializeAsync()
    {
        _bare = Path.Combine(_root, "origin.git");
        var seed = Path.Combine(_root, "seed");
        Directory.CreateDirectory(seed);
        await Git(_root, "init", "--bare", "-b", "main", _bare);
        await Git(seed, "init", "-b", "main");
        await File.WriteAllTextAsync(Path.Combine(seed, "a.txt"), "a");
        await Git(seed, "add", ".");
        await Git(seed, "commit", "-m", "A");
        _shaA = (await Git(seed, "rev-parse", "HEAD")).Trim();
        await File.WriteAllTextAsync(Path.Combine(seed, "b.txt"), "b");
        await Git(seed, "add", ".");
        await Git(seed, "commit", "-m", "B");
        await Git(seed, "push", _bare, "main");

        var options = Options.Create(new QueueOptions { RunsDirectory = Path.Combine(_root, "runs") });
        _ws = new GitWorkspace(options, NullLogger<GitWorkspace>.Instance)
        {
            CloneUrl = _ => new Uri(_bare).AbsoluteUri,
            RetryDelay = _ => TimeSpan.Zero,
        };
    }

    private static readonly string[] IdentityArgs = ["-c", "user.name=Test", "-c", "user.email=test@example.com", "-c", "commit.gpgsign=false"];

    private static async Task<string> Git(string dir, params string[] args)
    {
        var full = IdentityArgs.Concat(args);
        var r = await ProcessRunner.RunAsync("git", full, dir, default);
        Assert.True(r.ExitCode == 0, $"git {string.Join(' ', args)}: {r.StdErr}");
        return r.StdOut;
    }

    [Fact] public async Task Prepare_clones_checks_out_sha_and_creates_branch()
    {
        var dir = await _ws.PrepareAsync(new PlanReference("x/y", "p.md", _shaA), "plan/SCRUM-1-x", default);
        Assert.Equal(_shaA, await _ws.HeadShaAsync(dir, default));
        Assert.Equal("plan/SCRUM-1-x", (await Git(dir, "branch", "--show-current")).Trim());
        Assert.EndsWith("-plan_SCRUM-1-x", dir);
    }

    [Fact] public async Task CommitsSince_counts_new_commits()
    {
        var dir = await _ws.PrepareAsync(new PlanReference("x/y", "p.md", _shaA), "plan/SCRUM-1-x", default);
        await Git(dir, "commit", "--allow-empty", "-m", "1");
        await Git(dir, "commit", "--allow-empty", "-m", "2");
        Assert.Equal(2, await _ws.CommitsSinceAsync(dir, _shaA, default));
    }

    [Fact] public async Task Push_publishes_branch_to_origin()
    {
        var dir = await _ws.PrepareAsync(new PlanReference("x/y", "p.md", _shaA), "plan/SCRUM-1-x", default);
        await Git(dir, "commit", "--allow-empty", "-m", "1");
        await _ws.PushAsync(dir, "plan/SCRUM-1-x", default);
        Assert.Contains("plan/SCRUM-1-x", await Git(_bare, "branch"));
    }

    [Fact] public async Task Push_failure_retries_then_throws()
    {
        var dir = await _ws.PrepareAsync(new PlanReference("x/y", "p.md", _shaA), "plan/SCRUM-1-x", default);
        await Git(dir, "remote", "set-url", "origin", new Uri(Path.Combine(_root, "missing.git")).AbsoluteUri);
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => _ws.PushAsync(dir, "plan/SCRUM-1-x", default));
        Assert.Contains("git push", ex.Message);
    }

    [Fact] public async Task Prepare_with_unknown_sha_throws_with_git_stderr() =>
        Assert.Contains("git checkout", (await Assert.ThrowsAsync<InvalidOperationException>(() =>
            _ws.PrepareAsync(new PlanReference("x/y", "p.md", new string('f', 40)), "b", default))).Message);

    public Task DisposeAsync()
    {
        if (!Directory.Exists(_root)) return Task.CompletedTask;
        foreach (var f in Directory.EnumerateFiles(_root, "*", SearchOption.AllDirectories))
            File.SetAttributes(f, FileAttributes.Normal);
        try { Directory.Delete(_root, true); } catch (IOException) { }
        return Task.CompletedTask;
    }
}
