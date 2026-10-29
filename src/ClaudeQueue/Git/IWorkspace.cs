using ClaudeQueue.Cards;

namespace ClaudeQueue.Git;

/// <summary>Git working copy operations for a run.</summary>
public interface IWorkspace
{
    /// <summary>Fresh-clones the plan's repo, checks out the plan commit and creates <paramref name="branch"/>.</summary>
    /// <param name="plan">Plan reference naming the repo and commit.</param>
    /// <param name="branch">Branch to create.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The working directory.</returns>
    Task<string> PrepareAsync(PlanReference plan, string branch, CancellationToken ct);

    /// <summary>Checks whether <paramref name="branch"/> exists on the remote of <paramref name="repo"/>, without cloning.</summary>
    /// <param name="repo">Repository in owner/name form.</param>
    /// <param name="branch">Branch name.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<bool> RemoteBranchExistsAsync(string repo, string branch, CancellationToken ct);

    /// <summary>Returns the full SHA of HEAD.</summary>
    /// <param name="workDir">Working directory.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<string> HeadShaAsync(string workDir, CancellationToken ct);

    /// <summary>Counts commits reachable from HEAD but not from <paramref name="baseSha"/>.</summary>
    /// <param name="workDir">Working directory.</param>
    /// <param name="baseSha">Commit to count from.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<int> CommitsSinceAsync(string workDir, string baseSha, CancellationToken ct);

    /// <summary>Pushes <paramref name="branch"/> to origin, retrying transient failures.</summary>
    /// <param name="workDir">Working directory.</param>
    /// <param name="branch">Branch to push.</param>
    /// <param name="ct">Cancellation token.</param>
    Task PushAsync(string workDir, string branch, CancellationToken ct);
}
