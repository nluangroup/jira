using System.Globalization;
using ClaudeQueue.Cards;
using ClaudeQueue.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Git;

/// <summary>Git CLI backed <see cref="IWorkspace"/>; relies on the gh credential helper (<c>gh auth setup-git</c>) for https auth.</summary>
/// <param name="options">Queue settings.</param>
/// <param name="logger">Logger.</param>
public sealed partial class GitWorkspace(IOptions<QueueOptions> options, ILogger<GitWorkspace> logger) : IWorkspace
{
    internal Func<string, string> CloneUrl { get; set; } = repo => $"https://github.com/{repo}.git";

    internal Func<int, TimeSpan> RetryDelay { get; set; } = Backoff.Default;

    /// <inheritdoc />
    public async Task<string> PrepareAsync(PlanReference plan, string branch, CancellationToken ct)
    {
        var runs = options.Value.RunsDirectory;
        Directory.CreateDirectory(runs);
        var dir = Path.Combine(runs, $"{DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture)}-{branch.Replace('/', '_')}");
        await GitAsync(runs, ct, "clone", CloneUrl(plan.Repo), dir);
        await GitAsync(dir, ct, "checkout", plan.CommitSha);
        await GitAsync(dir, ct, "checkout", "-b", branch);
        return dir;
    }

    /// <inheritdoc />
    public async Task<bool> RemoteBranchExistsAsync(string repo, string branch, CancellationToken ct)
    {
        var runs = options.Value.RunsDirectory;
        Directory.CreateDirectory(runs);
        var output = await GitAsync(runs, ct, "ls-remote", "--heads", CloneUrl(repo), $"refs/heads/{branch}");
        return output.Trim().Length > 0;
    }

    /// <inheritdoc />
    public async Task<string> HeadShaAsync(string workDir, CancellationToken ct) =>
        (await GitAsync(workDir, ct, "rev-parse", "HEAD")).Trim();

    /// <inheritdoc />
    public async Task<int> CommitsSinceAsync(string workDir, string baseSha, CancellationToken ct) =>
        int.Parse((await GitAsync(workDir, ct, "rev-list", "--count", $"{baseSha}..HEAD")).Trim(), CultureInfo.InvariantCulture);

    /// <inheritdoc />
    public async Task<bool> IsDirtyAsync(string workDir, CancellationToken ct) =>
        (await GitAsync(workDir, ct, "status", "--porcelain")).Trim().Length > 0;

    /// <inheritdoc />
    public async Task StashAsync(string workDir, CancellationToken ct) =>
        await GitAsync(workDir, ct, "-c", "user.name=ClaudeQueue", "-c", "user.email=claude-queue@localhost", "stash", "push", "-u", "-m", "claude-queue-uncommitted");

    /// <inheritdoc />
    public Task PushAsync(string workDir, string branch, CancellationToken ct) =>
        Backoff.RetryAsync(
            () => GitAsync(workDir, ct, "push", "-u", "origin", branch),
            RetryDelay,
            (retry, ex) => LogPushRetry(logger, retry, ex.Message),
            ct);

    [LoggerMessage(Level = LogLevel.Warning, Message = "git push failed (retry {Retry}/3): {Message}")]
    private static partial void LogPushRetry(ILogger logger, int retry, string message);

    private static readonly Dictionary<string, string> NoPromptEnvironment = new()
    {
        ["GIT_TERMINAL_PROMPT"] = "0",
        ["GCM_INTERACTIVE"] = "never",
    };

    private static async Task<string> GitAsync(string workDir, CancellationToken ct, params string[] args)
    {
        var r = await ProcessRunner.RunAsync("git", args, workDir, ct, NoPromptEnvironment);
        if (r.ExitCode != 0)
            throw new InvalidOperationException($"git {string.Join(' ', args)} failed (exit {r.ExitCode}): {r.StdErr.Trim()}");
        return r.StdOut;
    }
}
