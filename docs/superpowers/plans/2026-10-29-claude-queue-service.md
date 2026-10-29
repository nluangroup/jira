# Claude Queue Service Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A .NET 10 worker in `nluangroup/jira` that takes the top Jira card in "Ready for Claude", runs Claude headless on the approved Superpowers plan in a fresh clone of `nluangroup/agentic-demo`, shows live progress (console, GitHub pushes, Jira comments), verifies independently, opens a PR and moves the card to In Review.

**Architecture:** A `BackgroundService` polls Jira and hands each card to `PlanRunner`, which orchestrates five narrow ports (`IJira`, `IWorkspace`, `IClaude`, `IVerifier`, `IPullRequests`) plus `LiveProgress` running alongside Claude. All parsing and formatting (card block, stream events, ledger, test summary, report) is pure static code with unit tests; ports are thin wrappers over `HttpClient`, `git`, `gh`, `claude` and `pwsh`.

**Tech Stack:** .NET 10 (`net10.0`), Worker Service, `System.Text.Json`, `Microsoft.Extensions.Http.Resilience`, xUnit, git, GitHub CLI (`gh`), Claude Code CLI 2.1.x, PowerShell 7.

**Spec:** `docs/superpowers/specs/2026-10-29-claude-jira-queue-design.md`

**Out of scope (separate plan):** the `agentic-demo` repo setup (demo app, `verify.ps1`, project `CLAUDE.md`, `.claude/settings.json`, "queue this plan" skill). This plan's tests never touch `agentic-demo`; Task 10's smoke test needs it.

## Global Constraints

- Target `net10.0`. `async`/`await` throughout; never `.Result`/`.Wait()`; `CancellationToken` threaded through every async chain.
- All public types and members have XML doc comments. `System.Text.Json` only.
- Console log line format: `[yyyy-MM-dd HH:mm:ss] LEVEL: message`.
- Transient Jira/GitHub failures retry with exponential backoff; connection errors are never silently swallowed (log them).
- JQL: `project = SCRUM AND status = "Ready for Claude" ORDER BY Rank ASC`. Poll interval default 5 s.
- Status names: `Ready for Claude`, `In Progress`, `In Review`. The service never transitions to `Done`.
- Branch: `plan/SCRUM-<n>-<slug>`. PR base `main`; draft if verification failed or Claude failed/timed out; no push/PR when there are no new commits.
- Claude invocation: `claude -p <prompt> --output-format stream-json --verbose --permission-mode dontAsk --max-budget-usd <n> --disallowedTools "Bash(git push:*)"`; wall-clock timeout enforced by killing the process tree. (`--max-turns` is not offered by the installed CLI 2.1.312; budget + timeout are the limits.)
- Report outcomes, exact text: `Verified`, `Verification failed`, `Claude failed`, `Timed out`, `Invalid card`.
- Every run ends with a report comment and a transition to `In Review`, whatever failed. A failure in a live-progress channel is logged and never stops the run.
- Only cards naming the configured demo repo (`nluangroup/agentic-demo`) are run; anything else is `Invalid card`.

## Review Focus

- **Card description edited by hand** (block lines moved, extra prose, wrong-case keys, a short SHA): parse tolerantly by line; anything missing or malformed → `Invalid card` with the reason, never an exception. Test in Task 2.
- **Claude runs past the timeout**: the process tree is killed, outcome `Timed out`, and whatever commits exist still get a draft PR. Test in Task 7 and Task 9.
- **A transition the workflow does not offer** (e.g. status renamed on the board): `TransitionAsync` throws a message naming the card and the missing status; `PlanRunner` still posts the report. Test in Task 5 and Task 9.
- **Push fails mid-run** (network blip during live pushes): logged, run continues, final push retried at the end. Test in Task 8.
- **`verify.ps1` missing from the demo repo**: `Verification failed` with "verify.ps1 not found", not a crash. Test in Task 6.

---

## File Structure

```
ClaudeQueue.slnx
src/ClaudeQueue/
  ClaudeQueue.csproj            Worker SDK, net10.0
  Program.cs                    host, DI, options, logging
  appsettings.json              non-secret config
  QueueOptions.cs               options class
  Logging/TimestampConsoleFormatter.cs
  Cards/PlanReference.cs        record + TryParse (card block)
  Cards/BranchName.cs
  Plans/PlanTaskTitles.cs       "### Task N: Title" headings
  Plans/LedgerParser.cs         "Task N: complete" lines
  Plans/RunPrompt.cs            prompt text + ExecutionMode
  Claude/StreamEvents.cs        event records + parser + console line
  Claude/IClaude.cs             port + ClaudeResult
  Claude/ClaudeCli.cs
  Jira/IJira.cs                 port + JiraCard
  Jira/JiraClient.cs
  Git/IWorkspace.cs
  Git/GitWorkspace.cs
  Git/IPullRequests.cs
  Git/GhPullRequests.cs
  Verify/IVerifier.cs           port + VerifyResult + TestSummaryParser
  Verify/PwshVerifier.cs
  Processes/ProcessRunner.cs    run + stream lines + kill tree
  Reporting/ReportBuilder.cs    RunOutcome, RunReport, Build, ExtractRulings
  Runs/LiveProgress.cs
  Runs/PlanRunner.cs
  QueueWorker.cs
tests/ClaudeQueue.Tests/
  ClaudeQueue.Tests.csproj      xUnit, net10.0
  (one test file per production file under test)
  Fakes/                        FakeJira, FakeWorkspace, FakeClaude, FakeVerifier, FakePullRequests
  Scripts/fake-claude.ps1       emits canned stream-json lines; used by ClaudeCli tests
README.md                       setup + run instructions
```

---

### Task 1: Solution scaffold, options, console log format

**Files:**
- Create: `ClaudeQueue.slnx`, `src/ClaudeQueue/ClaudeQueue.csproj`, `src/ClaudeQueue/Program.cs`, `src/ClaudeQueue/QueueOptions.cs`, `src/ClaudeQueue/appsettings.json`, `src/ClaudeQueue/Logging/TimestampConsoleFormatter.cs`, `tests/ClaudeQueue.Tests/ClaudeQueue.Tests.csproj`, `.gitignore` (dotnet template)
- Test: `tests/ClaudeQueue.Tests/Logging/TimestampConsoleFormatterTests.cs`

**Interfaces:**
- Produces:
  - `public sealed class QueueOptions` with properties (defaults in parentheses): `JiraBaseUrl` (`https://nlundgaard.atlassian.net`), `JiraEmail`, `JiraApiToken` (both from user secrets), `ProjectKey` (`SCRUM`), `ReadyStatus` (`Ready for Claude`), `InProgressStatus` (`In Progress`), `ReviewStatus` (`In Review`), `DemoRepo` (`nluangroup/agentic-demo`), `PollInterval` (`00:00:05`), `ProgressInterval` (`00:00:03`), `RunsDirectory` (`%LOCALAPPDATA%\ClaudeQueue\runs`, expanded at startup), `ClaudeExecutable` (`claude`), `ClaudeTimeout` (`00:30:00`), `ClaudeMaxBudgetUsd` (`5`), `ExecutionMode` (`SubagentDriven`), `VerifyScript` (`verify.ps1`). Section name `"Queue"`.
  - `public sealed class TimestampConsoleFormatter : ConsoleFormatter`, registered name `"timestamp"`.

