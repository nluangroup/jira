using ClaudeQueue.Claude;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Returns a scripted Claude result and records invocations.</summary>
public sealed class FakeClaude : IClaude
{
    /// <summary>Result returned by <see cref="RunAsync"/>.</summary>
    public ClaudeResult Result { get; set; } = new(ClaudeExit.Succeeded, 1.25m, "done\n## Rulings I made\n- chose X");

    /// <summary>Number of runs started.</summary>
    public int Calls { get; private set; }

    /// <summary>Prompts received, in order.</summary>
    public List<string> Prompts { get; } = [];

    /// <summary>When set, <see cref="RunAsync"/> waits on the token and throws when it is cancelled.</summary>
    public bool BlockUntilCancelled { get; set; }

    /// <summary>When set, <see cref="RunAsync"/> throws it.</summary>
    public Exception? ThrowOnRun { get; set; }

    /// <inheritdoc />
    public async Task<ClaudeResult> RunAsync(string workDir, string prompt, Action<StreamEvent> onEvent, CancellationToken ct)
    {
        Calls++;
        Prompts.Add(prompt);
        if (ThrowOnRun is not null) throw ThrowOnRun;
        if (BlockUntilCancelled) await Task.Delay(Timeout.Infinite, ct);
        return Result;
    }
}
