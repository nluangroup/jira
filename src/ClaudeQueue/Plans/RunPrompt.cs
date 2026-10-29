namespace ClaudeQueue.Plans;

/// <summary>How Claude is told to execute an approved plan.</summary>
public enum ExecutionMode
{
    /// <summary>Execute the plan using subagent-driven development.</summary>
    SubagentDriven,

    /// <summary>Execute the plan inline in the main session.</summary>
    Inline,
}

/// <summary>Builds the prompt sent to Claude for a headless run of an approved plan.</summary>
public static class RunPrompt
{
    /// <summary>Builds the run prompt.</summary>
    /// <param name="plan">The approved plan to execute.</param>
    /// <param name="mode">Which Superpowers execution skill to use.</param>
    /// <returns>The prompt text.</returns>
    public static string Build(Cards.PlanReference plan, ExecutionMode mode)
    {
        var skill = mode == ExecutionMode.SubagentDriven
            ? "superpowers:subagent-driven-development"
            : "superpowers:executing-plans";
        return $"""
            Execute the implementation plan at {plan.PlanPath} using {skill}.

            - The spec and plan are approved: do not brainstorm or redesign.
            - The current directory is already an isolated workspace on the correct branch: do not create a worktree or another branch.
            - Commit your work on the current branch as the plan directs.
            - Stop after the final review.
            - Do not run superpowers:finishing-a-development-branch; do not merge, push, or open a PR.
            - End with a short summary that includes a "## Rulings I made" section listing any judgment calls you made.
            """;
    }
}