- [ ] **Step 1:** `dotnet new worker -n ClaudeQueue -o src/ClaudeQueue -f net10.0`, `dotnet new xunit -n ClaudeQueue.Tests -o tests/ClaudeQueue.Tests -f net10.0`, `dotnet new sln -n ClaudeQueue` (produces `.slnx`), add both projects, add project reference tests → src, `dotnet user-secrets init` in src, add `Microsoft.Extensions.Http.Resilience` to src. Delete the template `Worker.cs`.
- [ ] **Step 2: Write the failing test**

```csharp
[Fact]
public void Formats_timestamp_level_and_message()
{
    var line = TimestampConsoleFormatter.FormatLine(
        new DateTime(2026, 10, 29, 21, 5, 9), LogLevel.Warning, "Card SCRUM-7 moved");
    Assert.Equal("[2026-10-29 21:05:09] WARNING: Card SCRUM-7 moved", line);
}

[Theory]
[InlineData(LogLevel.Information, "INFO")]
[InlineData(LogLevel.Error, "ERROR")]
[InlineData(LogLevel.Debug, "DEBUG")]
public void Uses_short_uppercase_level_names(LogLevel level, string expected) =>
    Assert.Contains($"] {expected}: ", TimestampConsoleFormatter.FormatLine(DateTime.Now, level, "x"));
```

- [ ] **Step 3:** Run `dotnet test` → FAIL (type missing).
- [ ] **Step 4:** Implement `public static string FormatLine(DateTime local, LogLevel level, string message)` and the `Write` override (uses local time; appends exception `ToString()` on the next line when present). Level names: Trace→TRACE, Debug→DEBUG, Information→INFO, Warning→WARNING, Error→ERROR, Critical→CRITICAL. In `Program.cs`: bind `QueueOptions` from `"Queue"`, `AddConsole(o => o.FormatterName = "timestamp")` + `AddConsoleFormatter<TimestampConsoleFormatter, ConsoleFormatterOptions>()`. Fill `appsettings.json` with every non-secret default above.
- [ ] **Step 5:** `dotnet build` and `dotnet test` → all pass; `dotnet run --project src/ClaudeQueue` starts and exits cleanly on Ctrl+C.
- [ ] **Step 6:** Commit `feat: scaffold ClaudeQueue worker with timestamped console logging`.

---

### Task 2: Card block, branch name, plan headings, ledger

**Files:**
- Create: `src/ClaudeQueue/Cards/PlanReference.cs`, `src/ClaudeQueue/Cards/BranchName.cs`, `src/ClaudeQueue/Plans/PlanTaskTitles.cs`, `src/ClaudeQueue/Plans/LedgerParser.cs`
- Test: `tests/ClaudeQueue.Tests/Cards/PlanReferenceTests.cs`, `.../Cards/BranchNameTests.cs`, `.../Plans/PlanTaskTitlesTests.cs`, `.../Plans/LedgerParserTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record PlanReference(string Repo, string PlanPath, string CommitSha)` with `public static bool TryParse(string? description, out PlanReference? reference, out string error)`.
  - `public static class BranchName { public static string For(string cardKey, string summary) }`
  - `public static class PlanTaskTitles { public static IReadOnlyList<string> Parse(string planMarkdown) }` — index 0 is Task 1.
  - `public static class LedgerParser { public static IReadOnlySet<int> CompletedTasks(string ledgerText) }`

The card block format (the "queue this plan" skill writes exactly this; the parser accepts it anywhere in the description):

```
Queue-Repo: nluangroup/agentic-demo
Queue-Plan: docs/superpowers/plans/2026-10-29-health-endpoint.md
Queue-Commit: 0123456789abcdef0123456789abcdef01234567
```

- [ ] **Step 1: Write the failing tests**

```csharp
// PlanReferenceTests
const string Sha = "0123456789abcdef0123456789abcdef01234567";

[Fact] public void Parses_block_surrounded_by_prose()
{
    var text = $"Adds a health endpoint.\n\nQueue-Repo: nluangroup/agentic-demo\nQueue-Plan: docs/superpowers/plans/p.md\nQueue-Commit: {Sha}\n\nEdited by hand later.";
    Assert.True(PlanReference.TryParse(text, out var r, out _));
    Assert.Equal(new PlanReference("nluangroup/agentic-demo", "docs/superpowers/plans/p.md", Sha), r);
}
[Fact] public void Keys_are_case_insensitive_and_trimmed() =>
    Assert.True(PlanReference.TryParse($"  queue-repo:  a/b \nQUEUE-PLAN: x.md\nqueue-commit: {Sha}  ", out _, out _));
[Theory]
[InlineData(null, "description is empty")]
[InlineData("Queue-Repo: a/b\nQueue-Plan: x.md", "Queue-Commit is missing")]
[InlineData("Queue-Repo: a/b\nQueue-Plan: x.md\nQueue-Commit: abc123", "Queue-Commit must be a full 40-character SHA")]
[InlineData("Queue-Plan: x.md\nQueue-Commit: 0123456789abcdef0123456789abcdef01234567", "Queue-Repo is missing")]
public void Rejects_incomplete_blocks_with_reason(string? text, string expectedError)
{
    Assert.False(PlanReference.TryParse(text, out var r, out var error));
    Assert.Null(r);
    Assert.Equal(expectedError, error);
}

// BranchNameTests
[Theory]
[InlineData("SCRUM-12", "Add a /health endpoint!", "plan/SCRUM-12-add-a-health-endpoint")]
[InlineData("SCRUM-3", "  Ærø   logging -- cleanup ", "plan/SCRUM-3-r-logging-cleanup")]
public void Builds_slugged_branch(string key, string summary, string expected) =>
    Assert.Equal(expected, BranchName.For(key, summary));
[Fact] public void Slug_is_capped_at_40_chars_without_trailing_dash() { var b = BranchName.For("SCRUM-1", new string('a', 39) + " bbbb"); Assert.Equal("plan/SCRUM-1-" + new string('a', 39), b); }

// PlanTaskTitlesTests
[Fact] public void Reads_task_headings_in_order() =>
    Assert.Equal(new[] { "Health endpoint", "Health check test" },
        PlanTaskTitles.Parse("# Plan\n### Task 1: Health endpoint\ntext\n### Task 2: Health check test\n## Other"));

// LedgerParserTests
[Fact] public void Finds_completed_tasks_only() =>
    Assert.Equal(new HashSet<int> { 1, 3 }, LedgerParser.CompletedTasks(
        "# SDD ledger — plan: p.md\nTask 1: complete (commits a..b, review clean)\nTask 2: fix round 1/5 (...)\nTask 3: complete (commits c..d, 1 parked)"));
```

