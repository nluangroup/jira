using ClaudeQueue.Claude;
using ClaudeQueue.Jira;
using ClaudeQueue.Runs;
using ClaudeQueue.Tests.Fakes;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Tests.Runs;

public sealed class PlanRunnerTests : IDisposable
{
    private const string Sha = "0123456789abcdef0123456789abcdef01234567";
    private const string PlanPath = "docs/plan.md";
    private const string Base = "plan/SCRUM-7-health-endpoint";
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cq-run-" + Guid.NewGuid().ToString("N"));

    private sealed record Fx(PlanRunner Runner, FakeJira Jira, FakeWorkspace Workspace, FakeClaude Claude, FakeVerifier Verifier, FakePullRequests Prs);

    private static string BlockFor(string repo) => $"Queue-Repo: {repo}\nQueue-Plan: {PlanPath}\nQueue-Commit: {Sha}";
    private static readonly string ValidBlock = BlockFor("nluangroup/agentic-demo");
    private static JiraCard Card(string description, string summary = "Health endpoint") => new("SCRUM-7", summary, description);

    private Fx Fixture(ClaudeExit claude = ClaudeExit.Succeeded, bool verifyPassed = true, int commits = 1, bool planFile = true)
    {
        Directory.CreateDirectory(Path.Combine(_dir, "docs"));
        if (planFile) File.WriteAllText(Path.Combine(_dir, PlanPath), "### Task 1: One\n### Task 2: Two\n");
        var jira = new FakeJira();
        var ws = new FakeWorkspace { WorkDir = _dir, Commits = commits };
        var cl = new FakeClaude { Result = new ClaudeResult(claude, 1.25m, "done\n## Rulings I made\n- chose X") };
        var ver = new FakeVerifier { Passed = verifyPassed };
        var prs = new FakePullRequests();
        var options = Options.Create(new QueueOptions { ProgressInterval = TimeSpan.FromMilliseconds(20) });
        var progress = new LiveProgress(jira, ws, NullLogger<LiveProgress>.Instance);
        var runner = new PlanRunner(jira, ws, cl, ver, prs, progress, options, TimeProvider.System, NullLogger<PlanRunner>.Instance);
        return new Fx(runner, jira, ws, cl, ver, prs);
    }

    private static Task Run(Fx f) => f.Runner.RunAsync(Card(ValidBlock), default);

    [Fact] public async Task Happy_path_ends_in_review_with_ready_pr()
    {
        var f = Fixture(ClaudeExit.Succeeded, verifyPassed: true, commits: 3);
        await f.Runner.RunAsync(Card(ValidBlock), default);
        Assert.Equal(["In Progress", "In Review"], f.Jira.Transitions.Select(t => t.Status));
        Assert.False(f.Prs.Created.Single().Draft);
        Assert.StartsWith("✅ Verified", f.Jira.Comments.Last().Text);
        Assert.StartsWith("Started ", f.Jira.Comments.First().Text);
        var body = f.Prs.Created.Single().Body;
        Assert.StartsWith($"Plan: {PlanPath} @ 0123456\nJira: https://nlundgaard.atlassian.net/browse/SCRUM-7\n\n✅ Verified", body);
        Assert.DoesNotContain("PR:", body);
        Assert.Contains("PR: https://github.com/nluangroup/agentic-demo/pull/1", f.Jira.Comments.Last().Text);
        Assert.Single(f.Workspace.Pushes);
        Assert.Contains(PlanPath, f.Claude.Prompts.Single());
    }

    [Fact] public async Task Verify_failure_gives_draft_pr() { var f = Fixture(ClaudeExit.Succeeded, verifyPassed: false, commits: 2); await Run(f); Assert.True(f.Prs.Created.Single().Draft); Assert.StartsWith("⚠️ Verification failed", f.Jira.Comments.Last().Text); }
    [Fact] public async Task Timeout_still_gets_draft_pr_from_existing_commits() { var f = Fixture(ClaudeExit.TimedOut, verifyPassed: false, commits: 1); await Run(f); Assert.True(f.Prs.Created.Single().Draft); Assert.StartsWith("⏱️ Timed out", f.Jira.Comments.Last().Text); Assert.Equal(1, f.Verifier.Calls); }
    [Fact] public async Task Claude_failure_is_claude_failed_with_draft_even_if_verify_passes() { var f = Fixture(ClaudeExit.Failed, verifyPassed: true, commits: 1); await Run(f); Assert.True(f.Prs.Created.Single().Draft); Assert.StartsWith("❌ Claude failed", f.Jira.Comments.Last().Text); }
    [Fact] public async Task No_commits_means_no_push_and_no_pr() { var f = Fixture(ClaudeExit.Succeeded, verifyPassed: true, commits: 0); await Run(f); Assert.Empty(f.Prs.Created); Assert.Empty(f.Workspace.Pushes); Assert.Contains("No commits were made", f.Jira.Comments.Last().Text); }

