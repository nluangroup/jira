using ClaudeQueue.Verify;

namespace ClaudeQueue.Tests.Verify;

public class TestSummaryParserTests
{
    [Fact] public void Sums_multiple_test_projects() =>
        Assert.Equal((13, 1, 14), TestSummaryParser.Parse(
            "Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 1 s - A.Tests.dll (net10.0)\n" +
            "Failed!  - Failed:     1, Passed:     3, Skipped:     0, Total:     4, Duration: 2 s - B.Tests.dll (net10.0)"));

    [Fact] public void No_summary_returns_null() => Assert.Null(TestSummaryParser.Parse("Build FAILED."));

    [Fact] public void Parses_vitest_all_passed() =>
        Assert.Equal((12, 0, 12), TestSummaryParser.Parse("      Tests  12 passed (12)"));

    [Fact] public void Parses_vitest_with_failures() =>
        Assert.Equal((11, 1, 12), TestSummaryParser.Parse("      Tests  1 failed | 11 passed (12)"));

    [Fact] public void Parses_vitest_with_skips_in_any_order() =>
        Assert.Equal((10, 1, 12), TestSummaryParser.Parse("      Tests  1 skipped | 10 passed | 1 failed (12)"));

    [Fact] public void Parses_vitest_with_ansi_codes_and_ignores_test_files_line() =>
        Assert.Equal((2, 0, 2), TestSummaryParser.Parse(
            "\u001b[2m Test Files \u001b[22m \u001b[1m\u001b[32m2 passed\u001b[39m\u001b[22m\u001b[90m (2)\u001b[39m\n" +
            "\u001b[2m      Tests \u001b[22m \u001b[1m\u001b[32m2 passed\u001b[39m\u001b[22m\u001b[90m (2)\u001b[39m\n"));

    [Fact] public void Ignores_vitest_test_files_line_alone() =>
        Assert.Null(TestSummaryParser.Parse(" Test Files  2 passed (2)"));

    [Fact] public void Sums_dotnet_and_vitest() =>
        Assert.Equal((12, 1, 13), TestSummaryParser.Parse(
            "Failed!  - Failed:     1, Passed:     3, Skipped:     0, Total:     4, Duration: 2 s - B.Tests.dll (net10.0)\n" +
            " Test Files  1 passed (1)\n      Tests  9 passed (9)\n"));

    [Fact] public void Collects_skipped_check_lines() =>
        Assert.Equal(["SKIPPED: mutation testing — dotnet-stryker not installed", "SKIPPED: coverage"],
            TestSummaryParser.SkippedChecks("PASS: format\n  SKIPPED: mutation testing — dotnet-stryker not installed\nFAIL: nothing\nSKIPPED: coverage\nnot SKIPPED here"));
}