- [ ] **Step 2:** `dotnet test` → FAIL (types missing).
- [ ] **Step 3:** Implement. `PlanReference.TryParse`: per-line regex `^\s*queue-(repo|plan|commit)\s*:\s*(\S+)\s*$` (ignore case, multiline); report the first missing key in order Repo, Plan, Commit; commit must match `^[0-9a-f]{40}$` (ignore case). `BranchName.For`: lowercase, every run of non-`[a-z0-9]` chars → one `-`, trim `-`, cap 40 chars then trim trailing `-`. `PlanTaskTitles`: `^###\s+Task\s+(\d+):\s*(.+)$`. `LedgerParser`: `^Task\s+(\d+):\s+complete\b`.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: parse card plan block, branch names, plan headings and ledger`.

---

### Task 3: Claude stream events and run prompt

**Files:**
- Create: `src/ClaudeQueue/Claude/StreamEvents.cs`, `src/ClaudeQueue/Plans/RunPrompt.cs`
- Test: `tests/ClaudeQueue.Tests/Claude/StreamEventsTests.cs`, `tests/ClaudeQueue.Tests/Plans/RunPromptTests.cs`

**Interfaces:**
- Produces:
  - `public abstract record StreamEvent;` `public sealed record SubagentStarted(string Description) : StreamEvent;` `public sealed record AssistantText(string Text) : StreamEvent;` `public sealed record RunFinished(bool IsError, decimal? CostUsd, string? ResultText) : StreamEvent;`
  - `public static class StreamEventParser { public static StreamEvent? Parse(string jsonLine); public static string? ToConsoleLine(StreamEvent e); }`
  - `public enum ExecutionMode { SubagentDriven, Inline }` and `public static class RunPrompt { public static string Build(PlanReference plan, ExecutionMode mode); }`

Stream-json shapes to handle (one JSON object per line): `{"type":"assistant","message":{"content":[{"type":"text","text":"..."},{"type":"tool_use","name":"Agent","input":{"description":"...","prompt":"..."}}]}}` and `{"type":"result","subtype":"success","is_error":false,"total_cost_usd":0.42,"result":"..."}`. Everything else → `null`. The subagent tool may be named `Agent` or `Task`; treat both as `SubagentStarted`. An assistant message with several content items yields the first `tool_use` subagent if present, else the first non-empty text.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Subagent_tool_use_becomes_SubagentStarted() =>
    Assert.Equal(new SubagentStarted("Implement Task 1: Health endpoint"), StreamEventParser.Parse(
        """{"type":"assistant","message":{"content":[{"type":"tool_use","name":"Agent","input":{"description":"Implement Task 1: Health endpoint","prompt":"..."}}]}}"""));
[Fact] public void Result_line_carries_cost_and_text() =>
    Assert.Equal(new RunFinished(false, 0.42m, "All done"), StreamEventParser.Parse(
        """{"type":"result","subtype":"success","is_error":false,"total_cost_usd":0.42,"result":"All done"}"""));
[Theory]
[InlineData("""{"type":"system","subtype":"init"}""")]
[InlineData("not json")]
[InlineData("")]
public void Unknown_or_bad_lines_are_ignored(string line) => Assert.Null(StreamEventParser.Parse(line));
[Fact] public void Console_line_for_subagent() =>
    Assert.Equal("Claude ▶ Implement Task 1: Health endpoint", StreamEventParser.ToConsoleLine(new SubagentStarted("Implement Task 1: Health endpoint")));
[Fact] public void Console_line_for_text_is_first_line_capped_at_120() =>
    Assert.Equal("Claude: " + new string('x', 120) + "…", StreamEventParser.ToConsoleLine(new AssistantText(new string('x', 200) + "\nsecond")));

// RunPromptTests
[Fact] public void Prompt_contains_the_non_negotiables()
{
    var p = RunPrompt.Build(new PlanReference("nluangroup/agentic-demo", "docs/superpowers/plans/p.md", new string('a', 40)), ExecutionMode.SubagentDriven);
    Assert.Contains("docs/superpowers/plans/p.md", p);
    Assert.Contains("superpowers:subagent-driven-development", p);
    Assert.Contains("do not create a worktree", p, StringComparison.OrdinalIgnoreCase);
    Assert.Contains("Stop after the final review", p);
    Assert.Contains("Rulings I made", p);
}
[Fact] public void Inline_mode_uses_executing_plans() =>
    Assert.Contains("superpowers:executing-plans", RunPrompt.Build(new PlanReference("a/b", "p.md", new string('a', 40)), ExecutionMode.Inline));
```

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement with `JsonDocument`; `catch (JsonException)` → `null`. Prompt must also state: the spec and plan are approved, do not brainstorm or redesign; do not run superpowers:finishing-a-development-branch; do not merge, push or open a PR; end with a short summary including a "Rulings I made" list.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: parse Claude stream events and build run prompt`.

---

### Task 4: Test summary parsing and report text

**Files:**
- Create: `src/ClaudeQueue/Verify/IVerifier.cs` (`VerifyResult`, `TestSummaryParser`, `IVerifier`), `src/ClaudeQueue/Reporting/ReportBuilder.cs`
- Test: `tests/ClaudeQueue.Tests/Verify/TestSummaryParserTests.cs`, `tests/ClaudeQueue.Tests/Reporting/ReportBuilderTests.cs`

**Interfaces:**
- Produces:
  - `public sealed record VerifyResult(bool Passed, int? TestsPassed, int? TestsFailed, int? TestsTotal, string Detail) { public IReadOnlyList<string> Skipped { get; init; } = []; }` — `Skipped` holds the verify script's SKIPPED lines (owner's verify contract: PASS / FAIL / SKIPPED; a skip is not a failure but must be reported).
  - `public static class TestSummaryParser { public static (int Passed, int Failed, int Total)? Parse(string output); public static IReadOnlyList<string> SkippedChecks(string output); }` — `Parse` sums every `dotnet test` summary line; `SkippedChecks` returns each output line that starts with `SKIPPED` (after optional whitespace, case-sensitive), trimmed, in order.
  - `public interface IVerifier { Task<VerifyResult> RunAsync(string workDir, CancellationToken ct); }`
  - `public enum RunOutcome { Verified, VerificationFailed, ClaudeFailed, TimedOut, InvalidCard }`
  - `public sealed record RunReport(RunOutcome Outcome, string? Branch, string? PrUrl, bool PrIsDraft, VerifyResult? Verify, string? Rulings, TimeSpan Duration, decimal? CostUsd, string? Detail);`
  - `public static class ReportBuilder { public static string Build(RunReport r); public static string? ExtractRulings(string? finalMessage); public static string Label(RunOutcome o); }`

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Sums_multiple_test_projects() =>
    Assert.Equal((13, 1, 14), TestSummaryParser.Parse(
        "Passed!  - Failed:     0, Passed:    10, Skipped:     0, Total:    10, Duration: 1 s - A.Tests.dll (net10.0)\n" +
        "Failed!  - Failed:     1, Passed:     3, Skipped:     0, Total:     4, Duration: 2 s - B.Tests.dll (net10.0)"));
[Fact] public void No_summary_returns_null() => Assert.Null(TestSummaryParser.Parse("Build FAILED."));
[Fact] public void Collects_skipped_check_lines() =>
    Assert.Equal(["SKIPPED: mutation testing — dotnet-stryker not installed", "SKIPPED: coverage"],
        TestSummaryParser.SkippedChecks("PASS: format\n  SKIPPED: mutation testing — dotnet-stryker not installed\nFAIL: nothing\nSKIPPED: coverage\nnot SKIPPED here"));
[Fact] public void Report_lists_skipped_checks_even_when_verified() {
    var t = ReportBuilder.Build(new RunReport(RunOutcome.Verified, "b", "u", false,
        new VerifyResult(true, 5, 0, 5, "") { Skipped = ["SKIPPED: coverage"] }, null, TimeSpan.Zero, null, null));
    Assert.StartsWith("✅ Verified", t); Assert.Contains("⚠️ Skipped checks:\n- SKIPPED: coverage", t); }

[Theory]
[InlineData(RunOutcome.Verified, "Verified")]
[InlineData(RunOutcome.VerificationFailed, "Verification failed")]
[InlineData(RunOutcome.ClaudeFailed, "Claude failed")]
[InlineData(RunOutcome.TimedOut, "Timed out")]
[InlineData(RunOutcome.InvalidCard, "Invalid card")]
public void Outcome_labels_match_spec(RunOutcome o, string label) => Assert.Equal(label, ReportBuilder.Label(o));

[Fact] public void Verified_report_shows_pr_tests_rulings_time_cost()
{
    var text = ReportBuilder.Build(new RunReport(RunOutcome.Verified, "plan/SCRUM-7-x", "https://github.com/nluangroup/agentic-demo/pull/3", false,
        new VerifyResult(true, 14, 0, 14, ""), "- Used 200 for healthy — spec silent — cheap to change", TimeSpan.FromSeconds(512), 1.234m, null));
    Assert.StartsWith("✅ Verified", text);
    Assert.Contains("PR: https://github.com/nluangroup/agentic-demo/pull/3", text);
    Assert.Contains("Branch: plan/SCRUM-7-x", text);
    Assert.Contains("Tests: 14/14 passed", text);
    Assert.Contains("Rulings I made:\n- Used 200 for healthy", text);
    Assert.Contains("Time: 8m 32s · Cost: $1.23", text);
}
[Fact] public void Failed_verification_marks_pr_as_draft() =>
    Assert.Contains("PR (draft):", ReportBuilder.Build(new RunReport(RunOutcome.VerificationFailed, "b", "u", true, new VerifyResult(false, 3, 1, 4, ""), null, TimeSpan.Zero, null, null)));
[Fact] public void Invalid_card_shows_reason_and_no_pr() {
    var t = ReportBuilder.Build(new RunReport(RunOutcome.InvalidCard, null, null, false, null, null, TimeSpan.Zero, null, "Queue-Commit is missing"));
    Assert.StartsWith("⛔ Invalid card", t); Assert.Contains("Queue-Commit is missing", t); Assert.DoesNotContain("PR", t); }
[Fact] public void Extracts_rulings_section_until_next_heading() =>
    Assert.Equal("- A — b — c\n- D — e — f", ReportBuilder.ExtractRulings("Done.\n\n## Rulings I made\n- A — b — c\n- D — e — f\n\n## Next steps\nx"));
[Fact] public void No_rulings_section_returns_null() => Assert.Null(ReportBuilder.ExtractRulings("Done."));
```

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement. Summary regex per line: `(Passed|Failed)!\s+-\s+Failed:\s+(\d+),\s+Passed:\s+(\d+),\s+Skipped:\s+\d+,\s+Total:\s+(\d+)`. Report layout, one item per line, omitting absent items: `<icon> <Label>` (✅ Verified, ⚠️ Verification failed, ❌ Claude failed, ⏱️ Timed out, ⛔ Invalid card), `Detail`, `PR:`/`PR (draft):`, `Branch:`, `Tests: <passed>/<total> passed` (+ `, <failed> failed` when >0; `Tests: no summary` when verify ran without one), when `Verify.Skipped` is non-empty a `⚠️ Skipped checks:` line followed by one `- <line>` per skip, blank line + `Rulings I made:` block, `Time: <m>m <s>s · Cost: $<0.00>` (cost omitted when null, cost formatted with invariant culture). `ExtractRulings`: from a line containing "Rulings I made" (any heading level or bold) to the next markdown heading or end; trimmed; null if absent or empty.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: parse test summaries and build run report`.

---

### Task 5: Jira client

**Files:**
- Create: `src/ClaudeQueue/Jira/IJira.cs`, `src/ClaudeQueue/Jira/JiraClient.cs`; modify `src/ClaudeQueue/Program.cs` (typed `HttpClient` + `AddStandardResilienceHandler()`)
- Test: `tests/ClaudeQueue.Tests/Jira/JiraClientTests.cs` (with a recording fake `HttpMessageHandler`)

**Interfaces:**
- Consumes: `QueueOptions` (Task 1).
- Produces:
  - `public sealed record JiraCard(string Key, string Summary, string? Description);`
  - `public interface IJira { Task<JiraCard?> GetNextReadyAsync(CancellationToken ct); Task TransitionAsync(string cardKey, string statusName, CancellationToken ct); Task CommentAsync(string cardKey, string text, CancellationToken ct); }`
  - `public sealed class JiraClient(HttpClient http, IOptions<QueueOptions> options) : IJira` — `http.BaseAddress` = `JiraBaseUrl`, Basic auth `email:token`.

Endpoints (REST v2, so descriptions and comments are plain text):
- `GET /rest/api/2/search/jql?jql=<url-encoded JQL>&fields=summary,description&maxResults=1` → `issues[0].key`, `.fields.summary`, `.fields.description`.
- `GET /rest/api/2/issue/{key}/transitions` → pick the transition whose `to.name` equals the status (ordinal-ignore-case); `POST` same path with `{"transition":{"id":"<id>"}}`.
- `POST /rest/api/2/issue/{key}/comment` with `{"body":"<text>"}`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public async Task Next_ready_uses_spec_jql_and_maps_card()
{
    var h = new FakeHandler().Respond(HttpStatusCode.OK,
        """{"issues":[{"key":"SCRUM-7","fields":{"summary":"Health","description":"Queue-Repo: a/b"}}]}""");
    var card = await Client(h).GetNextReadyAsync(CancellationToken.None);
    Assert.Equal(new JiraCard("SCRUM-7", "Health", "Queue-Repo: a/b"), card);
    Assert.Contains("project = SCRUM AND status = \"Ready for Claude\" ORDER BY Rank ASC",
        Uri.UnescapeDataString(h.Requests[0].RequestUri!.Query));
}
[Fact] public async Task Next_ready_returns_null_when_queue_empty() =>
    Assert.Null(await Client(new FakeHandler().Respond(HttpStatusCode.OK, """{"issues":[]}""")).GetNextReadyAsync(default));
[Fact] public async Task Transition_posts_matching_transition_id()
{
    var h = new FakeHandler()
        .Respond(HttpStatusCode.OK, """{"transitions":[{"id":"21","to":{"name":"In Progress"}},{"id":"31","to":{"name":"In Review"}}]}""")
        .Respond(HttpStatusCode.NoContent, "");
    await Client(h).TransitionAsync("SCRUM-7", "in review", default);
    Assert.Equal("""{"transition":{"id":"31"}}""", h.Bodies[1]);
}
[Fact] public async Task Missing_transition_names_card_and_status()
{
    var h = new FakeHandler().Respond(HttpStatusCode.OK, """{"transitions":[{"id":"21","to":{"name":"In Progress"}}]}""");
    var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => Client(h).TransitionAsync("SCRUM-7", "In Review", default));
    Assert.Equal("SCRUM-7 has no transition to 'In Review'", ex.Message);
}
[Fact] public async Task Comment_posts_plain_text_body()
{
    var h = new FakeHandler().Respond(HttpStatusCode.Created, "{}");
    await Client(h).CommentAsync("SCRUM-7", "Started 2026-10-29 21:00:00", default);
    Assert.Equal("""{"body":"Started 2026-10-29 21:00:00"}""", h.Bodies[0]);
}
[Fact] public async Task Non_success_status_throws_HttpRequestException() =>
    await Assert.ThrowsAsync<HttpRequestException>(() =>
        Client(new FakeHandler().Respond(HttpStatusCode.Unauthorized, "")).CommentAsync("SCRUM-7", "x", default));
```