    [Fact] public async Task Invalid_block_skips_claude_and_ends_in_review()
    {
        var f = Fixture(); await f.Runner.RunAsync(Card("no block here"), default);
        Assert.Equal(0, f.Claude.Calls); Assert.StartsWith("⛔ Invalid card", f.Jira.Comments.Last().Text);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
    }

    [Fact] public async Task Wrong_repo_is_invalid_card() { var f = Fixture(); await f.Runner.RunAsync(Card(BlockFor("nluangroup/jira")), default); Assert.Contains("Card names repo 'nluangroup/jira', expected 'nluangroup/agentic-demo'", f.Jira.Comments.Last().Text); Assert.Empty(f.Workspace.Prepared); }

    [Fact] public async Task Exception_mid_run_still_reports_and_moves_to_review()
    {
        var f = Fixture(); f.Workspace.ThrowOnPrepare = new InvalidOperationException("git clone failed");
        await Run(f);
        Assert.Contains("git clone failed", f.Jira.Comments.Last().Text); Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
        Assert.StartsWith("❌ Claude failed", f.Jira.Comments.Last().Text);
    }

    [Fact] public async Task Claude_exception_still_runs_verify_and_reports()
    {
        var f = Fixture(); f.Claude.ThrowOnRun = new InvalidOperationException("boom");
        await Run(f);
        Assert.Equal(1, f.Verifier.Calls);
        var last = f.Jira.Comments.Last().Text;
        Assert.StartsWith("❌ Claude failed", last);
        Assert.Contains("boom", last);
        Assert.Contains("Tests:", last);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
        Assert.Empty(f.Prs.Created);
    }

    [Fact] public async Task Uncommitted_changes_are_stashed_before_verify_and_noted()
    {
        var f = Fixture();
        var log = new List<string>();
        f.Workspace.Log = log; f.Verifier.Log = log; f.Workspace.Dirty = true;
        await Run(f);
        Assert.Equal(["stash", "verify"], log);
        Assert.Contains("Claude left uncommitted changes (stashed, not in the PR)", f.Jira.Comments.Last().Text);
        Assert.Contains("Claude left uncommitted changes (stashed, not in the PR)", f.Prs.Created.Single().Body);
    }

    [Fact] public async Task Clean_tree_is_not_stashed() { var f = Fixture(); await Run(f); Assert.Equal(0, f.Workspace.Stashes); Assert.DoesNotContain("uncommitted", f.Jira.Comments.Last().Text); }

    [Fact] public async Task Pr_creation_failure_after_verified_run_keeps_verified_label_and_names_the_step()
    {
        var f = Fixture(); f.Prs.ThrowOnCreate = new InvalidOperationException("gh exploded");
        await Run(f);
        var last = f.Jira.Comments.Last().Text;
        Assert.StartsWith("✅ Verified", last);
        Assert.Contains("pull request failed: gh exploded", last);
        Assert.DoesNotContain("Claude failed", last);
        Assert.Contains("Branch: " + Base, last);
        Assert.Equal(1, f.Verifier.Calls);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
    }

    [Fact] public async Task Push_failure_after_verification_failure_keeps_that_label_and_branch()
    {
        var f = Fixture(verifyPassed: false); f.Workspace.FailNextPush = true;
        await Run(f);
        var last = f.Jira.Comments.Last().Text;
        Assert.StartsWith("⚠️ Verification failed", last);
        Assert.Contains("push failed: push failed", last);
        Assert.Contains("Branch: " + Base, last);
        Assert.Empty(f.Prs.Created);
    }

