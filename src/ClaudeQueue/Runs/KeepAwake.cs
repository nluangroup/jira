using System.Runtime.InteropServices;

namespace ClaudeQueue.Runs;

/// <summary>Prevents Windows from sleeping while held; a no-op on other platforms.</summary>
public sealed partial class KeepAwake : IDisposable
{
    private const uint EsContinuous = 0x80000000;
    private const uint EsSystemRequired = 0x00000001;
    private static readonly TimeSpan JoinTimeout = TimeSpan.FromSeconds(2);

    private readonly ManualResetEventSlim release = new();
    private readonly Thread? thread;

    [LibraryImport("kernel32.dll")]
    private static partial uint SetThreadExecutionState(uint flags);

    private KeepAwake()
    {
        if (!OperatingSystem.IsWindows()) return;

        // SetThreadExecutionState is per-thread and async code hops threads, so a dedicated thread owns it.
        using var ready = new ManualResetEventSlim();
        thread = new Thread(() =>
        {
            _ = SetThreadExecutionState(EsContinuous | EsSystemRequired);
            ready.Set();
            release.Wait();
            _ = SetThreadExecutionState(EsContinuous);
        })
        { IsBackground = true, Name = "KeepAwake" };
        thread.Start();
        ready.Wait();
    }

    /// <summary>Starts holding the machine awake until the returned instance is disposed.</summary>
    public static KeepAwake Acquire() => new();

    /// <summary>Releases the hold.</summary>
    public void Dispose()
    {
        release.Set();
        thread?.Join(JoinTimeout);
    }
}