(`FakeHandler` queues responses and records `Requests` and string `Bodies`; `Client(h)` builds a `JiraClient` with base address `https://example.atlassian.net` and default options.)

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement with `System.Text.Json`; `EnsureSuccessStatusCode()` on every response. Register in `Program.cs`: `services.AddHttpClient<IJira, JiraClient>().AddStandardResilienceHandler();` (exponential-backoff retries on transient status codes and timeouts).
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: add Jira REST client`.

---

### Task 6: Processes, git workspace, PRs, verifier

**Files:**
- Create: `src/ClaudeQueue/Processes/ProcessRunner.cs`, `src/ClaudeQueue/Git/IWorkspace.cs`, `src/ClaudeQueue/Git/GitWorkspace.cs`, `src/ClaudeQueue/Git/IPullRequests.cs`, `src/ClaudeQueue/Git/GhPullRequests.cs`, `src/ClaudeQueue/Verify/PwshVerifier.cs`
- Test: `tests/ClaudeQueue.Tests/Processes/ProcessRunnerTests.cs`, `.../Git/GitWorkspaceTests.cs`, `.../Git/GhPullRequestsTests.cs`, `.../Verify/PwshVerifierTests.cs`

**Interfaces:**
- Consumes: `PlanReference` (Task 2), `VerifyResult`, `TestSummaryParser`, `IVerifier` (Task 4), `QueueOptions` (Task 1).
- Produces:
  - `public sealed record ProcessResult(int ExitCode, string StdOut, string StdErr);`
  - `public static class ProcessRunner { public static Task<ProcessResult> RunAsync(string file, IEnumerable<string> args, string workDir, CancellationToken ct); public static Task<int> StreamAsync(string file, IEnumerable<string> args, string workDir, Action<string> onStdOutLine, TimeSpan timeout, CancellationToken ct); }` — `StreamAsync` throws `TimeoutException` after killing the process tree when `timeout` elapses; kills the tree and rethrows on cancellation. Args always via `ArgumentList`.
  - `public interface IWorkspace { Task<string> PrepareAsync(PlanReference plan, string branch, CancellationToken ct); Task<string> HeadShaAsync(string workDir, CancellationToken ct); Task<int> CommitsSinceAsync(string workDir, string baseSha, CancellationToken ct); Task PushAsync(string workDir, string branch, CancellationToken ct); }`
  - `public sealed class GitWorkspace(IOptions<QueueOptions> options, ILogger<GitWorkspace> logger) : IWorkspace` — `PrepareAsync` clones `https://github.com/<repo>.git` into `<RunsDirectory>/<yyyyMMdd-HHmmss>-<branch with / → _>`, `git checkout <sha>`, `git checkout -b <branch>`, returns the directory. `PushAsync` = `git push -u origin <branch>`, retried 3× with 2/4/8 s backoff, then throws. Any non-zero git exit throws `InvalidOperationException` containing the git command and stderr.
  - `public interface IPullRequests { Task<string> CreateAsync(string repo, string branch, string title, string body, bool draft, CancellationToken ct); }`
  - `public sealed class GhPullRequests : IPullRequests` with `public static IReadOnlyList<string> BuildArgs(string repo, string branch, string title, string body, bool draft)` → `pr create --repo <repo> --base main --head <branch> --title <title> --body <body> [--draft]`; returns the trimmed stdout (the PR URL). Retried 3× with 2/4/8 s backoff on non-zero exit, then throws with stderr.
  - `public sealed class PwshVerifier(IOptions<QueueOptions> options) : IVerifier` — runs `pwsh -NoProfile -ExecutionPolicy Bypass -File <VerifyScript>` in `workDir`, writes combined output to `<workDir>/../<dirname>-verify.log`, `Detail` = last 20 output lines; `Skipped` = `TestSummaryParser.SkippedChecks(output)`.

