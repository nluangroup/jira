using System.Globalization;
using ClaudeQueue.Processes;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Claude;

/// <summary>Runs the Claude CLI headless and maps its stream-json output.</summary>
/// <param name="options">Queue settings.</param>
/// <param name="logger">Logger.</param>
public sealed partial class ClaudeCli(IOptions<QueueOptions> options, ILogger<ClaudeCli> logger) : IClaude
{
    private static readonly Dictionary<string, string> BaseEnvironment = new()
    {
        ["MSBUILDDISABLENODEREUSE"] = "1",
        ["DOTNET_CLI_USE_MSBUILD_SERVER"] = "0",
    };

    /// <summary>Extra environment variables for the child process; a test hook.</summary>
    internal IReadOnlyDictionary<string, string>? ExtraEnvironment { get; init; }

    /// <summary>Builds the Claude CLI arguments.</summary>
    /// <param name="prompt">The prompt.</param>
    /// <param name="maxBudgetUsd">Spend limit in USD.</param>
    /// <param name="permissions">Tool permissions to pass on the command line.</param>
    /// <returns>The argument list.</returns>
    public static IReadOnlyList<string> BuildArgs(string prompt, decimal maxBudgetUsd, HeadlessPermissions permissions)
    {
        List<string> args =
        [
            "-p", prompt, "--output-format", "stream-json", "--verbose", "--permission-mode", "dontAsk",
            "--max-budget-usd", maxBudgetUsd.ToString(CultureInfo.InvariantCulture),
        ];
        if (permissions.Allow.Count > 0) args.AddRange(["--allowedTools", .. permissions.Allow]);
        if (permissions.Deny.Count > 0) args.AddRange(["--disallowedTools", .. permissions.Deny]);
        return args;
    }

    /// <inheritdoc />
    public async Task<ClaudeResult> RunAsync(string workDir, string prompt, Action<StreamEvent> onEvent, CancellationToken ct)
    {
        var o = options.Value;
        var log = Path.Combine(Path.GetDirectoryName(workDir)!, Path.GetFileName(workDir) + "-claude.jsonl");
        var env = new Dictionary<string, string>(BaseEnvironment);
        if (ExtraEnvironment is not null)
            foreach (var (k, v) in ExtraEnvironment) env[k] = v;

        var permissions = HeadlessPermissions.Load(workDir);
        LogPermissions(logger, permissions.Allow.Count, permissions.Deny.Count);
        if (permissions.Warning is not null) LogPermissionsWarning(logger, permissions.Warning);

        RunFinished? finished = null;
        await using var raw = new StreamWriter(log, append: false) { AutoFlush = true };
        void OnLine(string line)
        {
            raw.WriteLine(line);
            if (StreamEventParser.Parse(line) is not { } ev) return;
            if (ev is RunFinished rf) finished = rf;
            try { onEvent(ev); }
            catch (Exception ex) { LogProgressFailed(logger, ex, ev.GetType().Name); }
        }

        try
        {
            var exitCode = await ProcessRunner.StreamAsync(o.ClaudeExecutable, [.. o.ClaudeExecutableArgs, .. BuildArgs(prompt, o.ClaudeMaxBudgetUsd, permissions)],
                workDir, OnLine, o.ClaudeTimeout, ct, env);
            var exit = exitCode != 0 || finished is not { IsError: false } ? ClaudeExit.Failed : ClaudeExit.Succeeded;
            if (exit == ClaudeExit.Failed)
                LogFailed(logger, exitCode, finished is null ? "missing" : finished.IsError ? "error" : "ok");
            return new ClaudeResult(exit, finished?.CostUsd, finished?.ResultText) { PermissionsWarning = permissions.Warning };
        }
        catch (TimeoutException ex)
        {
            LogTimedOut(logger, ex.Message);
            return new ClaudeResult(ClaudeExit.TimedOut, finished?.CostUsd, finished?.ResultText) { PermissionsWarning = permissions.Warning };
        }
    }

    [LoggerMessage(Level = LogLevel.Information, Message = "Permissions: {Allowed} allowed, {Denied} denied")]
    private static partial void LogPermissions(ILogger logger, int allowed, int denied);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Permissions: {Warning}")]
    private static partial void LogPermissionsWarning(ILogger logger, string warning);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Progress callback failed for {Event}")]
    private static partial void LogProgressFailed(ILogger logger, Exception ex, string @event);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Claude failed: exit code {ExitCode}, result {Result}")]
    private static partial void LogFailed(ILogger logger, int exitCode, string result);

    [LoggerMessage(Level = LogLevel.Warning, Message = "Claude timed out: {Message}")]
    private static partial void LogTimedOut(ILogger logger, string message);
}
