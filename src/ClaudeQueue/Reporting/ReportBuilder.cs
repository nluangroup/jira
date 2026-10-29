using System.Globalization;
using ClaudeQueue.Verify;

namespace ClaudeQueue.Reporting;

/// <summary>How a queue run ended.</summary>
public enum RunOutcome
{
    /// <summary>Claude finished and verification passed.</summary>
    Verified,
    /// <summary>Claude finished but verification failed.</summary>
    VerificationFailed,
    /// <summary>The Claude process failed.</summary>
    ClaudeFailed,
    /// <summary>The wall-clock timeout was hit.</summary>
    TimedOut,
    /// <summary>The card could not be run.</summary>
    InvalidCard
}

/// <summary>Everything the report comment needs.</summary>
/// <param name="Outcome">Run outcome.</param>
/// <param name="Branch">Pushed branch, if any.</param>
/// <param name="PrUrl">Pull request URL, if any.</param>
/// <param name="PrIsDraft">Whether the PR is a draft.</param>
/// <param name="Verify">Verification result, if verification ran.</param>
/// <param name="Rulings">Rulings text extracted from Claude's final message.</param>
/// <param name="Duration">Wall-clock run time.</param>
/// <param name="CostUsd">Run cost in USD, if known.</param>
/// <param name="Detail">Extra explanation, e.g. why a card is invalid.</param>
public sealed record RunReport(RunOutcome Outcome, string? Branch, string? PrUrl, bool PrIsDraft, VerifyResult? Verify, string? Rulings, TimeSpan Duration, decimal? CostUsd, string? Detail);

/// <summary>Builds the Jira report comment text.</summary>
public static class ReportBuilder
{
    /// <summary>Renders the report comment.</summary>
    /// <param name="r">Run report.</param>
    public static string Build(RunReport r)
    {
        var lines = new List<string> { $"{Icon(r.Outcome)} {Label(r.Outcome)}" };
        if (!string.IsNullOrWhiteSpace(r.Detail)) lines.Add(r.Detail);
        if (r.PrUrl is not null) lines.Add($"{(r.PrIsDraft ? "PR (draft)" : "PR")}: {r.PrUrl}");
        if (r.Branch is not null) lines.Add($"Branch: {r.Branch}");
        if (r.Verify is { } v)
        {
            lines.Add(v.TestsTotal is { } total
                ? $"Tests: {v.TestsPassed}/{total} passed" + (v.TestsFailed > 0 ? $", {v.TestsFailed} failed" : "")
                : "Tests: no summary");
            if (v.Skipped.Count > 0)
            {
                lines.Add("⚠️ Skipped checks:");
                lines.AddRange(v.Skipped.Select(s => $"- {s}"));
            }
        }
        if (!string.IsNullOrWhiteSpace(r.Rulings))
        {
            lines.Add("");
            lines.Add("Rulings I made:");
            lines.Add(r.Rulings);
        }
        lines.Add("");
        var time = $"Time: {(int)r.Duration.TotalMinutes}m {r.Duration.Seconds}s";
        lines.Add(r.CostUsd is { } cost ? $"{time} · Cost: ${cost.ToString("0.00", CultureInfo.InvariantCulture)}" : time);
        return string.Join("\n", lines);
    }

    /// <summary>Extracts the "Rulings I made" section from Claude's final message, or null.</summary>
    /// <param name="finalMessage">Claude's final message.</param>
    public static string? ExtractRulings(string? finalMessage)
    {
        if (finalMessage is null) return null;
        var section = finalMessage.Split('\n')
            .SkipWhile(l => !l.Contains("Rulings I made", StringComparison.Ordinal))
            .Skip(1)
            .TakeWhile(l => !l.StartsWith('#'));
        var text = string.Join("\n", section).Trim();
        return text.Length == 0 ? null : text;
    }

    /// <summary>Human label for an outcome.</summary>
    /// <param name="o">Outcome.</param>
    public static string Label(RunOutcome o) => o switch
    {
        RunOutcome.Verified => "Verified",
        RunOutcome.VerificationFailed => "Verification failed",
        RunOutcome.ClaudeFailed => "Claude failed",
        RunOutcome.TimedOut => "Timed out",
        RunOutcome.InvalidCard => "Invalid card",
        _ => throw new ArgumentOutOfRangeException(nameof(o))
    };

    private static string Icon(RunOutcome o) => o switch
    {
        RunOutcome.Verified => "✅",
        RunOutcome.VerificationFailed => "⚠️",
        RunOutcome.ClaudeFailed => "❌",
        RunOutcome.TimedOut => "⏱️",
        _ => "⛔"
    };
}
