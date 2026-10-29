using ClaudeQueue.Processes;

namespace ClaudeQueue.Git;

/// <summary><see cref="IPullRequests"/> implemented with the <c>gh</c> CLI.</summary>
public sealed class GhPullRequests : IPullRequests
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
        }, RetryDelay, (retry, ex) => Console.Error.WriteLine($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARN: gh pr create failed (retry {retry}/3): {ex.Message}"), ct);
        return url;
    }
}
