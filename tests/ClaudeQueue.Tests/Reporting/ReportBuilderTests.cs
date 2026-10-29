using ClaudeQueue.Reporting;
using ClaudeQueue.Verify;

namespace ClaudeQueue.Tests.Reporting;

public class ReportBuilderTests
{
    [Fact] public void Report_lists_skipped_checks_even_when_verified()
    {
        var t = ReportBuilder.Build(new RunReport(RunOutcome.Verified, "b", "u", false,
            new VerifyResult(true, 5, 0, 5, "") { Skipped = ["SKIPPED: coverage"] }, null, TimeSpan.Zero, null, null));
        Assert.StartsWith("✅ Verified", t);
        Assert.Contains("⚠️ Skipped checks:\n- SKIPPED: coverage", t);
    }

    [Theory]
    [InlineData(RunOutcome.Verified, "Verified")]
    [InlineData(RunOutcome.VerificationFailed, "Verification failed")]
    [InlineData(RunOutcome.ClaudeFailed, "Claude failed")]
    [InlineData(RunOutcome.TimedOut, "Timed out")]
    [InlineData(RunOutcome.InvalidCard, "Invalid card")]
    public void Outcome_labels_match_spec(RunOutcome o, string label) => Assert.Equal(label, ReportBuilder.Label(o));

    [Fact] public void Verified_report_shows_pr_tests_rulings_time_cost()
    {
        var text = ReportBuilder.Build(new RunReport(RunOutcome.Verified, "plan/SCRUM-7-x", "https://github.com/nluangroup/agentic-demo/pull/3", false,
            new VerifyResult(true, 14, 0, 14, ""), "- Used 200 for healthy — spec silent — cheap to change", TimeSpan.FromSeconds(512), 1.234m, null));
        Assert.StartsWith("✅ Verified", text);
        Assert.Contains("PR: https://github.com/nluangroup/agentic-demo/pull/3", text);
        Assert.Contains("Branch: plan/SCRUM-7-x", text);
        Assert.Contains("Tests: 14/14 passed", text);
        Assert.Contains("Rulings I made:\n- Used 200 for healthy", text);
        Assert.Contains("Time: 8m 32s · Cost: $1.23", text);
    }

    [Fact] public void Failed_verification_marks_pr_as_draft() =>
        Assert.Contains("PR (draft):", ReportBuilder.Build(new RunReport(RunOutcome.VerificationFailed, "b", "u", true, new VerifyResult(false, 3, 1, 4, ""), null, TimeSpan.Zero, null, null)));

    [Fact] public void Invalid_card_shows_reason_and_no_pr()
    {
        var t = ReportBuilder.Build(new RunReport(RunOutcome.InvalidCard, null, null, false, null, null, TimeSpan.Zero, null, "Queue-Commit is missing"));
        Assert.StartsWith("⛔ Invalid card", t);
        Assert.Contains("Queue-Commit is missing", t);
        Assert.DoesNotContain("PR", t);
    }

    [Fact] public void Jira_text_wraps_rulings_and_detail_in_noformat_but_plain_text_does_not()
    {
        var report = new RunReport(RunOutcome.Verified, "b", "u", false, null, "- GET /items/{id} returns [x] done", TimeSpan.Zero, null, "Rerun 2 - previous: *b*");
        var jira = ReportBuilder.BuildForJira(report);
        Assert.Contains("Rulings I made:\n{noformat}\n- GET /items/{id} returns [x] done\n{noformat}", jira);
        Assert.Contains("{noformat}\nRerun 2 - previous: *b*\n{noformat}", jira);
        var plain = ReportBuilder.Build(report);
        Assert.DoesNotContain("{noformat}", plain);
        Assert.Contains("Rulings I made:\n- GET /items/{id} returns [x] done", plain);
    }

    [Fact] public void Extracts_rulings_section_until_next_heading() =>
        Assert.Equal("- A — b — c\n- D — e — f", ReportBuilder.ExtractRulings("Done.\n\n## Rulings I made\n- A — b — c\n- D — e — f\n\n## Next steps\nx"));

    [Fact] public void No_rulings_section_returns_null() => Assert.Null(ReportBuilder.ExtractRulings("Done."));

    [Fact] public void Tests_line_shows_failed_count_and_no_summary()
    {
        var failed = ReportBuilder.Build(new RunReport(RunOutcome.VerificationFailed, null, null, false, new VerifyResult(false, 3, 1, 4, ""), null, TimeSpan.Zero, null, null));
        Assert.Contains("Tests: 3/4 passed, 1 failed", failed);
        var none = ReportBuilder.Build(new RunReport(RunOutcome.VerificationFailed, null, null, false, new VerifyResult(false, null, null, null, ""), null, TimeSpan.Zero, null, null));
        Assert.Contains("Tests: no summary", none);
    }

    [Fact] public void Time_uses_total_minutes_and_omits_null_cost() =>
        Assert.Contains("Time: 75m 0s", ReportBuilder.Build(new RunReport(RunOutcome.TimedOut, null, null, false, null, null, TimeSpan.FromMinutes(75), null, null)));
}
