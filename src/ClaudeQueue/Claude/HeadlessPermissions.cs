using System.Text.Json;

namespace ClaudeQueue.Claude;

/// <summary>Tool permissions passed to headless Claude on its command line, so they apply in fresh, untrusted clones.</summary>
/// <param name="Allow">Allowed tool rules from the repo's settings.</param>
/// <param name="Deny">Denied tool rules: the repo's plus <see cref="HeadlessPermissions.FixedDenies"/>.</param>
/// <param name="Warning">Set when the repo's settings could not be used; null otherwise.</param>
public sealed record HeadlessPermissions(IReadOnlyList<string> Allow, IReadOnlyList<string> Deny, string? Warning)
{
    private const string FixedBlocksOnly = "ran with the service's fixed blocks only";

    /// <summary>Rules denied on every run regardless of the repo's settings.</summary>
    public static IReadOnlyList<string> FixedDenies { get; } =
    [
        "Bash(git push:*)", "PowerShell(git push:*)",
        "Edit(docs/superpowers/**)", "Write(docs/superpowers/**)",
        "Edit(.claude/**)", "Write(.claude/**)",
        "Edit(verify.ps1)", "Write(verify.ps1)",
        "Edit(tools/smoke.mjs)", "Write(tools/smoke.mjs)",
    ];

    /// <summary>Reads <c>.claude/settings.json</c> from the working copy and adds the fixed denies. Never throws for file content problems.</summary>
    /// <param name="workDir">Repository working directory.</param>
    /// <returns>The permissions to pass to Claude.</returns>
    public static HeadlessPermissions Load(string workDir)
    {
        var path = Path.Combine(workDir, ".claude", "settings.json");
        if (!File.Exists(path))
            return FixedOnly($"No .claude/settings.json in the repo; {FixedBlocksOnly}");
        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllText(path));
            if (doc.RootElement.ValueKind != JsonValueKind.Object) return Unreadable("root is not an object");
            if (!doc.RootElement.TryGetProperty("permissions", out var perms)) return From([], []);
            if (perms.ValueKind != JsonValueKind.Object) return Unreadable("permissions is not an object");
            return From(Strings(perms, "allow"), Strings(perms, "deny"));
        }
        catch (JsonException)
        {
            return Unreadable("invalid JSON");
        }
        catch (FormatException ex)
        {
            return Unreadable(ex.Message);
        }
        catch (IOException ex)
        {
            return Unreadable(ex.Message);
        }
        catch (UnauthorizedAccessException ex)
        {
            return Unreadable(ex.Message);
        }
    }

    private static HeadlessPermissions Unreadable(string reason) =>
        FixedOnly($"Could not read .claude/settings.json ({reason}); {FixedBlocksOnly}");

    private static HeadlessPermissions FixedOnly(string warning) => new([], FixedDenies, warning);

    private static HeadlessPermissions From(List<string> allow, List<string> deny) =>
        new(allow.Distinct().ToList(), deny.Concat(FixedDenies).Distinct().ToList(), null);

    private static List<string> Strings(JsonElement perms, string name)
    {
        if (!perms.TryGetProperty(name, out var arr)) return [];
        if (arr.ValueKind != JsonValueKind.Array) throw new FormatException($"permissions.{name} is not an array");
        return [.. arr.EnumerateArray().Select(e => e.ValueKind == JsonValueKind.String
            ? e.GetString()!
            : throw new FormatException($"permissions.{name} has a non-string entry"))];
    }
}
