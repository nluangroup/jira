using ClaudeQueue.Git;

namespace ClaudeQueue.Tests.Git;

public class GhPullRequestsTests
{
    [Fact] public void Gh_args_for_draft() =>
        Assert.Equal(["pr", "create", "--repo", "nluangroup/agentic-demo", "--base", "main", "--head", "plan/SCRUM-1-x", "--title", "T", "--body", "B", "--draft"],
            GhPullRequests.BuildArgs("nluangroup/agentic-demo", "plan/SCRUM-1-x", "T", "B", draft: true));

    [Fact] public void Gh_args_for_ready() =>
        Assert.Equal(["pr", "create", "--repo", "r/r", "--base", "main", "--head", "b", "--title", "T", "--body", "B"],
            GhPullRequests.BuildArgs("r/r", "b", "T", "B", draft: false));
}
