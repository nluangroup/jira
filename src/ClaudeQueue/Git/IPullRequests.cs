namespace ClaudeQueue.Git;

/// <summary>Opens pull requests on GitHub.</summary>
public interface IPullRequests
{
    /// <summary>Creates a pull request against <c>main</c>.</summary>
    /// <param name="repo">Repository in owner/name form.</param>
    /// <param name="branch">Head branch.</param>
    /// <param name="title">PR title.</param>
    /// <param name="body">PR body.</param>
    /// <param name="draft">True to open as a draft.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PR URL.</returns>
    Task<string> CreateAsync(string repo, string branch, string title, string body, bool draft, CancellationToken ct);

    /// <summary>Finds the pull request (any state) whose head is <paramref name="branch"/>.</summary>
    /// <param name="repo">Repository in owner/name form.</param>
    /// <param name="branch">Head branch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The PR URL, or null when there is none.</returns>
    Task<string?> FindAsync(string repo, string branch, CancellationToken ct);
}
