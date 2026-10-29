using System.Diagnostics;
using System.Text;

namespace ClaudeQueue.Processes;

/// <summary>Captured result of a finished process.</summary>
/// <param name="ExitCode">Process exit code.</param>
/// <param name="StdOut">Everything written to standard output.</param>
/// <param name="StdErr">Everything written to standard error.</param>
public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);

/// <summary>Runs child processes with argument lists, cancellation and process-tree kill.</summary>
public static class ProcessRunner
{
    /// <summary>Runs a process to completion, capturing output; kills the process tree if cancelled.</summary>
    /// <param name="file">Executable name or path.</param>
    /// <param name="args">Arguments, passed individually (never joined into one string).</param>
    /// <param name="workDir">Working directory.</param>
    /// <param name="ct">Cancellation token.</param>
    public static async Task<ProcessResult> RunAsync(string file, IEnumerable<string> args, string workDir, CancellationToken ct)
    {
        using var proc = Start(file, args, workDir);
        try
        {
            var stdOut = proc.StandardOutput.ReadToEndAsync(ct);
            var stdErr = proc.StandardError.ReadToEndAsync(ct);
            await proc.WaitForExitAsync(ct);
            return new ProcessResult(proc.ExitCode, await stdOut, await stdErr);
        }
        catch
        {
            await KillTreeAsync(proc);
            throw;
        }
    }

    /// <summary>Runs a process, delivering each stdout line to a callback as it arrives.</summary>
    /// <param name="file">Executable name or path.</param>
    /// <param name="args">Arguments, passed individually.</param>
    /// <param name="workDir">Working directory.</param>
    /// <param name="onStdOutLine">Called for every stdout line.</param>
    /// <param name="timeout">Wall-clock limit; the process tree is killed when it elapses.</param>
    /// <param name="ct">Cancellation token; the process tree is killed when it fires.</param>
    /// <returns>The process exit code.</returns>
    /// <exception cref="TimeoutException">The timeout elapsed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    public static async Task<int> StreamAsync(string file, IEnumerable<string> args, string workDir, Action<string> onStdOutLine, TimeSpan timeout, CancellationToken ct)
    {
        using var proc = Start(file, args, workDir);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);
        var stdErr = proc.StandardError.ReadToEndAsync(CancellationToken.None);
        try
        {
            while (await proc.StandardOutput.ReadLineAsync(linked.Token) is { } line)
                onStdOutLine(line);
            await proc.WaitForExitAsync(linked.Token);
            await stdErr;
            return proc.ExitCode;
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            await KillTreeAsync(proc);
            throw new TimeoutException($"{file} did not finish within {timeout}.");
        }
        catch
        {
            await KillTreeAsync(proc);
            throw;
        }
    }

    private static Process Start(string file, IEnumerable<string> args, string workDir)
    {
        var info = new ProcessStartInfo(file)
        {
            WorkingDirectory = workDir,
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardInput = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8,
        };
        foreach (var a in args) info.ArgumentList.Add(a);
        var proc = Process.Start(info) ?? throw new InvalidOperationException($"Failed to start {file}.");
        proc.StandardInput.Close();
        return proc;
    }

    private static async Task KillTreeAsync(Process proc)
    {
        try
        {
            if (!proc.HasExited) proc.Kill(entireProcessTree: true);
        }
        catch (InvalidOperationException) { }
        await proc.WaitForExitAsync(CancellationToken.None);
    }
}

internal static class Backoff
{
    public static TimeSpan Default(int retry) => TimeSpan.FromSeconds(Math.Pow(2, retry));

    /// <summary>Runs <paramref name="attempt"/> up to 4 times (initial plus 3 retries), waiting delay(1..3) between failures; the last failure propagates.</summary>
    public static async Task RetryAsync(Func<Task> attempt, Func<int, TimeSpan> delay, Action<int, Exception> onRetry, CancellationToken ct)
    {
        for (var retry = 1; ; retry++)
        {
            try
            {
                await attempt();
                return;
            }
            catch (InvalidOperationException ex) when (retry <= 3)
            {
                onRetry(retry, ex);
                await Task.Delay(delay(retry), ct);
            }
        }
    }
}
