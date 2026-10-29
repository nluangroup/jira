using ClaudeQueue.Cards;
using ClaudeQueue.Plans;

namespace ClaudeQueue.Tests.Plans;

public class RunPromptTests
{
    [Fact] public void Prompt_contains_the_non_negotiables()
    {
        var p = RunPrompt.Build(new PlanReference("nluangroup/agentic-demo", "docs/superpowers/plans/p.md", new string('a', 40)), ExecutionMode.SubagentDriven);
        Assert.Contains("docs/superpowers/plans/p.md", p);
        Assert.Contains("superpowers:subagent-driven-development", p);
        Assert.Contains("do not create a worktree", p, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Stop after the final review", p);
        Assert.Contains("## Rulings I made", p);
        Assert.Contains("do not run superpowers:finishing-a-development-branch", p, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("do not brainstorm", p, StringComparison.OrdinalIgnoreCase);
    }

    [Fact] public void Inline_mode_uses_executing_plans()
    {
        var p = RunPrompt.Build(new PlanReference("a/b", "p.md", new string('a', 40)), ExecutionMode.Inline);
        Assert.Contains("superpowers:executing-plans", p);
        Assert.DoesNotContain("subagent-driven-development", p);
    }
}
