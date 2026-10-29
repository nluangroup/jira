using ClaudeQueue.Jira;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Records Jira calls; optionally fails comments.</summary>
public sealed class FakeJira : IJira
{
    /// <summary>Cards returned by <see cref="GetNextReadyAsync"/>, in order.</summary>
    public Queue<JiraCard> Ready { get; } = new();

    /// <summary>Transitions requested, in order.</summary>
    public List<(string Key, string Status)> Transitions { get; } = [];

    /// <summary>Comments posted successfully, in order.</summary>
    public List<(string Key, string Text)> Comments { get; } = [];

    /// <summary>When true, <see cref="CommentAsync"/> throws.</summary>
    public bool FailComments { get; set; }

    /// <inheritdoc />
    public Task<JiraCard?> GetNextReadyAsync(CancellationToken ct) =>
        Task.FromResult(Ready.Count > 0 ? Ready.Dequeue() : null);

    /// <inheritdoc />
    public Task TransitionAsync(string cardKey, string statusName, CancellationToken ct)
    {
        Transitions.Add((cardKey, statusName));
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task CommentAsync(string cardKey, string text, CancellationToken ct)
    {
        if (FailComments) throw new HttpRequestException("jira down");
        Comments.Add((cardKey, text));
        return Task.CompletedTask;
    }
}
