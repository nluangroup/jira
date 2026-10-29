using ClaudeQueue.Verify;

namespace ClaudeQueue.Tests.Verify;

public class TestSummaryParserTests
{
    [Fact] public void Sums_multiple_test_projects() =>
        Assert.Equal((13, 1, 14), TestSummaryParser.Parse(
            "Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 1 s - A.Tests.dll (net10.0)\n" +
            "Failed!  - Failed:     1, Passed:     3, Skipped:     0, Total:     4, Duration: 2 s - B.Tests.dll (net10.0)"));

    [Fact] public void No_summary_returns_null() => Assert.Null(TestSummaryParser.Parse("Build FAILED."));

    [Fact] public void Collects_skipped_check_lines() =>
        Assert.Equal(["SKIPPED: mutation testing — dotnet-stryker not installed", "SKIPPED: coverage"],
            TestSummaryParser.SkippedChecks("PASS: format\n  SKIPPED: mutation testing — dotnet-stryker not installed\nFAIL: nothing\nSKIPPED: coverage\nnot SKIPPED here"));
}
