using ClaudeQueue.Processes;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Verify;

/// <summary>Runs the repo's verify script with PowerShell 7.</summary>
/// <param name="options">Queue settings.</param>
public sealed class PwshVerifier(IOptions<QueueOptions> options) : IVerifier
{
    /// <inheritdoc />
    public async Task<VerifyResult> RunAsync(string workDir, CancellationToken ct)
    {
        var script = options.Value.VerifyScript;
        if (!File.Exists(Path.Combine(workDir, script)))
            return new VerifyResult(false, null, null, null, $"{script} not found");

        var r = await ProcessRunner.RunAsync("pwsh", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script], workDir, ct);
        var output = r.StdOut + r.StdErr;

        var log = Path.Combine(Path.GetDirectoryName(workDir)!, Path.GetFileName(workDir) + "-verify.log");
        await File.WriteAllTextAsync(log, output, ct);

        var tail = output.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).TakeLast(20);
        var counts = TestSummaryParser.Parse(output);
        return new VerifyResult(r.ExitCode == 0, counts?.Passed, counts?.Failed, counts?.Total, string.Join('\n', tail))
        {
            Skipped = TestSummaryParser.SkippedChecks(output),
        };
    }
}
