using ClaudeQueue.Processes;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Verify;

/// <summary>Runs the repo's verify script with PowerShell 7.</summary>
/// <param name="options">Queue settings.</param>
public sealed class PwshVerifier(IOptions<QueueOptions> options) : IVerifier
{
    private static readonly Dictionary<string, string> Environment = new()
    {
        ["MSBUILDDISABLENODEREUSE"] = "1",
        ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0",
    };

    /// <inheritdoc />
    public async Task<VerifyResult> RunAsync(string workDir, CancellationToken ct)
    {
        var script = options.Value.VerifyScript;
        if (!File.Exists(Path.Combine(workDir, script)))
            return new VerifyResult(false, null, null, null, $"{script} not found");

        var lines = new List<string>();
        var timedOut = false;
        var exitCode = -1;
        try
        {
            exitCode = await ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-ExecutionPolicy", "Bypass", "-File", script], workDir,
                line => { lock (lines) lines.Add(line); }, options.Value.VerifyTimeout, ct, Environment);
        }
        catch (TimeoutException)
        {
            timedOut = true;
        }

        string output;
        lock (lines) output = string.Join('\n', lines);
        if (timedOut) output += $"\nverify timed out after {(int)options.Value.VerifyTimeout.TotalMinutes}m";

        var log = Path.Combine(Path.GetDirectoryName(workDir)!, Path.GetFileName(workDir) + "-verify.log");
        await File.WriteAllTextAsync(log, output, ct);

        if (timedOut)
            return new VerifyResult(false, null, null, null, $"verify timed out after {(int)options.Value.VerifyTimeout.TotalMinutes}m");

        var tail = output.Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.Length > 0).TakeLast(20);
        var counts = TestSummaryParser.Parse(output);
        return new VerifyResult(exitCode == 0, counts?.Passed, counts?.Failed, counts?.Total, string.Join('\n', tail))
        {
            Skipped = TestSummaryParser.SkippedChecks(output),
        };
    }
}
