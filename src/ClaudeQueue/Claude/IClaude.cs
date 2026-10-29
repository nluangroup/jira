namespace ClaudeQueue.Claude;

/// <summary>How a Claude run ended.</summary>
public enum ClaudeExit
{
    /// <summary>Claude exited cleanly and reported success.</summary>
    Succeeded,

    /// <summary>Claude exited non-zero, reported an error, or produced no result.</summary>
    Failed,

    /// <summary>The wall-clock timeout elapsed and the process tree was killed.</summary>
    TimedOut,
}

/// <summary>Outcome of a Claude run.</summary>
/// <param name="Exit">How the run ended.</param>
/// <param name="CostUsd">Total cost in USD, when reported.</param>
/// <param name="FinalMessage">Claude's final result text, when reported.</param>
public sealed record ClaudeResult(ClaudeExit Exit, decimal? CostUsd, string? FinalMessage)
{
    /// <summary>Set when the repo's <c>.claude/settings.json</c> could not be used and only the service's fixed blocks applied.</summary>
    public string? PermissionsWarning { get; init; }
}

/// <summary>Runs Claude headlessly against a working copy.</summary>
public interface IClaude
{
    /// <summary>Runs Claude with a prompt, streaming events as they arrive.</summary>
    /// <param name="workDir">Repository working directory.</param>
    /// <param name="prompt">The prompt.</param>
    /// <param name="onEvent">Called for each recognised stream event; exceptions it throws are logged and ignored.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The run outcome.</returns>
    Task<ClaudeResult> RunAsync(string workDir, string prompt, Action<StreamEvent> onEvent, CancellationToken ct);
}
