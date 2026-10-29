using ClaudeQueue.Plans;

namespace ClaudeQueue.Tests.Plans;

public class LedgerParserTests
{
    [Fact]
    public void Finds_completed_tasks_only() =>
        Assert.Equal(new HashSet<int> { 1, 3 }, LedgerParser.CompletedTasks(
            "# SDD ledger — plan: p.md\nTask 1: complete (commits a..b, review clean)\nTask 2: fix round 1/5 (...)\nTask 3: complete (commits c..d, 1 parked)"));
}
