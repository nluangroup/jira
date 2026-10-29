using System.Text.RegularExpressions;

namespace ClaudeQueue.Cards;

/// <summary>Builds the working branch name for a card.</summary>
public static partial class BranchName
{
    private const int MaxSlugLength = 40;

    [GeneratedRegex("[^a-z0-9]+")]
    private static partial Regex NonSlugRun();

    /// <summary>Builds <c>plan/&lt;cardKey&gt;-&lt;slug&gt;</c> from the card key and summary.</summary>
    /// <param name="cardKey">The Jira issue key, e.g. SCRUM-12.</param>
    /// <param name="summary">The card summary to slugify.</param>
    /// <returns>The branch name.</returns>
    public static string For(string cardKey, string summary)
    {
        var slug = NonSlugRun().Replace(summary.ToLowerInvariant(), "-").Trim('-');
        if (slug.Length > MaxSlugLength)
        {
            slug = slug[..MaxSlugLength].TrimEnd('-');
        }

        return $"plan/{cardKey}-{slug}";
    }
}