    [Fact] public async Task Verifier_exception_runs_verify_once_and_reports_verify_step_after_claude_succeeded()
    {
        var f = Fixture(); f.Verifier.Throw = new InvalidOperationException("pwsh missing");
        await Run(f);
        Assert.Equal(1, f.Verifier.Calls);
        var last = f.Jira.Comments.Last().Text;
        Assert.DoesNotContain("Claude failed", last);
        Assert.Contains("verify failed: pwsh missing", last);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
    }

    [Fact] public async Task Failing_started_comment_still_runs_and_ends_with_report_and_review()
    {
        var f = Fixture(); f.Jira.FailNextComments = 1;
        await Run(f);
        Assert.Equal(1, f.Claude.Calls);
        Assert.StartsWith("✅ Verified", f.Jira.Comments.Last().Text);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
    }

    [Fact] public async Task Failed_review_transition_does_not_throw() { var f = Fixture(); f.Jira.FailTransitionTo = "In Review"; await Run(f); Assert.Contains("Verified", f.Jira.Comments.Last().Text); }
    [Fact] public async Task Pr_title_is_card_key_and_summary() { var f = Fixture(commits: 1); await f.Runner.RunAsync(Card(ValidBlock, summary: "Health endpoint"), default); Assert.Equal("SCRUM-7 Health endpoint", f.Prs.Created.Single().Title); }

    [Fact] public async Task Missing_plan_file_is_claude_failed()
    {
        var f = Fixture(planFile: false); await Run(f);
        Assert.Equal(0, f.Claude.Calls);
        Assert.StartsWith("❌ Claude failed", f.Jira.Comments.Last().Text);
        Assert.Contains($"Plan file not found: {PlanPath}", f.Jira.Comments.Last().Text);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
    }

    [Fact] public async Task Rerun_uses_run2_when_branch_exists()
    {
        var f = Fixture(); f.Workspace.RemoteBranches.Add(Base);
        f.Prs.Existing[Base] = "https://github.com/nluangroup/agentic-demo/pull/3";
        await Run(f);
        Assert.Equal(Base + "-run2", f.Workspace.Prepared.Single().Branch);
        Assert.Contains("Rerun 2 — previous: https://github.com/nluangroup/agentic-demo/pull/3", f.Jira.Comments.Last().Text);
    }

    [Fact] public async Task Rerun_without_previous_pr_names_previous_branch()
    {
        var f = Fixture(commits: 0); f.Workspace.RemoteBranches.Add(Base);
        await Run(f);
        Assert.Contains($"Rerun 2 — previous: {Base}; No commits were made", f.Jira.Comments.Last().Text);
    }

    [Fact] public async Task Third_run_uses_run3()
    {
        var f = Fixture(); f.Workspace.RemoteBranches.UnionWith([Base, Base + "-run2"]);
        f.Prs.Existing[Base + "-run2"] = "https://github.com/nluangroup/agentic-demo/pull/4";
        await Run(f);
        Assert.Equal(Base + "-run3", f.Workspace.Prepared.Single().Branch);
        Assert.Contains("Rerun 3 — previous: https://github.com/nluangroup/agentic-demo/pull/4", f.Jira.Comments.Last().Text);
    }

    [Fact] public async Task Too_many_reruns_is_claude_failed()
    {
        var f = Fixture(); f.Workspace.RemoteBranches.Add(Base);
        for (var n = 2; n <= 20; n++) f.Workspace.RemoteBranches.Add($"{Base}-run{n}");
        await Run(f);
        Assert.Empty(f.Workspace.Prepared);
        Assert.Contains("Too many reruns of SCRUM-7", f.Jira.Comments.Last().Text);
        Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
    }

    [Fact] public async Task Service_cancellation_propagates_without_report_or_review_transition()
    {
        var f = Fixture(); f.Claude.BlockUntilCancelled = true;
        using var cts = new CancellationTokenSource();
        var run = f.Runner.RunAsync(Card(ValidBlock), cts.Token);
        while (f.Claude.Calls == 0) await Task.Delay(10);
        await cts.CancelAsync();
        #pragma warning disable VSTHRD003
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => run);
#pragma warning restore VSTHRD003
        Assert.Equal(["In Progress"], f.Jira.Transitions.Select(t => t.Status));
        Assert.Single(f.Jira.Comments);
    }

    public void Dispose() { try { Directory.Delete(_dir, true); } catch (IOException) { } }
}
