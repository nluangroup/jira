using ClaudeQueue.Cards;

namespace ClaudeQueue.Tests.Cards;

public class BranchNameTests
{
    [Theory]
    [InlineData("SCRUM-12", "Add a /health endpoint!", "plan/SCRUM-12-add-a-health-endpoint")]
    [InlineData("SCRUM-3", "  Ærø   logging -- cleanup ", "plan/SCRUM-3-r-logging-cleanup")]
    public void Builds_slugged_branch(string key, string summary, string expected) =>
        Assert.Equal(expected, BranchName.For(key, summary));

    [Fact]
    public void Slug_is_capped_at_40_chars_without_trailing_dash()
    {
        var b = BranchName.For("SCRUM-1", new string('a', 39) + " bbbb");
        Assert.Equal("plan/SCRUM-1-" + new string('a', 39), b);
    }
}
