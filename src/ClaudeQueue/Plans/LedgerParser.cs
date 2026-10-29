using System.Text.RegularExpressions;

namespace ClaudeQueue.Plans;

/// <summary>Reads the subagent-driven-development ledger.</summary>
public static partial class LedgerParser
{
    [GeneratedRegex(@"^Task\s+(\d+):\s+complete\b", RegexOptions.Multiline)]
    private static partial Regex CompleteLine();

    /// <summary>Returns the numbers of tasks the ledger marks complete.</summary>
    /// <param name="ledgerText">The ledger file contents.</param>
    /// <returns>The set of completed task numbers.</returns>
    public static IReadOnlySet<int> CompletedTasks(string ledgerText) =>
        CompleteLine().Matches(ledgerText).Select(m => int.Parse(m.Groups[1].Value, System.Globalization.CultureInfo.InvariantCulture)).ToHashSet();
}
