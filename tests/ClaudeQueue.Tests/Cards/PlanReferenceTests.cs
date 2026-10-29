using ClaudeQueue.Cards;

namespace ClaudeQueue.Tests.Cards;

public class PlanReferenceTests
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void Parses_block_surrounded_by_prose()
    {
        var text = $"Adds a health endpoint.\n\nQueue-Repo: nluangroup/agentic-demo\nQueue-Plan: docs/superpowers/plans/p.md\nQueue-Commit: {Sha}\n\nEdited by hand later.";
        Assert.True(PlanReference.TryParse(text, out var r, out _));
        Assert.Equal(new PlanReference("nluangroup/agentic-demo", "docs/superpowers/plans/p.md", Sha), r);
    }

    [Fact]
    public void Keys_are_case_insensitive_and_trimmed() =>
        Assert.True(PlanReference.TryParse($"  queue-repo:  a/b \nQUEUE-PLAN: x.md\nqueue-commit: {Sha}  ", out _, out _));

    [Theory]
    [InlineData("* Queue-Repo: a/b\n* Queue-Plan: x.md\n- Queue-Commit: {0}")]
    [InlineData("**Queue-Repo:** a/b\n**Queue-Plan:** x.md\n**Queue-Commit:** {0}")]
    [InlineData("*Queue-Repo:* a/b\r\n*Queue-Plan:* x.md\r\n*Queue-Commit:* {0}")]
    [InlineData("* **Queue-Repo**: a/b\n* **Queue-Plan**: x.md\n* **Queue-Commit**: {0}")]
    [InlineData("Queue-Repo: **a/b**\nQueue-Plan: x.md\nQueue-Commit: {0}")]
    public void Tolerates_bullets_and_bold_markers(string template)
    {
        Assert.True(PlanReference.TryParse(template.Replace("{0}", Sha), out var r, out var error), error);
        Assert.Equal(new PlanReference("a/b", "x.md", Sha), r);
    }

    [Theory]
    [InlineData(null, "description is empty")]
    [InlineData("Queue-Repo: a/b\nQueue-Plan: x.md", "Queue-Commit is missing")]
    [InlineData("Queue-Repo: a/b\nQueue-Plan: x.md\nQueue-Commit: abc123", "Queue-Commit must be a full 40-character SHA")]
    [InlineData("Queue-Plan: x.md\nQueue-Commit: 0123456789abcdef0123456789abcdef01234567", "Queue-Repo is missing")]
    public void Rejects_incomplete_blocks_with_reason(string? text, string expectedError)
    {
        Assert.False(PlanReference.TryParse(text, out var r, out var error));
        Assert.Null(r);
        Assert.Equal(expectedError, error);
    }
}
