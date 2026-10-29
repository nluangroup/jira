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

    /// <summary>When set, transitions to this status throw.</summary>
    public string? FailTransitionTo { get; set; }

    /// <summary>Number of times <see cref="GetNextReadyAsync"/> was called.</summary>
    public int ReadyPolls { get; private set; }

    /// <summary>When true, the next poll throws once and the flag resets.</summary>
    public bool FailNextPoll { get; set; }

    /// <inheritdoc />
    public Task<JiraCard?> GetNextReadyAsync(CancellationToken ct)
    {
        ReadyPolls++;
        if (FailNextPoll)
        {
            FailNextPoll = false;
            throw new HttpRequestException("jira down");
        }
        return Task.FromResult(Ready.Count > 0 ? Ready.Dequeue() : null);
    }

    /// <summary>Status returned by <see cref="GetStatusAsync"/> for any card.</summary>
    public string Status { get; set; } = "Ready for Claude";

    /// <inheritdoc />
    public Task<string> GetStatusAsync(string cardKey, CancellationToken ct) => Task.FromResult(Status);

    /// <inheritdoc />
    public Task TransitionAsync(string cardKey, string statusName, CancellationToken ct)
    {
        if (statusName == FailTransitionTo) throw new HttpRequestException("jira down");
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