- [ ] **Step 1: Write the failing tests** (git tests create a temp bare repo with one commit as `origin`, using a `file://` URL; expose an `internal` overridable clone URL factory on `GitWorkspace` for this.)

```csharp
[Fact] public async Task Run_captures_exit_and_output()
{
    var r = await ProcessRunner.RunAsync("pwsh", ["-NoProfile", "-Command", "Write-Output hi; exit 3"], Path.GetTempPath(), default);
    Assert.Equal(3, r.ExitCode); Assert.Equal("hi", r.StdOut.Trim());
}
[Fact] public async Task Stream_delivers_lines_as_they_arrive()
{
    var lines = new List<string>();
    var code = await ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", "'a'; 'b'"], Path.GetTempPath(), lines.Add, TimeSpan.FromSeconds(30), default);
    Assert.Equal(0, code); Assert.Equal(["a", "b"], lines);
}
[Fact] public async Task Stream_kills_process_on_timeout() =>
    await Assert.ThrowsAsync<TimeoutException>(() => ProcessRunner.StreamAsync("pwsh", ["-NoProfile", "-Command", "Start-Sleep 60"], Path.GetTempPath(), _ => { }, TimeSpan.FromSeconds(2), default));

[Fact] public async Task Prepare_clones_checks_out_sha_and_creates_branch()  // GitWorkspaceTests
{   // origin has commits A then B; plan references A
    var dir = await ws.PrepareAsync(new PlanReference("x/y", "p.md", shaA), "plan/SCRUM-1-x", default);
    Assert.Equal(shaA, await ws.HeadShaAsync(dir, default));
    Assert.Equal("plan/SCRUM-1-x", (await Git(dir, "branch", "--show-current")).Trim());
}
[Fact] public async Task CommitsSince_counts_new_commits() { /* commit twice in dir */ Assert.Equal(2, await ws.CommitsSinceAsync(dir, shaA, default)); }
[Fact] public async Task Push_publishes_branch_to_origin() { await ws.PushAsync(dir, "plan/SCRUM-1-x", default); Assert.Contains("plan/SCRUM-1-x", await Git(bareOrigin, "branch")); }
[Fact] public async Task Prepare_with_unknown_sha_throws_with_git_stderr() =>
    Assert.Contains("git checkout", (await Assert.ThrowsAsync<InvalidOperationException>(() => ws.PrepareAsync(new PlanReference("x/y", "p.md", new string('f', 40)), "b", default))).Message);

[Fact] public void Gh_args_for_draft() =>   // GhPullRequestsTests
    Assert.Equal(["pr", "create", "--repo", "nluangroup/agentic-demo", "--base", "main", "--head", "plan/SCRUM-1-x", "--title", "T", "--body", "B", "--draft"],
        GhPullRequests.BuildArgs("nluangroup/agentic-demo", "plan/SCRUM-1-x", "T", "B", draft: true));

[Fact] public async Task Verifier_passes_with_counts()   // PwshVerifierTests: temp dir with a verify.ps1
{   // verify.ps1: Write-Output "Passed!  - Failed:     0, Passed:     5, Skipped:     0, Total:     5, Duration: 1 s - T.dll (net10.0)"; exit 0
    Assert.True(r.Passed); Assert.Equal((5, 0, 5), (r.TestsPassed, r.TestsFailed, r.TestsTotal)); Assert.Empty(r.Skipped);
}
[Fact] public async Task Verifier_reports_skipped_checks() =>
    Assert.Equal(["SKIPPED: coverage"], (await RunWith("Write-Output 'SKIPPED: coverage'; exit 0")).Skipped);
[Fact] public async Task Verifier_fails_on_nonzero_exit() => Assert.False((await RunWith("exit 1")).Passed);
[Fact] public async Task Missing_verify_script_fails_with_reason()
{
    var r = await new PwshVerifier(Opts()).RunAsync(EmptyTempDir(), default);
    Assert.False(r.Passed); Assert.Equal("verify.ps1 not found", r.Detail);
}
```

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement. `StreamAsync` reads stdout line-by-line with `ReadLineAsync`, drains stderr concurrently, uses a linked CTS with `CancelAfter(timeout)`, and `Kill(entireProcessTree: true)` then `WaitForExitAsync(CancellationToken.None)` on cancel/timeout. Clone uses `gh`-authenticated https (git credential helper from `gh auth setup-git`, documented in README).
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: add process runner, git workspace, gh PRs and verifier`.

---

### Task 7: Claude CLI port

**Files:**
- Create: `src/ClaudeQueue/Claude/IClaude.cs`, `src/ClaudeQueue/Claude/ClaudeCli.cs`, `tests/ClaudeQueue.Tests/Scripts/fake-claude.ps1` (copied to output)
- Test: `tests/ClaudeQueue.Tests/Claude/ClaudeCliTests.cs`

**Interfaces:**
- Consumes: `ProcessRunner.StreamAsync` (Task 6), `StreamEventParser`, `StreamEvent`, `RunFinished` (Task 3), `QueueOptions` (Task 1).
- Produces:
  - `public enum ClaudeExit { Succeeded, Failed, TimedOut }`
  - `public sealed record ClaudeResult(ClaudeExit Exit, decimal? CostUsd, string? FinalMessage);`
  - `public interface IClaude { Task<ClaudeResult> RunAsync(string workDir, string prompt, Action<StreamEvent> onEvent, CancellationToken ct); }`
  - `public sealed class ClaudeCli(IOptions<QueueOptions> options, ILogger<ClaudeCli> logger) : IClaude` with `public static IReadOnlyList<string> BuildArgs(string prompt, decimal maxBudgetUsd)`.

`fake-claude.ps1` takes a mode as first arg via env var `FAKE_CLAUDE_MODE` (`success` | `error` | `hang`) and prints canned stream-json lines; tests set `ClaudeExecutable` to `pwsh` with the script path prepended via an options property `ClaudeExecutableArgs` (`string[]`, default empty) — add it to `QueueOptions`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public void Args_match_spec() =>
    Assert.Equal(["-p", "PROMPT", "--output-format", "stream-json", "--verbose", "--permission-mode", "dontAsk",
                  "--max-budget-usd", "5", "--disallowedTools", "Bash(git push:*)"],
        ClaudeCli.BuildArgs("PROMPT", 5m));
[Fact] public async Task Success_stream_yields_events_cost_and_final_message()
{
    var events = new List<StreamEvent>();
    var r = await Cli("success").RunAsync(TempDir(), "p", events.Add, default);
    Assert.Equal(ClaudeExit.Succeeded, r.Exit); Assert.Equal(0.42m, r.CostUsd); Assert.Equal("All done", r.FinalMessage);
    Assert.Contains(new SubagentStarted("Implement Task 1: Health endpoint"), events);
}
[Fact] public async Task Error_result_is_Failed() => Assert.Equal(ClaudeExit.Failed, (await Cli("error").RunAsync(TempDir(), "p", _ => { }, default)).Exit);
[Fact] public async Task Hang_past_timeout_is_TimedOut() =>   // options.ClaudeTimeout = 2 s
    Assert.Equal(ClaudeExit.TimedOut, (await Cli("hang", timeout: TimeSpan.FromSeconds(2)).RunAsync(TempDir(), "p", _ => { }, default)).Exit);
```

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement. Budget formatted with invariant culture. Each stdout line → `StreamEventParser.Parse`; non-null events go to `onEvent`; the last `RunFinished` gives cost and message. Exit: `TimeoutException` → `TimedOut`; non-zero exit or `RunFinished.IsError` or no `RunFinished` → `Failed`; else `Succeeded`. Save every raw line to `<workDir>/../<dirname>-claude.jsonl`. Exceptions from `onEvent` are caught and logged (a progress failure never stops the run).
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: run Claude CLI headless with streamed events`.

---

### Task 8: Live progress

**Files:**
- Create: `src/ClaudeQueue/Runs/LiveProgress.cs`, `tests/ClaudeQueue.Tests/Fakes/FakeJira.cs`, `tests/ClaudeQueue.Tests/Fakes/FakeWorkspace.cs`
- Test: `tests/ClaudeQueue.Tests/Runs/LiveProgressTests.cs`

**Interfaces:**
- Consumes: `IJira` (Task 5), `IWorkspace` (Task 6), `LedgerParser` (Task 2).
- Produces: `public sealed class LiveProgress(IJira jira, IWorkspace workspace, ILogger<LiveProgress> logger)` with `public Task RunAsync(string cardKey, string workDir, string branch, IReadOnlyList<string> taskTitles, TimeSpan interval, CancellationToken ct)` — loops until `ct` is cancelled, then returns normally (no `OperationCanceledException` escapes).

Each tick: (1) `HeadShaAsync`; if it changed since the last tick and differs from the starting head → `PushAsync`, log `Pushed <branch> @ <sha7>`. (2) Read every `<workDir>/.superpowers/sdd/*/progress.md`; for each completed task number not yet announced → `CommentAsync(cardKey, "Task <n>/<total> done — <title>")` (title from `taskTitles[n-1]`, or `Task <n>/<total> done` if out of range; `<total>` = `taskTitles.Count`, or `?` when empty). Every failure inside a tick is logged as WARNING and the loop continues; a failed push is retried on the next tick.

- [ ] **Step 1: Write the failing tests** (fakes record calls; `FakeWorkspace.Heads` is a queue of SHAs to return; `FakeWorkspace.FailNextPush` makes one push throw.)

```csharp
[Fact] public async Task Pushes_when_head_moves()
{
    var ws = new FakeWorkspace(heads: ["a", "a", "b", "b", "c"]);
    await RunTicks(ws, new FakeJira(), ticks: 5);
    Assert.Equal(2, ws.Pushes.Count);
}
[Fact] public async Task Comments_each_completed_task_once()
{
    var dir = TempDir(); WriteLedger(dir, "Task 1: complete (commits a..b, review clean)");
    var jira = new FakeJira(); var run = Start(dir, jira, titles: ["Health endpoint", "Health test"]);
    await Tick(); WriteLedger(dir, "Task 1: complete (...)\nTask 2: complete (...)"); await Tick(); await Tick();
    Assert.Equal(["Task 1/2 done — Health endpoint", "Task 2/2 done — Health test"], jira.Comments.Select(c => c.Text));
}
[Fact] public async Task Push_failure_is_logged_and_retried_next_tick()
{
    var ws = new FakeWorkspace(heads: ["a", "b", "b"]) { FailNextPush = true };
    await RunTicks(ws, new FakeJira(), ticks: 3);
    Assert.Single(ws.Pushes);  // first attempt threw, second succeeded
}
[Fact] public async Task Jira_failure_does_not_stop_the_loop() { /* FakeJira.FailComments = true; assert loop keeps pushing */ }
[Fact] public async Task Cancellation_returns_without_throwing() { /* cancel token; await RunAsync completes */ }
```

(Use `interval: TimeSpan.FromMilliseconds(20)` and cancel after the expected tick count; `RunTicks`/`Tick` helpers drive this.)

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement with `PeriodicTimer`.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: live progress via pushes and per-task Jira comments`.

