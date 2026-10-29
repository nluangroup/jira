using System.Text.RegularExpressions;

namespace ClaudeQueue.Cards;

/// <summary>Pointer to an approved plan, read from the Queue-* block in a card description.</summary>
/// <param name="Repo">GitHub repository in owner/name form.</param>
/// <param name="PlanPath">Path of the plan file within the repository.</param>
/// <param name="CommitSha">Full 40-character commit SHA the plan was approved at.</param>
public sealed partial record PlanReference(string Repo, string PlanPath, string CommitSha)
{
    [GeneratedRegex(@"^\s*queue-(repo|plan|commit)\s*:\s*(\S+)\s*$", RegexOptions.IgnoreCase | RegexOptions.Multiline)]
    private static partial Regex KeyLine();

    [GeneratedRegex("^[0-9a-f]{40}$", RegexOptions.IgnoreCase)]
    private static partial Regex FullSha();

    /// <summary>Extracts the Queue-Repo, Queue-Plan and Queue-Commit lines from anywhere in a description.</summary>
    /// <param name="description">The card description text, possibly null.</param>
    /// <param name="reference">The parsed reference, or null on failure.</param>
    /// <param name="error">The reason parsing failed, or empty on success.</param>
    /// <returns>True when a complete, valid block was found.</returns>
    public static bool TryParse(string? description, out PlanReference? reference, out string error)
    {
        reference = null;
        if (string.IsNullOrWhiteSpace(description))
        {
            error = "description is empty";
            return false;
        }

        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (Match m in KeyLine().Matches(description))
        {
            values.TryAdd(m.Groups[1].Value, m.Groups[2].Value);
        }

        foreach (var (key, name) in new[] { ("repo", "Queue-Repo"), ("plan", "Queue-Plan"), ("commit", "Queue-Commit") })
        {
            if (!values.ContainsKey(key))
            {
                error = $"{name} is missing";
                return false;
            }
        }

        if (!FullSha().IsMatch(values["commit"]))
        {
            error = "Queue-Commit must be a full 40-character SHA";
            return false;
        }

        reference = new PlanReference(values["repo"], values["plan"], values["commit"]);
        error = string.Empty;
        return true;
    }
}
