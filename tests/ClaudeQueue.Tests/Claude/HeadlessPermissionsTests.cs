using ClaudeQueue.Claude;

namespace ClaudeQueue.Tests.Claude;

public class HeadlessPermissionsTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cq-perm-" + Guid.NewGuid().ToString("N"));

    public HeadlessPermissionsTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        Directory.Delete(_dir, recursive: true);
        GC.SuppressFinalize(this);
    }

    private void WriteSettings(string json)
    {
        Directory.CreateDirectory(Path.Combine(_dir, ".claude"));
        File.WriteAllText(Path.Combine(_dir, ".claude", "settings.json"), json);
    }

    [Fact] public void Reads_allow_and_deny_and_appends_fixed_denies()
    {
        WriteSettings("""{"permissions":{"allow":["Bash(dotnet:*)","Read"],"deny":["Bash(rm:*)"]}}""");
        var p = HeadlessPermissions.Load(_dir);
        Assert.Equal(["Bash(dotnet:*)", "Read"], p.Allow);
        Assert.Equal(["Bash(rm:*)", .. HeadlessPermissions.FixedDenies], p.Deny);
        Assert.Null(p.Warning);
    }

    [Fact] public void Duplicates_are_removed_keeping_order()
    {
        WriteSettings("""{"permissions":{"allow":["A","B","A"],"deny":["Bash(git push:*)","X","X"]}}""");
        var p = HeadlessPermissions.Load(_dir);
        Assert.Equal(["A", "B"], p.Allow);
        Assert.Equal(["Bash(git push:*)", "X", .. HeadlessPermissions.FixedDenies.Skip(1)], p.Deny);
    }

    [Fact] public void Missing_file_gives_fixed_denies_and_warning()
    {
        var p = HeadlessPermissions.Load(_dir);
        Assert.Empty(p.Allow);
        Assert.Equal(HeadlessPermissions.FixedDenies, p.Deny);
        Assert.Equal("No .claude/settings.json in the repo; ran with the service's fixed blocks only", p.Warning);
    }

    [Theory]
    [InlineData("{not json")]
    [InlineData("[]")]
    [InlineData("""{"permissions":{"allow":"Read"}}""")]
    [InlineData("""{"permissions":{"deny":[1]}}""")]
    [InlineData("""{"permissions":5}""")]
    public void Invalid_or_wrongly_shaped_file_warns_without_throwing(string json)
    {
        WriteSettings(json);
        var p = HeadlessPermissions.Load(_dir);
        Assert.Empty(p.Allow);
        Assert.Equal(HeadlessPermissions.FixedDenies, p.Deny);
        Assert.StartsWith("Could not read .claude/settings.json (", p.Warning);
        Assert.EndsWith("); ran with the service's fixed blocks only", p.Warning);
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"permissions":{}}""")]
    public void Valid_file_without_entries_has_no_warning(string json)
    {
        WriteSettings(json);
        var p = HeadlessPermissions.Load(_dir);
        Assert.Empty(p.Allow);
        Assert.Equal(HeadlessPermissions.FixedDenies, p.Deny);
        Assert.Null(p.Warning);
    }

    [Fact] public void Fixed_denies_are_exact() =>
        Assert.Equal(["Bash(git push:*)", "PowerShell(git push:*)", "Edit(docs/superpowers/**)", "Write(docs/superpowers/**)",
                      "Edit(.claude/**)", "Write(.claude/**)", "Edit(verify.ps1)", "Write(verify.ps1)",
                      "Edit(tools/smoke.mjs)", "Write(tools/smoke.mjs)"], HeadlessPermissions.FixedDenies);
}
