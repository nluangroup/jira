using System.Text.RegularExpressions;

namespace ClaudeQueue.Plans;

/// <summary>Reads task headings from a plan document.</summary>
public static partial class PlanTaskTitles
{
    [GeneratedRegex(@"^###\s+Task\s+(\d+):\s*(.+?)\s*$", RegexOptions.Multiline)]
    private static partial Regex TaskHeading();

    /// <summary>Returns task titles in document order; index 0 is Task 1.</summary>
    /// <param name="planMarkdown">The plan markdown.</param>
    /// <returns>The task titles.</returns>
    public static IReadOnlyList<string> Parse(string planMarkdown) =>
        TaskHeading().Matches(planMarkdown).Select(m => m.Groups[2].Value).ToList();
}
