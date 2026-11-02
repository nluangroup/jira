# Verification Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add `verify.ps1`, which checks the process documents, run it in CI on every PR, and describe it in the README.

**Architecture:** One PowerShell 7 script at the repo root with a `-Root` parameter, so it can be pointed at throwaway fixture folders. Each check is a function that returns its problems; the script prints `PASS`/`FAIL` lines and sets the exit code. There is no test framework: each task exercises its checks by hand against fixtures in a temp folder and records the commands and output in the report.

**Tech Stack:** PowerShell 7, git, GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-11-02-verification-design.md`

## Global Constraints

- Command: `pwsh ./verify.ps1`. Parameter: `-Root <path>`, default the script's folder.
- Output per check: `PASS: <name>` or one `FAIL: <name> — <file>:<line> <reason>` line per problem. Exit 1 if any check fails, else 0.
- Check names, exactly: `links`, `todo`, `decisions`, `specs`, `plans`.
- TODO categories, exactly: Critical, High Priority, Medium Priority, Low Priority, Architecture.
- Spec type line, exactly one of: `Type: feature`, `Type: fix`, `Type: chore`.
- File paths in output are relative to `-Root`, with forward slashes.
- Needs only PowerShell 7 and git. No Pester, no modules, no test files committed.
- Fixtures live in a temp folder outside the repo (`[IO.Path]::GetTempPath()`), are `git init`-ed and `git add`-ed (the `links` check reads tracked files via `git ls-files`), and are deleted afterwards.

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

**Interfaces:**
- Produces: `verify.ps1 [-Root <string>]`. Output and exit code as in Global Constraints.

- [ ] **Step 1: Implement `verify.ps1`** with `-Root`, the output and exit-code rules, and the `links` check.

- [ ] **Step 2: Check it by hand against fixtures.** For each case, build the fixture, run `pwsh ./verify.ps1 -Root <fixture>`, and record the command and output in the report:
  - existing relative link: `a.md` links `[b](b.md)`, `b.md` exists → `PASS: links`, exit 0.
  - missing target: `a.md` line 3 links `[x](missing.md)` → `FAIL: links — a.md:3 missing.md not found`, exit 1.
  - code blocks and inline code: a fenced block and an inline span both containing `[spec](<relative path>)` → `PASS: links`.
  - external and anchor links: `https://…`, `mailto:…`, `#top` → `PASS: links`.
  - fragment and `%20`: `[b](my%20file.md#part)` with `my file.md` present → `PASS: links`.
  - CRLF: the first fixture written with CRLF line endings → `PASS: links`.

- [ ] **Step 3: Run it on the repo**

Run: `pwsh ./verify.ps1`
Expected: `PASS: links`, exit 0. If a real broken link shows up, fix the link, not the check.

- [ ] **Step 4: Commit**

```bash
git add verify.ps1
git commit -m "chore: verify.ps1 with links check"
```

### Task 2: `todo` and `decisions` checks

**Files:**
- Modify: `verify.ps1`

**Interfaces:**
- Consumes: `verify.ps1` from Task 1.

- [ ] **Step 1: Implement the `todo` and `decisions` checks** per the spec's table.

- [ ] **Step 2: Check them by hand against fixtures**, recording commands and output:
  - `todo`, no items: `TODO.md` is `# TODO` and one sentence → `PASS: todo`.
  - `todo`, valid items: heading `## Low Priority — 2026-11-02`, items `#1` `[ ]` and `#2` `[x]` → `PASS: todo`.
  - `todo`, gap: `#1` then `#3` → a `FAIL: todo — TODO.md:<line>` line naming `#3` and expected `#2`.
  - `todo`, duplicate: `#1` twice → `FAIL: todo` naming the duplicate.
  - `todo`, unknown category: `## Someday — 2026-11-02` → `FAIL: todo` naming `Someday`.
  - `todo`, file missing → `PASS: todo`.
  - `decisions`, valid: two entries dated 2026-11-02 and 2026-11-01 in that order, each with What, Why, Links → `PASS: decisions`.
  - `decisions`, bad heading: `## Some title` → `FAIL: decisions`.
  - `decisions`, missing field: an entry without `**Why:**` → `FAIL: decisions` naming `Why`.
  - `decisions`, not newest first: 2026-11-01 above 2026-11-02 → `FAIL: decisions`.
  - `decisions`, file missing → `PASS: decisions`.

- [ ] **Step 3: Run it on the repo**

Run: `pwsh ./verify.ps1`
Expected: `PASS` for `links`, `todo` and `decisions`, exit 0.

- [ ] **Step 4: Commit**

```bash
git add verify.ps1
git commit -m "chore: verify todo and decisions"
```

### Task 3: `specs` and `plans` checks

**Files:**
- Modify: `verify.ps1`
- Modify: `docs/superpowers/specs/2026-11-02-team-claude-workflow-design.md` (add `Type: feature` under `Status:`)

**Interfaces:**
- Consumes: `verify.ps1` from Task 1.

- [ ] **Step 1: Implement the `specs` and `plans` checks** per the spec's table.

- [ ] **Step 2: Check them by hand against fixtures**, recording commands and output:
  - `specs`, no Type line: `docs/superpowers/specs/x-design.md` without one → `FAIL: specs — docs/superpowers/specs/x-design.md`.
  - `specs`, unknown type: `Type: spike` → `FAIL: specs`.
  - `specs`, each allowed type: one spec each for feature, fix, chore → `PASS: specs`.
  - `plans`, spec missing: a plan with `**Spec:** \`docs/superpowers/specs/nope.md\`` → `FAIL: plans` naming `nope.md`.
  - `plans`, no Spec line → `FAIL: plans`.
  - `plans`, spec exists → `PASS: plans`.

- [ ] **Step 3: Add `Type: feature` to the team-workflow spec.**

- [ ] **Step 4: Run the full verify on the repo**

Run: `pwsh ./verify.ps1`
Expected: `PASS` for all five checks, exit 0.

- [ ] **Step 5: Commit**

```bash
git add verify.ps1 docs/superpowers/specs/2026-11-02-team-claude-workflow-design.md
git commit -m "chore: verify specs and plans"
```

### Task 4: CI and README

**Files:**
- Create: `.github/workflows/verify.yml`
- Modify: `README.md` ("Setup" and "Verify")

**Interfaces:**
- Produces: the GitHub check named `verify`.

- [ ] **Step 1: Write `.github/workflows/verify.yml`**: name `verify`, triggers `pull_request` (branches `main`) and `push` (branches `main`), job id `verify` on `ubuntu-latest`, steps: checkout; `./verify.ps1` with `shell: pwsh`.

- [ ] **Step 2: Update `README.md`**: "Setup" adds PowerShell 7. "Verify" replaces the "No app yet" text with the command, one line per check from the spec, that CI runs it on every PR and it must pass to merge, and that the app's build and tests will be added to the same script.

- [ ] **Step 3: Run verify**

Run: `pwsh ./verify.ps1`
Expected: all five `PASS`, exit 0.

- [ ] **Step 4: Commit and push; open the PR**

```bash
git add .github/workflows/verify.yml README.md
git commit -m "chore: run verify in CI; document it in README"
```

After `gh pr create`: `gh pr checks --watch` shows `verify` passing.

- [ ] **Step 5: After the PR is merged, with the owner's go-ahead,** add `verify` as a required status check on the `main` ruleset, and record it on the card.
