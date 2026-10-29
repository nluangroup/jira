using System.Globalization;
using System.Text.RegularExpressions;

namespace ClaudeQueue.Verify;

/// <summary>Outcome of running the repo's verify script.</summary>
/// <param name="Passed">True when verification succeeded.</param>
/// <param name="TestsPassed">Total passed tests, or null when no summary was found.</param>
/// <param name="TestsFailed">Total failed tests, or null when no summary was found.</param>
/// <param name="TestsTotal">Total tests, or null when no summary was found.</param>
/// <param name="Detail">Short explanation, typically the failure output tail.</param>
public sealed record VerifyResult(bool Passed, int? TestsPassed, int? TestsFailed, int? TestsTotal, string Detail)
{
    /// <summary>SKIPPED check lines from the verify script; a skip is not a failure but must be reported.</summary>
    public IReadOnlyList<string> Skipped { get; init; } = [];
}

/// <summary>Parses verify script output.</summary>
public static partial class TestSummaryParser
{
    [GeneratedRegex(@"(?:Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+\d+,\s+Total:\s+(\d+)")]
    private static partial Regex SummaryLine();

    [GeneratedRegex(@"\x1b\[[0-9;]*m")]
    private static partial Regex AnsiCode();

    [GeneratedRegex(@"^[ \t]*Tests[ \t]+(?<parts>[^\r\n]*?)[ \t]*\((?<total>\d+)\)[ \t]*\r?$", RegexOptions.Multiline)]
    private static partial Regex VitestSummaryLine();

    [GeneratedRegex(@"(\d+)\s+failed")]
    private static partial Regex VitestFailedPart();

    [GeneratedRegex(@"(\d+)\s+passed")]
    private static partial Regex VitestPassedPart();

    /// <summary>Sums every <c>dotnet test</c> and Vitest summary line; null when there are none.</summary>
    /// <param name="output">Raw verify output.</param>
    public static (int Passed, int Failed, int Total)? Parse(string output)
    {
        int passed = 0, failed = 0, total = 0;
        var found = false;
        output = AnsiCode().Replace(output, "");
        foreach (Match m in SummaryLine().Matches(output))
        {
            found = true;
            failed += int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture);
            passed += int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture);
            total += int.Parse(m.Groups[3].Value, CultureInfo.InvariantCulture);
        }
        foreach (Match m in VitestSummaryLine().Matches(output))
        {
            found = true;
            var parts = m.Groups["parts"].Value;
            failed += VitestCount(VitestFailedPart(), parts);
            passed += VitestCount(VitestPassedPart(), parts);
            total += int.Parse(m.Groups["total"].Value, CultureInfo.InvariantCulture);
        }
        return found ? (passed, failed, total) : null;
    }

    private static int VitestCount(Regex part, string parts) =>
        part.Match(parts) is { Success: true } m ? int.Parse(m.Groups[1].Value, CultureInfo.InvariantCulture) : 0;

    /// <summary>Returns each line starting with <c>SKIPPED</c> (after optional whitespace), trimmed, in order.</summary>
    /// <param name="output">Raw verify output.</param>
    public static IReadOnlyList<string> SkippedChecks(string output) =>
        output.Split('\n')
            .Select(l => l.Trim())
            .Where(l => l.StartsWith("SKIPPED", StringComparison.Ordinal))
            .ToList();
}

/// <summary>Runs the repo's verification in a working directory.</summary>
public interface IVerifier
{
    /// <summary>Runs verification in <paramref name="workDir"/>.</summary>
    /// <param name="workDir">Repository working directory.</param>
    /// <param name="ct">Cancellation token.</param>
    Task<VerifyResult> RunAsync(string workDir, CancellationToken ct);
}