---

### Task 9: Plan runner orchestration

**Files:**
- Modify: `src/ClaudeQueue/Git/IWorkspace.cs`, `src/ClaudeQueue/Git/GitWorkspace.cs`, `src/ClaudeQueue/Git/IPullRequests.cs`, `src/ClaudeQueue/Git/GhPullRequests.cs` (+ their test files) for the rerun lookups below
- Create: `src/ClaudeQueue/Runs/PlanRunner.cs`, `src/ClaudeQueue/Runs/KeepAwake.cs`, `tests/ClaudeQueue.Tests/Runs/KeepAwakeTests.cs`, `tests/ClaudeQueue.Tests/Fakes/FakeClaude.cs`, `.../Fakes/FakeVerifier.cs`, `.../Fakes/FakePullRequests.cs`
- Test: `tests/ClaudeQueue.Tests/Runs/PlanRunnerTests.cs`

**Interfaces:**
- Consumes: everything from Tasks 2–8.
- Produces: `public sealed class PlanRunner(IJira jira, IWorkspace workspace, IClaude claude, IVerifier verifier, IPullRequests prs, LiveProgress progress, IOptions<QueueOptions> options, TimeProvider time, ILogger<PlanRunner> logger)` with `public Task RunAsync(JiraCard card, CancellationToken ct)`.

