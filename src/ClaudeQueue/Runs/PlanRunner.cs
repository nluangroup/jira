using System.Globalization;
using ClaudeQueue.Cards;
using ClaudeQueue.Claude;
using ClaudeQueue.Git;
using ClaudeQueue.Jira;
using ClaudeQueue.Plans;
using ClaudeQueue.Reporting;
using ClaudeQueue.Verify;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Runs;

/// <summary>Takes one Jira card from the ready status to the review status: runs its plan with Claude, verifies, opens a PR and reports.</summary>
/// <param name="jira">Jira port.</param>
/// <param name="workspace">Git workspace port.</param>
/// <param name="claude">Claude runner.</param>
/// <param name="verifier">Verification runner.</param>
/// <param name="prs">Pull request port.</param>
/// <param name="progress">Live progress reporter.</param>
/// <param name="options">Queue settings.</param>
/// <param name="time">Clock.</param>
/// <param name="logger">Logger.</param>
public sealed partial class PlanRunner(
    IJira jira,
    IWorkspace workspace,
    IClaude claude,
    IVerifier verifier,
    IPullRequests prs,
    LiveProgress progress,
    IOptions<QueueOptions> options,
    TimeProvider time,
    ILogger<PlanRunner> logger)
{
    private const int MaxRun = 20;

    private sealed class RunState
    {
        public RunOutcome Outcome = RunOutcome.ClaudeFailed;
        public string? Branch;
        public string? PrUrl;
        public bool PrIsDraft;
        public VerifyResult? Verify;
        public string? ClaudeWorkDir;
        public string? Rulings;
        public decimal? CostUsd;
        public readonly List<string> Notes = [];
    }

    /// <summary>Runs one card to completion. Every failure still ends with a report comment and the review transition; only cancellation of <paramref name="ct"/> aborts without them.</summary>
    /// <param name="card">The card to run.</param>
    /// <param name="ct">Service cancellation token.</param>
    public async Task RunAsync(JiraCard card, CancellationToken ct)
    {
        var o = options.Value;
        using var awake = KeepAwake.Acquire();
        var started = time.GetTimestamp();

        await jira.TransitionAsync(card.Key, o.InProgressStatus, ct);
        await jira.CommentAsync(card.Key, $"Started {time.GetLocalNow().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)}", ct);

        var s = new RunState();
        try
        {
            await ExecuteAsync(card, s, started, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LogRunFailed(logger, card.Key, ex.ToString());
            s.Outcome = RunOutcome.ClaudeFailed;
            s.Notes.Add(ex.Message);
            await TryVerifyAfterFailureAsync(s, ct);
        }

        var report = ToReport(s, time.GetElapsedTime(started));
        try
        {
            await jira.CommentAsync(card.Key, ReportBuilder.BuildForJira(report), ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LogStepFailed(logger, card.Key, "report comment", ex.Message);
        }

        try
        {
            await jira.TransitionAsync(card.Key, o.ReviewStatus, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LogStepFailed(logger, card.Key, "transition to " + o.ReviewStatus, ex.Message);
        }
    }

    private static RunReport ToReport(RunState s, TimeSpan duration) =>
        new(s.Outcome, s.Branch, s.PrUrl, s.PrIsDraft, s.Verify, s.Rulings, duration, s.CostUsd,
            string.Join("; ", s.Notes.Where(n => !string.IsNullOrWhiteSpace(n))));

    private async Task ExecuteAsync(JiraCard card, RunState s, long started, CancellationToken ct)
    {
        var o = options.Value;
        if (!PlanReference.TryParse(card.Description, out var plan, out var error))
        {
            Invalid(s, error);
            return;
        }
        if (plan!.Repo != o.DemoRepo)
        {
            Invalid(s, $"Card names repo '{plan.Repo}', expected '{o.DemoRepo}'");
            return;
        }

        var branch = await PickBranchAsync(card, s, ct);
        if (branch is null)
        {
            s.Notes.Add($"Too many reruns of {card.Key}");
            return;
        }

        var workDir = await workspace.PrepareAsync(plan, branch, ct);
        var baseSha = await workspace.HeadShaAsync(workDir, ct);

        var planFile = Path.Combine(workDir, plan.PlanPath);
        if (!File.Exists(planFile))
        {
            s.Notes.Add($"Plan file not found: {plan.PlanPath}");
            return;
        }
        var titles = PlanTaskTitles.Parse(await File.ReadAllTextAsync(planFile, ct));

        s.ClaudeWorkDir = workDir;
        var result = await RunClaudeWithProgressAsync(card.Key, workDir, branch, titles, RunPrompt.Build(plan, o.ExecutionMode), ct);
        s.CostUsd = result.CostUsd;
        s.Rulings = ReportBuilder.ExtractRulings(result.FinalMessage);

        s.Verify = await verifier.RunAsync(workDir, ct);
        s.Outcome = result.Exit switch
        {
            ClaudeExit.TimedOut => RunOutcome.TimedOut,
            ClaudeExit.Failed => RunOutcome.ClaudeFailed,
            _ => s.Verify.Passed ? RunOutcome.Verified : RunOutcome.VerificationFailed,
        };

        if (await workspace.CommitsSinceAsync(workDir, baseSha, ct) == 0)
        {
            s.Notes.Add("No commits were made");
            return;
        }

        await workspace.PushAsync(workDir, branch, ct);
        s.Branch = branch;
        s.PrIsDraft = s.Outcome != RunOutcome.Verified;
        var body = $"Plan: {plan.PlanPath} @ {plan.CommitSha[..7]}\nJira: {o.JiraBaseUrl}/browse/{card.Key}\n\n"
            + ReportBuilder.Build(ToReport(s, time.GetElapsedTime(started)));
        s.PrUrl = await prs.CreateAsync(o.DemoRepo, branch, $"{card.Key} {card.Summary}", body, s.PrIsDraft, ct);
    }

    private async Task TryVerifyAfterFailureAsync(RunState s, CancellationToken ct)
    {
        if (s.ClaudeWorkDir is null || s.Verify is not null) return;
        try
        {
            s.Verify = await verifier.RunAsync(s.ClaudeWorkDir, ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
        {
            LogStepFailed(logger, "run", "verify after failure", ex.Message);
        }
    }

    private static void Invalid(RunState s, string reason)
    {
        s.Outcome = RunOutcome.InvalidCard;
        s.Notes.Add(reason);
    }

    private async Task<string?> PickBranchAsync(JiraCard card, RunState s, CancellationToken ct)
    {
        var repo = options.Value.DemoRepo;
        var first = BranchName.For(card.Key, card.Summary);
        if (!await workspace.RemoteBranchExistsAsync(repo, first, ct)) return first;

        var previous = first;
        for (var n = 2; n <= MaxRun; n++)
        {
            var candidate = $"{first}-run{n}";
            if (!await workspace.RemoteBranchExistsAsync(repo, candidate, ct))
            {
                var previousPr = await prs.FindAsync(repo, previous, ct);
                s.Notes.Add($"Rerun {n} — previous: {previousPr ?? previous}");
                return candidate;
            }
            previous = candidate;
        }
        return null;
    }

    private async Task<ClaudeResult> RunClaudeWithProgressAsync(string cardKey, string workDir, string branch, IReadOnlyList<string> titles, string prompt, CancellationToken ct)
    {
        using var stop = CancellationTokenSource.CreateLinkedTokenSource(ct);
        var ticking = progress.RunAsync(cardKey, workDir, branch, titles, options.Value.ProgressInterval, stop.Token);
        try
        {
            return await claude.RunAsync(workDir, prompt, e =>
            {
                if (StreamEventParser.ToConsoleLine(e) is { } line) LogClaude(logger, line);
            }, ct);
        }
        finally
        {
            await stop.CancelAsync();
            try
            {
                await ticking;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                LogStepFailed(logger, cardKey, "live progress", ex.Message);
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "claude: {Line}")]
    private static partial void LogClaude(ILogger logger, string line);

    [LoggerMessage(Level = LogLevel.Error, Message = "Run of {Key} failed: {Details}")]
    private static partial void LogRunFailed(ILogger logger, string key, string details);

    [LoggerMessage(Level = LogLevel.Error, Message = "{Key}: {Step} failed: {Message}")]
    private static partial void LogStepFailed(ILogger logger, string key, string step, string message);
}
