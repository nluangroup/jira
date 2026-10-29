using ClaudeQueue.Processes;
using Microsoft.Extensions.Logging;

namespace ClaudeQueue.Git;

/// <summary><see cref="IPullRequests"/> implemented with the <c>gh</c> CLI.</summary>
/// <param name="logger">Logger.</param>
public sealed partial class GhPullRequests(ILogger<GhPullRequests> logger) : IPullRequests
{
    internal Func<int, TimeSpan> RetryDelay { get; set; } = Backoff.Default;

    /// <summary>Builds the <c>gh pr create</c> argument list.</summary>
    /// <param name="repo">Repository in owner/name form.</param>
    /// <param name="branch">Head branch.</param>
    /// <param name="title">PR title.</param>
    /// <param name="body">PR body.</param>
    /// <param name="draft">True to add <c>--draft</c>.</param>
    public static IReadOnlyList<string> BuildArgs(string repo, string branch, string title, string body, bool draft)
    {
        var args = new List<string> { "pr", "create", "--repo", repo, "--base", "main", "--head", branch, "--title", title, "--body", body };
        if (draft) args.Add("--draft");
        return args;
    }

    /// <summary>Builds the <c>gh pr list</c> argument list that prints the URL of the newest PR for a head branch.</summary>
    /// <param name="repo">Repository in owner/name form.</param>
    /// <param name="branch">Head branch.</param>
    public static IReadOnlyList<string> BuildFindArgs(string repo, string branch) =>
        ["pr", "list", "--repo", repo, "--head", branch, "--state", "all", "--json", "url", "--jq", ".[0].url"];

    /// <inheritdoc />
    public async Task<string> CreateAsync(string repo, string branch, string title, string body, bool draft, CancellationToken ct)
    {
        var args = BuildArgs(repo, branch, title, body, draft);
        var url = "";
        await Backoff.RetryAsync(async () =>
        {
            var r = await ProcessRunner.RunAsync("gh", args, Directory.GetCurrentDirectory(), ct);
            if (r.ExitCode != 0)
                throw new InvalidOperationException($"gh pr create failed (exit {r.ExitCode}): {r.StdErr.Trim()}");
            url = r.StdOut.Trim();
        }, RetryDelay, (retry, ex) => LogRetry(logger, "create", retry, ex.Message), ct);
        return url;
    }

    /// <inheritdoc />
    public async Task<string?> FindAsync(string repo, string branch, CancellationToken ct)
    {
        var args = BuildFindArgs(repo, branch);
        string? url = null;
        await Backoff.RetryAsync(async () =>
        {
            var r = await ProcessRunner.RunAsync("gh", args, Directory.GetCurrentDirectory(), ct);
            if (r.ExitCode != 0)
                throw new InvalidOperationException($"gh pr list failed (exit {r.ExitCode}): {r.StdErr.Trim()}");
            var text = r.StdOut.Trim();
            url = text.Length == 0 || text == "null" ? null : text;
        }, RetryDelay, (retry, ex) => LogRetry(logger, "list", retry, ex.Message), ct);
        return url;
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "gh pr {Verb} failed (retry {Retry}/3): {Message}")]
    private static partial void LogRetry(ILogger logger, string verb, int retry, string message);
}