Sequence: transition to `InProgressStatus` + comment `Started <yyyy-MM-dd HH:mm:ss>` → parse `PlanReference` (failure, or `Repo` ≠ `DemoRepo` ⇒ `Invalid card` with reason `Card names repo '<x>', expected '<DemoRepo>'`) → `BranchName.For` → `PrepareAsync` → read plan file from workDir, `PlanTaskTitles.Parse` (missing file ⇒ `Claude failed`, Detail `Plan file not found: <path>`) → start `LiveProgress` with a linked CTS → `claude.RunAsync(workDir, RunPrompt.Build(...), e => log ToConsoleLine(e))` → stop progress (cancel + await) → `verifier.RunAsync` (always, including after a timeout) → `CommitsSinceAsync(base)`: if > 0 then `PushAsync` + `prs.CreateAsync(DemoRepo, branch, $"{card.Key} {card.Summary}", body, draft: outcome != Verified)` where `body` = `Plan: <PlanPath> @ <sha7>\nJira: <JiraBaseUrl>/browse/<key>\n\n` + the report text, else Detail `No commits were made` → outcome → `CommentAsync(ReportBuilder.Build(...))` → transition to `ReviewStatus`.

**Numbered reruns** (owner decision: dragging a card from In Review back to Ready must work). Before `PrepareAsync`, pick the branch: `b = BranchName.For(...)`; if `workspace.RemoteBranchExistsAsync(DemoRepo, b)` then try `b + "-run2"`, `-run3`, … up to `-run20` (then `Claude failed`, Detail `Too many reruns of <key>`). When the chosen branch is a rerun, look up the previous branch's PR with `prs.FindAsync(DemoRepo, previousBranch)` and put `Rerun <n> — previous: <PR url, or previous branch name if no PR>` in the report `Detail` (prefixed to any other detail with `; `). Additions to earlier tasks' ports, implemented in this task:
- `IWorkspace.RemoteBranchExistsAsync(string repo, string branch, CancellationToken ct)` → `GitWorkspace` runs `git ls-remote --heads <clone url> refs/heads/<branch>` (no clone needed) and returns true when output is non-empty; uses the same overridable clone-URL factory as `PrepareAsync`, so it is testable against the temp bare repo.
- `IPullRequests.FindAsync(string repo, string branch, CancellationToken ct)` → `Task<string?>`; `GhPullRequests` runs `gh pr list --repo <repo> --head <branch> --state all --json url --jq ".[0].url"`, returns trimmed stdout or null when empty; static `BuildFindArgs` tested like `BuildArgs`.
Tests: `Rerun_uses_run2_when_branch_exists` (FakeWorkspace reports `plan/SCRUM-7-health` exists → PrepareAsync gets `plan/SCRUM-7-health-run2`, report Detail contains `Rerun 2 — previous: https://…/pull/3`), `Third_run_uses_run3`, `GitWorkspace_RemoteBranchExists_true_after_push_false_before`, `Gh_find_args`.

**Keep the machine awake** for the whole run (from "Started" to the final transition): `Runs/KeepAwake.cs` — `public sealed class KeepAwake : IDisposable` with `public static KeepAwake Acquire()`, calling Win32 `SetThreadExecutionState(ES_CONTINUOUS | ES_SYSTEM_REQUIRED)` via `[LibraryImport("kernel32.dll")]` on acquire and `SetThreadExecutionState(ES_CONTINUOUS)` on dispose; a no-op on non-Windows (`OperatingSystem.IsWindows()`). Owner's harness log records an unattended run killed by the machine sleeping. Because `SetThreadExecutionState` is per-thread and `async` code hops threads, acquire and release it on a dedicated thread: `Acquire()` starts a background thread that sets the state and blocks on a `ManualResetEventSlim`; `Dispose()` signals it, and the thread clears the state and exits. Test: `KeepAwake_acquire_and_dispose_do_not_throw` (Windows-only assertion that `Dispose` completes within 2 s).

