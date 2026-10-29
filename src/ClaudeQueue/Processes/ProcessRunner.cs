using System.ComponentModel;
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
    /// <remarks>Completes when the process exits; output still buffered is given a short grace period, so a grandchild that keeps the pipes open cannot stall the run.</remarks>
    /// <param name="file">Executable name or path.</param>
    /// <param name="args">Arguments, passed individually.</param>
    /// <param name="workDir">Working directory.</param>
    /// <param name="onStdOutLine">Called for every stdout line.</param>
    /// <param name="timeout">Wall-clock limit; the process tree is killed when it elapses.</param>
    /// <param name="ct">Cancellation token; the process tree is killed when it fires.</param>
    /// <param name="environment">Extra environment variables for the child, or null.</param>
    /// <returns>The process exit code.</returns>
    /// <exception cref="TimeoutException">The timeout elapsed.</exception>
    /// <exception cref="OperationCanceledException"><paramref name="ct"/> was cancelled.</exception>
    public static async Task<int> StreamAsync(string file, IEnumerable<string> args, string workDir, Action<string> onStdOutLine, TimeSpan timeout, CancellationToken ct, IReadOnlyDictionary<string, string>? environment = null)
    {
        using var proc = Start(file, args, workDir, environment);
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        using var readers = new CancellationTokenSource();
        linked.CancelAfter(timeout);
        var stdOut = Task.Run(async () =>
        {
            while (await proc.StandardOutput.ReadLineAsync(readers.Token) is { } line)
                onStdOutLine(line);
        }, CancellationToken.None);
        var stdErr = proc.StandardError.ReadToEndAsync(readers.Token);
        var drained = Task.WhenAll(stdOut, stdErr);
        try
        {
            await proc.WaitForExitAsync(linked.Token);
            if (await Task.WhenAny(drained, Task.Delay(DrainGrace, ct)) == drained)
                await drained;
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
        finally
        {
            await readers.CancelAsync();
            try { await drained; }
            catch (Exception) { }
        }
    }

    private static readonly TimeSpan DrainGrace = TimeSpan.FromSeconds(2);

    private static Process Start(string file, IEnumerable<string> args, string workDir, IReadOnlyDictionary<string, string>? environment = null)
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
        if (environment is not null)
            foreach (var (k, v) in environment) info.Environment[k] = v;
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
        catch (Exception ex) when (ex is InvalidOperationException or Win32Exception or AggregateException)
        {
            await Console.Error.WriteLineAsync($"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] WARN: could not kill process tree: {ex.Message}");
        }
        using var bound = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        try { await proc.WaitForExitAsync(bound.Token); }
        catch (OperationCanceledException) { }
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
