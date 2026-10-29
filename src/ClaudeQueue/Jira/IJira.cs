namespace ClaudeQueue.Jira;

/// <summary>A Jira card as the queue sees it.</summary>
/// <param name="Key">Issue key, e.g. <c>SCRUM-7</c>.</param>
/// <param name="Summary">Card title.</param>
/// <param name="Description">Plain-text description, or null when empty.</param>
public sealed record JiraCard(string Key, string Summary, string? Description);

/// <summary>Port to the Jira board the queue works from.</summary>
public interface IJira
{
    /// <summary>Gets the top-ranked card in the ready status, or null when the queue is empty.</summary>
    /// <param name="ct">Cancellation token.</param>
    Task<JiraCard?> GetNextReadyAsync(CancellationToken ct);

    /// <summary>Moves a card to the status with the given name.</summary>
    /// <param name="cardKey">Issue key.</param>
    /// <param name="statusName">Target status name (case-insensitive).</param>
    /// <param name="ct">Cancellation token.</param>
    Task TransitionAsync(string cardKey, string statusName, CancellationToken ct);

    /// <summary>Adds a plain-text comment to a card.</summary>
    /// <param name="cardKey">Issue key.</param>
    /// <param name="text">Comment text.</param>
    /// <param name="ct">Cancellation token.</param>
    Task CommentAsync(string cardKey, string text, CancellationToken ct);
}