Outcome rule: Claude `TimedOut` ⇒ `Timed out`; Claude `Failed` ⇒ `Claude failed`; else verify passed ⇒ `Verified`, else `Verification failed`.

Any exception after the "Started" comment (except cancellation of the service's own token) is caught, logged at ERROR, and becomes `Claude failed` with Detail = exception message; the report comment and the transition to In Review are still attempted, each in its own try/catch that logs. On service cancellation, Claude is killed (via the token) and the card is left In Progress (spec: manual recovery).

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public async Task Happy_path_ends_in_review_with_ready_pr()
{
    var f = Fixture(claude: ClaudeExit.Succeeded, verifyPassed: true, commits: 3);
    await f.Runner.RunAsync(Card(validBlock), default);
    Assert.Equal(["In Progress", "In Review"], f.Jira.Transitions.Select(t => t.Status));
    Assert.False(f.Prs.Created.Single().Draft);
    Assert.StartsWith("✅ Verified", f.Jira.Comments.Last().Text);
    Assert.StartsWith("Started ", f.Jira.Comments.First().Text);
}
[Fact] public async Task Verify_failure_gives_draft_pr() { var f = Fixture(ClaudeExit.Succeeded, verifyPassed: false, commits: 2); await Run(f); Assert.True(f.Prs.Created.Single().Draft); Assert.StartsWith("⚠️ Verification failed", f.Jira.Comments.Last().Text); }
[Fact] public async Task Timeout_still_gets_draft_pr_from_existing_commits() { var f = Fixture(ClaudeExit.TimedOut, verifyPassed: false, commits: 1); await Run(f); Assert.True(f.Prs.Created.Single().Draft); Assert.StartsWith("⏱️ Timed out", f.Jira.Comments.Last().Text); }
[Fact] public async Task No_commits_means_no_push_and_no_pr() { var f = Fixture(ClaudeExit.Succeeded, verifyPassed: true, commits: 0); await Run(f); Assert.Empty(f.Prs.Created); Assert.Empty(f.Workspace.Pushes); Assert.Contains("No commits were made", f.Jira.Comments.Last().Text); }
[Fact] public async Task Invalid_block_skips_claude_and_ends_in_review()
{
    var f = Fixture(); await f.Runner.RunAsync(Card("no block here"), default);
    Assert.Equal(0, f.Claude.Calls); Assert.StartsWith("⛔ Invalid card", f.Jira.Comments.Last().Text);
    Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
}
[Fact] public async Task Wrong_repo_is_invalid_card() { var f = Fixture(); await f.Runner.RunAsync(Card(BlockFor("nluangroup/jira")), default); Assert.Contains("expected 'nluangroup/agentic-demo'", f.Jira.Comments.Last().Text); }
[Fact] public async Task Exception_mid_run_still_reports_and_moves_to_review()
{
    var f = Fixture(); f.Workspace.ThrowOnPrepare = new InvalidOperationException("git clone failed");
    await Run(f);
    Assert.Contains("git clone failed", f.Jira.Comments.Last().Text); Assert.Equal("In Review", f.Jira.Transitions.Last().Status);
}
[Fact] public async Task Failed_review_transition_does_not_throw() { var f = Fixture(); f.Jira.FailTransitionTo = "In Review"; await Run(f); /* completes; error logged */ }
[Fact] public async Task Pr_title_is_card_key_and_summary() { var f = Fixture(commits: 1); await f.Runner.RunAsync(Card(validBlock, summary: "Health endpoint"), default); Assert.Equal("SCRUM-7 Health endpoint", f.Prs.Created.Single().Title); }
```

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement `PlanRunner`. Duration via `TimeProvider`.
- [ ] **Step 4:** `dotnet test` → PASS.
- [ ] **Step 5:** Commit `feat: orchestrate a plan run from Ready to In Review`.

---

### Task 10: Queue worker, wiring, README, smoke run

**Files:**
- Create: `src/ClaudeQueue/QueueWorker.cs`, `README.md`; modify `src/ClaudeQueue/Program.cs`
- Test: `tests/ClaudeQueue.Tests/QueueWorkerTests.cs`

**Interfaces:**
- Consumes: `IJira.GetNextReadyAsync` (Task 5), `PlanRunner.RunAsync` (Task 9), `QueueOptions.PollInterval`.
- Produces: `public sealed class QueueWorker(IJira jira, PlanRunner runner, IOptions<QueueOptions> options, ILogger<QueueWorker> logger) : BackgroundService`.

Loop: log `Watching SCRUM for "Ready for Claude" every 5s` at start; each poll → card? log `Picked up <key>: <summary>` and `await runner.RunAsync` : wait `PollInterval`. A failed poll (Jira down after resilience retries) is logged at ERROR and the loop continues after `PollInterval`. One card at a time.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public async Task Runs_cards_one_at_a_time_in_order() { /* FakeJira returns SCRUM-1, SCRUM-2, then null; assert runner saw [SCRUM-1, SCRUM-2] and never overlapped */ }
[Fact] public async Task Poll_failure_is_logged_and_loop_continues() { /* first GetNextReadyAsync throws HttpRequestException, second returns a card; assert card ran */ }
```

(`PlanRunner` is sealed; for these tests extract nothing — construct a real `PlanRunner` over fakes and assert via `FakeJira.Transitions`.)

- [ ] **Step 2:** `dotnet test` → FAIL.
- [ ] **Step 3:** Implement `QueueWorker`; register in `Program.cs`: `IWorkspace→GitWorkspace`, `IClaude→ClaudeCli`, `IVerifier→PwshVerifier`, `IPullRequests→GhPullRequests`, `LiveProgress`, `PlanRunner`, `TimeProvider.System`, `AddHostedService<QueueWorker>()`. At startup validate `JiraEmail`/`JiraApiToken` are set and fail fast with a clear message naming `dotnet user-secrets set Queue:JiraApiToken ...`.
- [ ] **Step 4:** `dotnet test` → all pass.
- [ ] **Step 5:** Write `README.md`: prerequisites (.NET 10 SDK, PowerShell 7, git, `gh auth login` + `gh auth setup-git`, Claude Code CLI logged in with the Superpowers plugin), the two user-secrets commands, adding the "Ready for Claude" column on the SCRUM board, the card block format (copy from Task 2), `dotnet run --project src/ClaudeQueue`, and where run logs land.
- [ ] **Step 6: Smoke run (needs the `agentic-demo` plan done).** With the service running, create a SCRUM card whose description has a valid block for a one-task plan in `agentic-demo`; drag it to Ready for Claude. Expected within ~5 s: console `Picked up SCRUM-<n>`, card In Progress with a "Started" comment; during the run, `Claude ▶ …` lines, branch pushes on GitHub, a `Task 1/1 done — …` comment; at the end a PR on `nluangroup/agentic-demo` and a report comment, card In Review. If `agentic-demo` is not ready yet, run the smoke with a card that has no block and expect `⛔ Invalid card` + In Review — and record that the full smoke is pending.
- [ ] **Step 7:** Commit `feat: queue worker loop, wiring and README`.
