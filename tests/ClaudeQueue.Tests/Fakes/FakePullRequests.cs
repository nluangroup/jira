using ClaudeQueue.Git;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Records created PRs and serves scripted lookups.</summary>
public sealed class FakePullRequests : IPullRequests
{
    /// <summary>PRs created, in order.</summary>
    public List<(string Repo, string Branch, string Title, string Body, bool Draft)> Created { get; } = [];

    /// <summary>PR URLs by head branch, served by <see cref="FindAsync"/>.</summary>
    public Dictionary<string, string> Existing { get; } = [];

    /// <summary>When set, <see cref="CreateAsync"/> throws it.</summary>
    public Exception? ThrowOnCreate { get; set; }

    /// <inheritdoc />
    public Task<string> CreateAsync(string repo, string branch, string title, string body, bool draft, CancellationToken ct)
    {
        if (ThrowOnCreate is not null) throw ThrowOnCreate;
        Created.Add((repo, branch, title, body, draft));
        return Task.FromResult($"https://github.com/{repo}/pull/{Created.Count}");
    }

    /// <inheritdoc />
    public Task<string?> FindAsync(string repo, string branch, CancellationToken ct) =>
        Task.FromResult(Existing.TryGetValue(branch, out var url) ? url : null);
}
