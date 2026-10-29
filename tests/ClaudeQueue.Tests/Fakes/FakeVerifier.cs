using ClaudeQueue.Verify;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Returns a scripted verify result and counts calls.</summary>
public sealed class FakeVerifier : IVerifier
{
    /// <summary>Whether verification passes.</summary>
    public bool Passed { get; set; } = true;

    /// <summary>Number of verify runs.</summary>
    public int Calls { get; private set; }

    /// <summary>Optional shared log; each run appends "verify".</summary>
    public List<string>? Log { get; set; }

    /// <summary>When set, <see cref="RunAsync"/> throws it.</summary>
    public Exception? Throw { get; set; }

    /// <inheritdoc />
    public Task<VerifyResult> RunAsync(string workDir, CancellationToken ct)
    {
        Calls++;
        Log?.Add("verify");
        if (Throw is not null) throw Throw;
        return Task.FromResult(new VerifyResult(Passed, Passed ? 5 : 3, Passed ? 0 : 2, 5, ""));
    }
}
