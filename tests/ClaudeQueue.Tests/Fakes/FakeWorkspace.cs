using ClaudeQueue.Cards;
using ClaudeQueue.Git;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Records workspace calls and serves scripted HEAD SHAs.</summary>
/// <param name="heads">SHAs returned by successive <see cref="HeadShaAsync"/> calls; the last repeats once exhausted.</param>
public sealed class FakeWorkspace(IEnumerable<string>? heads = null) : IWorkspace
{
    private readonly Queue<string> headQueue = new(heads ?? []);
    private string lastHead = "head";

    /// <summary>Successful pushes as (workDir, branch).</summary>
    public List<(string WorkDir, string Branch)> Pushes { get; } = [];

    /// <summary>Calls made to <see cref="PrepareAsync"/>.</summary>
    public List<(PlanReference Plan, string Branch)> Prepared { get; } = [];

    /// <summary>When true, the next push throws once and the flag resets.</summary>
    public bool FailNextPush { get; set; }

    /// <summary>Directory returned by <see cref="PrepareAsync"/>.</summary>
    public string WorkDir { get; set; } = "workdir";

    /// <summary>Value returned by <see cref="CommitsSinceAsync"/>.</summary>
    public int Commits { get; set; }

    /// <inheritdoc />
    public Task<string> PrepareAsync(PlanReference plan, string branch, CancellationToken ct)
    {
        Prepared.Add((plan, branch));
        return Task.FromResult(WorkDir);
    }

    /// <inheritdoc />
    public Task<string> HeadShaAsync(string workDir, CancellationToken ct)
    {
        if (headQueue.Count > 0) lastHead = headQueue.Dequeue();
        return Task.FromResult(lastHead);
    }

    /// <inheritdoc />
    public Task<int> CommitsSinceAsync(string workDir, string baseSha, CancellationToken ct) => Task.FromResult(Commits);

    /// <inheritdoc />
    public Task PushAsync(string workDir, string branch, CancellationToken ct)
    {
        if (FailNextPush)
        {
            FailNextPush = false;
            throw new InvalidOperationException("push failed");
        }
        Pushes.Add((workDir, branch));
        return Task.CompletedTask;
    }
}
