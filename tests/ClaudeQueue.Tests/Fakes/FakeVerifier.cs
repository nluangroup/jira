using ClaudeQueue.Verify;

namespace ClaudeQueue.Tests.Fakes;

/// <summary>Returns a scripted verify result and counts calls.</summary>
public sealed class FakeVerifier : IVerifier
{
    /// <summary>Whether verification passes.</summary>
    public bool Passed { get; set; } = true;

    /// <summary>Number of verify runs.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public Task<VerifyResult> RunAsync(string workDir, CancellationToken ct)
    {
        Calls++;
        return Task.FromResult(new VerifyResult(Passed, Passed ? 5 : 3, Passed ? 0 : 2, 5, ""));
    }
}
