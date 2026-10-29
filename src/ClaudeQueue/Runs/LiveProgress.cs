using System.Globalization;
using ClaudeQueue.Git;
using ClaudeQueue.Jira;
using ClaudeQueue.Plans;
using Microsoft.Extensions.Logging;

namespace ClaudeQueue.Runs;

/// <summary>Pushes the branch as HEAD moves and comments on Jira as plan tasks complete while Claude runs.</summary>
/// <param name="jira">Jira port.</param>
/// <param name="workspace">Git workspace port.</param>
/// <param name="logger">Logger.</param>
public sealed partial class LiveProgress(IJira jira, IWorkspace workspace, ILogger<LiveProgress> logger)
{
    /// <summary>Ticks until cancelled, runs one last bounded tick so work finished in the final interval is still reported, then returns normally.</summary>
    /// <param name="cardKey">Jira card to comment on.</param>
    /// <param name="workDir">Clone directory Claude works in.</param>
    /// <param name="branch">Branch to push.</param>
    /// <param name="taskTitles">Plan task titles, in plan order.</param>
    /// <param name="interval">Time between ticks.</param>
    /// <param name="ct">Cancellation token that stops the loop.</param>
    public async Task RunAsync(string cardKey, string workDir, string branch, IReadOnlyList<string> taskTitles, TimeSpan interval, CancellationToken ct)
    {
        var session = StartSession(cardKey, workDir, branch, taskTitles);
        using var timer = new PeriodicTimer(interval);
        try
        {
            do
            {
                await session.TickAsync(ct);
            }
            while (await timer.WaitForNextTickAsync(ct));
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
        }

        using var bound = new CancellationTokenSource(FinalTickLimit);
        try
        {
            await session.TickAsync(bound.Token);
        }
        catch (OperationCanceledException) when (bound.IsCancellationRequested)
        {
        }
    }

    private static readonly TimeSpan FinalTickLimit = TimeSpan.FromSeconds(10);

    internal Session StartSession(string cardKey, string workDir, string branch, IReadOnlyList<string> taskTitles) =>
        new(jira, workspace, logger, cardKey, workDir, branch, taskTitles);

    internal sealed class Session(IJira jira, IWorkspace workspace, ILogger logger, string cardKey, string workDir, string branch, IReadOnlyList<string> taskTitles)
    {
        private readonly HashSet<int> announced = [];
        private string? lastPushed;

        public async Task TickAsync(CancellationToken ct)
        {
            await GuardAsync(PushIfMovedAsync, ct);
            await GuardAsync(CommentNewlyCompletedAsync, ct);
        }

        private async Task GuardAsync(Func<CancellationToken, Task> step, CancellationToken ct)
        {
            try
            {
                await step(ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !ct.IsCancellationRequested)
            {
                LogTickFailure(logger, ex.Message);
            }
        }

        private async Task PushIfMovedAsync(CancellationToken ct)
        {
            var head = await workspace.HeadShaAsync(workDir, ct);
            if (lastPushed is null)
            {
                lastPushed = head;
                return;
            }
            if (head == lastPushed) return;
            await workspace.PushAsync(workDir, branch, ct);
            lastPushed = head;
            LogPushed(logger, branch, head[..Math.Min(7, head.Length)]);
        }

        private async Task CommentNewlyCompletedAsync(CancellationToken ct)
        {
            var sdd = Path.Combine(workDir, ".superpowers", "sdd");
            if (!Directory.Exists(sdd)) return;

            var completed = new SortedSet<int>();
            foreach (var file in Directory.EnumerateDirectories(sdd).Select(d => Path.Combine(d, "progress.md")).Where(File.Exists))
            {
                try
                {
                    completed.UnionWith(LedgerParser.CompletedTasks(await File.ReadAllTextAsync(file, ct)));
                }
                catch (IOException ex)
                {
                    LogLedgerBusy(logger, file, ex.Message);
                    return;
                }
            }

            foreach (var n in completed.Where(n => !announced.Contains(n)))
            {
                var text = Describe(n);
                await jira.CommentAsync(cardKey, text, ct);
                announced.Add(n);
                LogCommented(logger, cardKey, text);
            }
        }

        private string Describe(int n)
        {
            var total = taskTitles.Count == 0 ? "?" : taskTitles.Count.ToString(CultureInfo.InvariantCulture);
            var prefix = $"Task {n}/{total} done";
            return n >= 1 && n <= taskTitles.Count ? $"{prefix} — {taskTitles[n - 1]}" : prefix;
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Pushed {Branch} @ {Sha}")]
    private static partial void LogPushed(ILogger logger, string branch, string sha);

    [LoggerMessage(Level = LogLevel.Information, Message = "Commented on {Key}: {Text}")]
    private static partial void LogCommented(ILogger logger, string key, string text);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Live progress tick failed: {Message}")]
    private static partial void LogTickFailure(ILogger logger, string message);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Ledger {File} unreadable, skipping this tick: {Message}")]
    private static partial void LogLedgerBusy(ILogger logger, string file, string message);
}
