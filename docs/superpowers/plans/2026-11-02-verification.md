# Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `verify.ps1`, which checks the process documents and runs the repo's tests, run it in CI on every PR, and describe it in the README.

**Architecture:** One PowerShell 7 script at the repo root with a `-Root` parameter, so tests can point it at fixture folders. Each check is a function that returns its problems; the script prints `PASS`/`FAIL` lines and sets the exit code. Pester 5 tests drive the script as a black box against fixtures built in `TestDrive:`.

**Tech Stack:** PowerShell 7, Pester 5, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-11-02-verification-design.md`

## Global Constraints

- Command: `pwsh ./verify.ps1`. Parameter: `-Root <path>`, default the script's folder.
- Output per check: `PASS: <name>` or one `FAIL: <name> — <file>:<line> <reason>` line per problem. Exit 1 if any check fails, else 0.
- Check names, exactly: `links`, `todo`, `decisions`, `specs`, `plans`, `tests`.
- TODO categories, exactly: Critical, High Priority, Medium Priority, Low Priority, Architecture.
- Spec type line, exactly one of: `Type: feature`, `Type: fix`, `Type: chore`.
- File paths in output are relative to `-Root`, with forward slashes.
- `-SkipTests` skips the `tests` check. The Pester tests run the script against fixtures with `-SkipTests`, so the suite doesn't run itself recursively.
- Execute after the branch-policy plan.

## Review Focus

- Placeholders in code blocks, like `[spec](<relative path>)` in `CLAUDE.md`'s templates, must not fail `links`.
- Links with a `#fragment` to an existing file pass; links with spaces encoded as `%20` resolve.
- An empty `TODO.md` (header only) passes `todo`.
- Windows CRLF line endings must not break any check (the repo's files are CRLF in the working copy on Windows).
- Missing `TODO.md`, `docs/decisions.md` or spec/plan folders: the check passes with nothing to check, rather than crashing.

---

### Task 1: Script skeleton and the `links` check

**Files:**
- Create: `verify.ps1`
- Create: `tests/verify.Tests.ps1`

**Interfaces:**
- Produces: `verify.ps1 [-Root <string>] [-SkipTests]`. Output and exit code as in Global Constraints. A test helper in `tests/verify.Tests.ps1`: `Invoke-Verify -Root <path>` returning `@{ ExitCode; Lines }`, running the script with `-SkipTests`.

- [ ] **Step 1: Write failing tests** in `tests/verify.Tests.ps1`, each building a fixture under `TestDrive:` with `git init` (the `links` check reads tracked files via `git ls-files`, so fixtures `git add` their files):
  - `links passes for an existing relative link`: `a.md` links `[b](b.md)`, `b.md` exists → lines contain `PASS: links`, exit 0.
  - `links fails for a missing target`: `a.md` line 3 links `[x](missing.md)` → a line `FAIL: links — a.md:3 missing.md not found`, exit 1.
  - `links ignores code blocks and inline code`: a fenced block and an inline span both containing `[spec](<relative path>)` → `PASS: links`.
  - `links ignores external and anchor links`: `https://…`, `mailto:…`, `#top` → `PASS: links`.
  - `links strips fragments and decodes %20`: `[b](my%20file.md#part)` with `my file.md` present → `PASS: links`.
  - `links handles CRLF`: the same file as the first test, written with CRLF line endings → `PASS: links`.

- [ ] **Step 2: Run them to verify they fail**

Run: `pwsh -NoProfile -c "Invoke-Pester tests -Output Detailed"`
Expected: FAIL, `verify.ps1` not found.

- [ ] **Step 3: Implement `verify.ps1`** with `-Root` and `-SkipTests`, the output and exit-code rules, and the `links` check.

- [ ] **Step 4: Run the tests to verify they pass**

Run: `pwsh -NoProfile -c "Invoke-Pester tests -Output Detailed"`
Expected: all pass.

- [ ] **Step 5: Run it on the repo**

Run: `pwsh ./verify.ps1 -SkipTests`
Expected: `PASS: links`, exit 0. If a real broken link shows up, fix the link, not the check.

- [ ] **Step 6: Commit**

```bash
git add verify.ps1 tests/verify.Tests.ps1
git commit -m "chore: verify.ps1 with links check"
```

### Task 2: `todo` and `decisions` checks

**Files:**
- Modify: `verify.ps1`
- Modify: `tests/verify.Tests.ps1`

**Interfaces:**
- Consumes: `Invoke-Verify` from Task 1.

- [ ] **Step 1: Write failing tests**
  - `todo passes with no items`: `TODO.md` is `# TODO` and one sentence → `PASS: todo`.
  - `todo passes for valid items`: heading `## Low Priority — 2026-11-02`, items `#1` `[ ]` and `#2` `[x]` → `PASS: todo`.
  - `todo fails on a gap`: items `#1` then `#3` → a `FAIL: todo — TODO.md:<line>` line naming `#3` and expected `#2`.
  - `todo fails on a duplicate`: `#1` twice → `FAIL: todo` naming the duplicate.
  - `todo fails on an unknown category`: `## Someday — 2026-11-02` → `FAIL: todo` naming `Someday`.
  - `todo passes when the file is missing`.
  - `decisions passes for valid entries`: two entries dated 2026-11-02 and 2026-11-01 in that order, each with What, Why, Links → `PASS: decisions`.
  - `decisions fails on a bad heading`: `## Some title` → `FAIL: decisions`.
  - `decisions fails on a missing field`: an entry without `**Why:**` → `FAIL: decisions` naming `Why`.
  - `decisions fails when not newest first`: 2026-11-01 above 2026-11-02 → `FAIL: decisions`.
  - `decisions passes when the file is missing`.

- [ ] **Step 2: Run them to verify they fail**

Run: `pwsh -NoProfile -c "Invoke-Pester tests -Output Detailed"`
Expected: the new tests FAIL; Task 1 tests pass.

- [ ] **Step 3: Implement the `todo` and `decisions` checks** per the spec's table.

- [ ] **Step 4: Run the tests and the repo check**

Run: `pwsh -NoProfile -c "Invoke-Pester tests -Output Detailed"`, then `pwsh ./verify.ps1 -SkipTests`
Expected: all tests pass; `PASS: todo` and `PASS: decisions` on the repo.

- [ ] **Step 5: Commit**

```bash
git add verify.ps1 tests/verify.Tests.ps1
git commit -m "chore: verify todo and decisions"
```

### Task 3: `specs`, `plans` and `tests` checks

**Files:**
- Modify: `verify.ps1`
- Modify: `tests/verify.Tests.ps1`
- Modify: `docs/superpowers/specs/2026-11-02-team-claude-workflow-design.md` (add `Type: feature` under `Status:`)

**Interfaces:**
- Consumes: `Invoke-Verify` from Task 1.

- [ ] **Step 1: Write failing tests**
  - `specs fails without a Type line`: `docs/superpowers/specs/x-design.md` without one → `FAIL: specs — docs/superpowers/specs/x-design.md`.
  - `specs fails on an unknown type`: `Type: spike` → `FAIL: specs`.
  - `specs passes for each allowed type`: one spec each for feature, fix, chore → `PASS: specs`.
  - `plans fails when the spec is missing`: a plan with `**Spec:** \`docs/superpowers/specs/nope.md\`` → `FAIL: plans` naming `nope.md`.
  - `plans fails without a Spec line` → `FAIL: plans`.
  - `plans passes when the spec exists`.
  - `tests is skipped with -SkipTests`: output contains no `tests` line.
  - `tests fails when a test fails`: run without `-SkipTests` on a fixture with `tests/fail.Tests.ps1` holding one failing `It` → `FAIL: tests`, exit 1.

- [ ] **Step 2: Run them to verify they fail**

Run: `pwsh -NoProfile -c "Invoke-Pester tests -Output Detailed"`
Expected: the new tests FAIL.

- [ ] **Step 3: Implement the three checks.** `tests` runs Pester 5 on `<Root>/tests` with `-PassThru` and fails if the result has failures; if `Get-Module -ListAvailable Pester` has no version 5 or newer, it fails with `FAIL: tests — Pester 5 not installed (Install-Module Pester -MinimumVersion 5 -Scope CurrentUser)`.

- [ ] **Step 4: Add `Type: feature` to the team-workflow spec.**

- [ ] **Step 5: Run the tests and the full verify**

Run: `pwsh ./verify.ps1`
Expected: `PASS` for all six checks, exit 0.

- [ ] **Step 6: Commit**

```bash
git add verify.ps1 tests/verify.Tests.ps1 docs/superpowers/specs/2026-11-02-team-claude-workflow-design.md
git commit -m "chore: verify specs, plans and tests"
```

### Task 4: CI and README

**Files:**
- Create: `.github/workflows/verify.yml`
- Modify: `README.md` ("Setup" and "Verify")

**Interfaces:**
- Produces: the GitHub check named `verify`.

- [ ] **Step 1: Write `.github/workflows/verify.yml`**: name `verify`, triggers `pull_request` (branches `main`) and `push` (branches `main`), job id `verify` on `ubuntu-latest`, steps: checkout; `Install-Module Pester -MinimumVersion 5 -Force -Scope CurrentUser` in `pwsh`; `./verify.ps1` in `pwsh`.

- [ ] **Step 2: Update `README.md`**: "Setup" adds PowerShell 7 and Pester 5 (with the install command). "Verify" replaces the "No app yet" text with the command, one line per check from the spec, that CI runs it on every PR and it must pass to merge, and that the app's build and tests will be added to the same script.

- [ ] **Step 3: Run verify**

Run: `pwsh ./verify.ps1`
Expected: all six `PASS`, exit 0.

- [ ] **Step 4: Commit and push; open the PR**

```bash
git add .github/workflows/verify.yml README.md
git commit -m "chore: run verify in CI; document it in README"
```

After `gh pr create`: `gh pr checks --watch` shows `verify` passing.

- [ ] **Step 5: After the PR is merged, with the owner's go-ahead,** add `verify` as a required status check on the `main` ruleset, and record it on the card.
