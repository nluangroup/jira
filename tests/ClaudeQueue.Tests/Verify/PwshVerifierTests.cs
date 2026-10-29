using ClaudeQueue.Verify;
using Microsoft.Extensions.Options;

namespace ClaudeQueue.Tests.Verify;

public class PwshVerifierTests
{
    private static IOptions<QueueOptions> Opts() => Options.Create(new QueueOptions());

    private static string EmptyTempDir()
    {
        var root = Path.Combine(Path.GetTempPath(), "cq-verify-" + Guid.NewGuid().ToString("N"));
        var dir = Path.Combine(root, "work");
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static async Task<VerifyResult> RunWith(string script)
    {
        var dir = EmptyTempDir();
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "verify.ps1"), script);
            return await new PwshVerifier(Opts()).RunAsync(dir, default);
        }
        finally { Directory.Delete(Path.GetDirectoryName(dir)!, true); }
    }

    [Fact] public async Task Verifier_passes_with_counts()
    {
        var r = await RunWith("Write-Output \"Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 1 s - T.dll (net10.0)\"; exit 0");
        Assert.True(r.Passed); Assert.Equal((5, 0, 5), (r.TestsPassed, r.TestsFailed, r.TestsTotal)); Assert.Empty(r.Skipped);
    }

    [Fact] public async Task Verifier_reports_skipped_checks() =>
        Assert.Equal(["SKIPPED: coverage"], (await RunWith("Write-Output 'SKIPPED: coverage'; exit 0")).Skipped);

    [Fact] public async Task Verifier_fails_on_nonzero_exit() => Assert.False((await RunWith("exit 1")).Passed);

    [Fact] public async Task Verifier_writes_log_next_to_workdir_and_keeps_last_20_lines()
    {
        var dir = EmptyTempDir();
        var root = Path.GetDirectoryName(dir)!;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "verify.ps1"), "1..30 | ForEach-Object { \"line $_\" }; exit 1");
            var r = await new PwshVerifier(Opts()).RunAsync(dir, default);
            Assert.False(r.Passed);
            var detail = r.Detail.Split('\n').Select(l => l.TrimEnd('\r')).ToList();
            Assert.Equal(20, detail.Count);
            Assert.Equal("line 11", detail[0]); Assert.Equal("line 30", detail[^1]);
            Assert.Contains("line 1", await File.ReadAllTextAsync(Path.Combine(root, "work-verify.log")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact] public async Task Verifier_times_out_kills_script_and_still_writes_log()
    {
        var dir = EmptyTempDir();
        var root = Path.GetDirectoryName(dir)!;
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "verify.ps1"), "Write-Output 'started'; Start-Sleep -Seconds 60");
            var options = Options.Create(new QueueOptions { VerifyTimeout = TimeSpan.FromSeconds(2) });
            var r = await new PwshVerifier(options).RunAsync(dir, default);
            Assert.False(r.Passed);
            Assert.Equal("verify timed out after 0m", r.Detail);
            Assert.Contains("started", await File.ReadAllTextAsync(Path.Combine(root, "work-verify.log")));
        }
        finally { Directory.Delete(root, true); }
    }

    [Fact] public async Task Verifier_runs_with_msbuild_node_reuse_disabled()
    {
        var r = await RunWith("Write-Output \"nr=$env:MSBUILDDISABLENODEREUSE srv=$env:DOTNET_CLI_USE_MSBUILD_SERVER\"; exit 0");
        Assert.Contains("nr=1 srv=0", r.Detail);
    }

    [Fact] public async Task Missing_verify_script_fails_with_reason()
    {
        var dir = EmptyTempDir();
        try
        {
            var r = await new PwshVerifier(Opts()).RunAsync(dir, default);
            Assert.False(r.Passed); Assert.Equal("verify.ps1 not found", r.Detail);
        }
        finally { Directory.Delete(Path.GetDirectoryName(dir)!, true); }
    }
}
