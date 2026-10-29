using ClaudeQueue.Plans;

namespace ClaudeQueue.Tests.Plans;

public class PlanTaskTitlesTests
{
    private static readonly string[] Expected = ["Health endpoint", "Health check test"];

    [Fact]
    public void Reads_task_headings_in_order() =>
        Assert.Equal(Expected,
            PlanTaskTitles.Parse("# Plan\n### Task 1: Health endpoint\ntext\n### Task 2: Health check test\n## Other"));
}
