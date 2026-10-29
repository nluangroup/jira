# TODO

## Medium Priority — 2026-10-29
- [ ] **#1 — Verify stderr is dropped from the report Detail and the verify log** (`src/ClaudeQueue/Verify/PwshVerifier.cs:~28`)
  A failing `verify.ps1` that ends with `throw`/`Write-Error` loses its reason. Add an stderr callback to `ProcessRunner.StreamAsync` or append stderr to the log.
- [ ] **#2 — A failing `gh pr list` lookup fails a rerun** (`src/ClaudeQueue/Runs/PlanRunner.cs` `PickBranchAsync`)
  The previous-PR lookup only feeds a note; catch, log and fall back to the previous branch name.
- [ ] **#3 — Plan path from the card is trusted** (`src/ClaudeQueue/Cards/PlanReference.cs`)
  Reject absolute paths and `..` segments before `Path.Combine(workDir, PlanPath)`.
- [ ] **#4 — Kill-tree is not asserted by any test** (`tests/ClaudeQueue.Tests/Processes/ProcessRunnerTests.cs`)
  Have a script start a child, record its PID, and assert it exited after timeout.

## Low Priority — 2026-10-29
- [ ] **#5 — Report names the branch before the final push succeeds** (`src/ClaudeQueue/Runs/PlanRunner.cs:~159`)
- [ ] **#6 — PR link in the rerun note is not clickable in Jira** (`src/ClaudeQueue/Reporting/ReportBuilder.cs` `Wrap`)
  Detail is inside `{noformat}`; render the rerun note outside it.
- [ ] **#7 — SKIPPED lines are not wrapped for Jira wiki markup** (`src/ClaudeQueue/Reporting/ReportBuilder.cs`)
- [ ] **#8 — Verify timeout Detail prints whole minutes ("0m" for sub-minute)** (`src/ClaudeQueue/Verify/PwshVerifier.cs:~676`)
- [ ] **#9 — Stream drain grace expiry drops lines silently** (`src/ClaudeQueue/Processes/ProcessRunner.cs`)
  Log a warning when the 2 s grace expires; cancel the `Task.Delay` timer when the readers finish first.
- [ ] **#10 — Same WARNING text for live-progress push and comment failures** (`src/ClaudeQueue/Runs/LiveProgress.cs`)
- [ ] **#11 — Malformed Jira payload throws an unclear exception** (`src/ClaudeQueue/Jira/JiraClient.cs`)
- [ ] **#12 — KeepAwake doesn't dispose its ManualResetEventSlim or check the SetThreadExecutionState result** (`src/ClaudeQueue/Runs/KeepAwake.cs`)
- [ ] **#13 — Parser edge cases** (`src/ClaudeQueue/Plans/LedgerParser.cs`, `src/ClaudeQueue/Cards/BranchName.cs`, `src/ClaudeQueue/Reporting/ReportBuilder.cs`)
  `int.Parse` overflow on huge task numbers; plan path with spaces reports "missing"; all-non-ASCII summary gives a trailing `-`; `ExtractRulings` keeps inner `\r` and matches "Rulings I made" on any line; `Icon` vs `Label` disagree on unknown outcomes.
- [ ] **#14 — Timing-based tests may flake on a slow machine** (`tests/ClaudeQueue.Tests/QueueWorkerTests.cs`, `tests/ClaudeQueue.Tests/Runs/LiveProgressTests.cs`)
- [ ] **#15 — Test gaps** (`tests/ClaudeQueue.Tests/`)
  `TimestampConsoleFormatter.Write`; resilience pipeline wiring; failed report comment still transitions; icons for three outcomes; empty `Skipped`.
- [ ] **#16 — Housekeeping** (`src/ClaudeQueue/ClaudeQueue.csproj`, `tests/ClaudeQueue.Tests/ClaudeQueue.Tests.csproj`)
  BOMs from templates, one CRLF line in the csproj, package version skew (Hosting 10.0.x vs Http.Resilience 10.11.x), CA1707 NoWarn may be unnecessary.

## Architecture — 2026-10-29
- [ ] **#17 — Standard resilience handler retries POSTs** (`src/ClaudeQueue/Program.cs`)
  A lost response can double a Jira comment, or turn an already-applied transition into a 400. Accepted for the demo; revisit with `DisableForUnsafeHttpMethods` plus idempotent checks if it shows up.

## Medium Priority — 2026-10-29 (headless permissions)
- [ ] **#18 — No warning when the target repo grants no allow rules** (`src/ClaudeQueue/Claude/HeadlessPermissions.cs:~34`)
  A settings file with no `permissions` key, or deny-only, omits `--allowedTools`; under `dontAsk` the run can do almost nothing and fails like an ordinary Claude failure. Warn when Allow is empty and update `Deny_only_file_has_empty_allow_and_no_warning`.

## Low Priority — 2026-10-29 (headless permissions)
- [ ] **#19 — Settings file read synchronously inside the async run** (`src/ClaudeQueue/Claude/HeadlessPermissions.cs:~24`)
  Acceptable for one small file.
- [ ] **#20 — Permissions warning lost if ClaudeCli.RunAsync throws** (`src/ClaudeQueue/Claude/ClaudeCli.cs`)
  Still in the console log.
