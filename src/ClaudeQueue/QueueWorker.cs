using ClaudeQueue.Jira;
using ClaudeQueue.Runs;
using Microsoft.Extensions.Options;

namespace ClaudeQueue;

/// <summary>Polls Jira for the next ready card and runs it, one card at a time.</summary>
/// <param name="jira">Jira port.</param>
/// <param name="runner">Runs a single card.</param>
/// <param name="options">Queue settings.</param>
/// <param name="logger">Logger.</param>
public sealed partial class QueueWorker(IJira jira, PlanRunner runner, IOptions<QueueOptions> options, ILogger<QueueWorker> logger) : BackgroundService
{
    /// <inheritdoc />
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var o = options.Value;
        LogWatching(logger, o.ProjectKey, o.ReadyStatus, (int)o.PollInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            var wait = true;
            try
            {
                if (await jira.GetNextReadyAsync(stoppingToken) is { } card)
                {
                    LogPickedUp(logger, card.Key, card.Summary);
                    wait = false;
                    await runner.RunAsync(card, stoppingToken);
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException || !stoppingToken.IsCancellationRequested)
            {
                wait = true;
                LogLoopFailed(logger, ex);
            }

            if (!wait) continue;
            try
            {
                await Task.Delay(o.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Watching {Project} for \"{Status}\" every {Seconds}s")]
    private static partial void LogWatching(ILogger logger, string project, string status, int seconds);

    [LoggerMessage(Level = LogLevel.Information, Message = "Picked up {Key}: {Summary}")]
    private static partial void LogPickedUp(ILogger logger, string key, string summary);

    [LoggerMessage(Level = LogLevel.Error, Message = "Queue loop failed, retrying after the poll interval")]
    private static partial void LogLoopFailed(ILogger logger, Exception ex);
}
