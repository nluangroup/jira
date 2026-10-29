# Claude Jira Queue — Design

Date: 2026-10-29
Status: Draft for review

## Intent

Demonstrate to colleagues the shift from "AI as a tool that waits for my next
instruction" to "AI that is given responsibility for a complete, approved
outcome". The core idea: **separate deciding the work from doing it.**

- Thinking stays interactive: specs and implementation plans are made with
  Claude CLI + Superpowers, and the engineer decides the design.
- Approval is an explicit human act: dragging a Jira card into a queue column.
- Execution is unattended: a local service picks up approved plans and runs
  Claude headless on them.
- Acceptance stays human: the result is a pull request, and "Done" is set only
  by the engineer after merging ("outsource your thinking, not your
  understanding").

This is a **demo**, shown in a 30-minute session. It must look convincing and
work reliably in front of an audience. It is not a production system. The
team's real tracker is Azure DevOps; Jira is used here because it is a cleaner
demo surface and already set up.

## Goals

- An approved Superpowers plan becomes a Jira card, and dragging it to
  "Ready for Claude" causes it to be implemented without further prompting.
- The audience can watch the card move: Ready for Claude → In Progress →
  In Review, with a pushed branch, an open PR, and a report comment.
- One live plan completes in roughly 10 minutes or less.
- Plans run earlier (e.g. overnight) are visible on the board as "what ran
  while I slept".

## Non-goals

- Azure DevOps integration.
- Parallel execution of plans.
- A custom dashboard (Jira is the dashboard).
- A local database (Jira is the queue and the record).
- Automatic task discovery or reprioritisation by Claude.
- Crash recovery beyond "a card left In Progress after a crash is handled by
  hand".
- Running anywhere except the presenter's Windows machine.

## Anti-goals (failure even if it technically works)

- Claude merging anything, or pushing to `main`.
- Claude silently stalling on stage (a permission prompt, a question to a
  human nobody sees, a hang with no visible state change).
- The service marking work Done.
- An unattended run touching the presenter's own working copies.
- The plan that runs differing from the plan that was approved.

## Repositories

| Repo | Contents |
|---|---|
| `nluangroup/jira` | The queue service (.NET 10 worker) and this design. |
| `nluangroup/agentic-demo` | The demo app that plans change, its Superpowers specs/plans, its `verify` script, its project `CLAUDE.md`, and its headless permission settings. Currently empty. |

The service must never run plans against its own repo.

Note: an earlier Claude-from-Jira experiment (PR #1, a Python Flask app) was
closed and its branch deleted on 2026-10-29. Nothing from it carries over.

## Jira

- Site: `https://nlundgaard.atlassian.net` (cloudId
  `2cf169bb-d580-472b-b2b0-49d9c66c6364`).
- Project: "Agentic", key `SCRUM`, team-managed, simple board ("SCRUM board").
  The presenter is project lead.
- Existing statuses: To Do, In Progress, In Review, Done.
- **New status/column to add by hand in the Jira UI: "Ready for Claude"**,
  between To Do and In Progress. (Done by the presenter, not by code.)
- Existing cards SCRUM-1..4 are placeholders; SCRUM-5 ("add a /helth
  endpoint") was used by the earlier experiment. The demo should use fresh
  cards; cleaning the board up before the session is the presenter's job.

### Status meanings

| Status | Meaning | Moved by |
|---|---|---|
| To Do | Plan queued but not yet approved | "Queue this plan" skill |
| Ready for Claude | Approved, waiting; board rank = priority | Presenter (drag) |
| In Progress | Service is running Claude on it | Service |
| In Review | Branch pushed, PR open (draft if verification failed) | Service |
| Done | PR merged | Presenter |

## Component 1 — "Queue this plan" skill (in `agentic-demo`)

A Claude Code skill checked into `agentic-demo` (project-level skill), used
interactively at the end of a Superpowers planning session: "queue this plan".

It:
1. Identifies the current plan under `docs/superpowers/plans/` and the spec it
   names under `docs/superpowers/specs/`.
2. Ensures both are committed and pushed to `origin/main` (asks before
   committing/pushing if they are not).
3. Creates a `SCRUM` Task through the Atlassian connector, in **To Do**, with:
   - Summary: the plan's title.
   - Description containing, in a fixed machine-readable block, the repo
     (`nluangroup/agentic-demo`), the plan path, and the full commit SHA that
     was approved. Plus a short human summary of the plan.
4. Replies with the card link.

The service relies only on the machine-readable block. Its exact format is the
builder's call, but it must be robust to the description being edited by
hand around it.

## Component 2 — Queue service (in `nluangroup/jira`)

A .NET 10 Worker Service (`BackgroundService`), run from the console on the
presenter's machine during the demo. One plan at a time.

### Loop

1. Poll Jira every ~5 seconds (configurable) with JQL for the top-ranked card:
   `project = SCRUM AND status = "Ready for Claude" ORDER BY Rank ASC`.
2. Transition the card to **In Progress** and comment "Started" with a
   timestamp.
3. Parse repo / plan path / commit from the description. If missing or
   invalid: comment the reason, transition to **In Review** with no PR, and
   continue. (Never leave a card stuck In Progress because of bad input.)
4. Fresh-clone `nluangroup/agentic-demo` into a new per-run directory, check
   out the approved commit, create branch `plan/SCRUM-<n>-<slug>`.
5. Run `claude -p` in that directory (see "Claude invocation"), with live
   progress while it runs (see "Live progress").
6. Run the repo's verify script independently of Claude.
7. Make a final push of the branch and open a PR with `gh` against `main`:
   ready-for-review if verification passed, **draft** if it failed or Claude
   failed. If there are no commits, skip push/PR and say so in the report.
8. Comment the report on the card and transition to **In Review**.
9. Loop.

### Claude invocation

- `claude -p "<run prompt>" --output-format stream-json` (with whatever
  companion flags the installed CLI requires for streaming in print mode) so
  progress is visible while it runs; cost and the final message are taken
  from the stream's final result event. Turn limit, cost
  limit (`--max-budget-usd`), and a wall-clock timeout enforced by the
  service (kill the process tree on timeout). Limits are configuration.
- Run prompt content (exact wording is the builder's call):
  - Implement the approved plan at `<plan path>`; the spec and plan are
    approved, do not brainstorm or redesign.
  - Use Superpowers subagent-driven development to execute it.
  - The current directory is already an isolated workspace on the correct
    branch: do not create a worktree or another branch.
  - **Stop after the final review.** Do not run "finishing a development
    branch", do not merge, push, or open a PR — the service does that.
  - End with a short summary including the "Rulings I made" list.
- Headless runs also pass `--disallowedTools "Bash(git push:*)"` so Claude
  cannot push even if the prompt is ignored. (The interactive "queue this
  plan" skill may push the plan and spec to `main` with the presenter's
  approval; the "no push to main" anti-goal applies to unattended runs.)
- Execution mode is configurable: subagent-driven development (default), or
  Superpowers' inline `executing-plans` as a fallback if the live run is too
  slow. Which mode the live demo uses is decided after timing rehearsals.

### Live progress (owner's decision: all three channels)

The audience must see the run move during the ~10 live minutes, in three
places at once:

1. **Console.** The service parses Claude's stream and logs readable progress
   lines, e.g. "Task 1 started", "Task 1 reviewed ✓", "Final review started".
   Not raw JSON. Which stream events map to which lines is the builder's call;
   the lines must be understandable to an audience at a glance.
2. **GitHub.** The service watches the run directory's branch (e.g. checks for
   new commits every few seconds) and pushes the branch whenever a new commit
   appears, so commits show up live on GitHub. The service pushes, never
   Claude. The PR itself is opened only at the end (step 7).
3. **Jira.** Each time a plan task completes, the service adds a one-line
   comment to the card (e.g. "Task 2/3 done — Add health endpoint test"). How
   task completion is detected (stream events, the Superpowers progress
   ledger, commit messages) is the builder's call; it must not spam the card
   with more than roughly one comment per plan task.

A failure in any progress channel is logged and must not stop the run.

### Verification

`agentic-demo` contains a verify script (e.g. `verify.ps1`) that builds and
runs all tests and exits non-zero on any failure. The service runs it after
Claude exits and records pass/fail plus test counts. Claude's own claim of
success is never sufficient.

### Report comment

Posted on the card at the end of each run:
- Outcome: Verified / Verification failed / Claude failed / Timed out /
  Invalid card.
- PR link (and whether draft) and branch name.
- Test summary from the verify run.
- "Rulings I made" from Claude's final message, if present.
- Duration and cost (cost from Claude's JSON output).

Short — readable on a projector in a few seconds.

### Credentials and configuration

- Jira: site URL, email, and API token in .NET user secrets.
- GitHub: the presenter's existing `gh` login (used for clone, push, PR).
- Config (`appsettings.json`): poll interval, Jira project key and status
  names, demo repo, runs directory, Claude limits, execution mode.

### Logging

Console log in the format `[yyyy-MM-dd HH:mm:ss] LEVEL: message`, with clear
lines per state change — the console is visible on stage next to the board.
Claude's raw stream output and the verify output are saved per run in the run
directory.

### Failure handling

- Jira/GitHub transient errors: retry with exponential backoff, never swallow.
- Any failure during a run still ends with a report comment and a transition
  to In Review — the audience always sees an outcome.
- Service crash mid-run: the card stays In Progress; recovery is manual.

## Component 3 — `agentic-demo` repo setup

- A tiny .NET 10 web API with a handful of xUnit tests — big enough that
  "add a health endpoint" or "add input validation" are real tasks, small
  enough that a 1–2 task plan finishes in ~10 minutes.
- `verify.ps1` (build + test, non-zero on failure).
- **Project `CLAUDE.md`** that overrides the presenter's global Default
  Workflow when executing a Superpowers plan: Superpowers' execution workflow
  replaces the global implement → review → unit-tester loop and its
  2-iteration halt; deferred findings go into the report instead of
  `TODO.md`. Coding standards still apply. The global `CLAUDE.md` is not
  changed.
- **Checked-in Claude permission settings** (`.claude/settings.json`) with an
  allowlist sufficient for a headless run: subagent dispatch, file edits,
  `dotnet build/test`, `git` (local operations only), and the Superpowers
  helper scripts (which run under Git Bash on Windows). Network push is not
  needed by Claude. Completeness is proven by rehearsal, not by inspection.
- At least one prepared, approved, short plan for the live run, and two or
  three larger plans run beforehand for the "while I slept" part.

## The demo (30 minutes)

| Time | Beat | Live / prepared |
|---|---|---|
| 0–5 | The idea, "outsource your thinking, not your understanding" | Slides |
| 5–10 | Superpowers spec + plan (prepared), then "queue this plan" | Live |
| 10–12 | Card appears in To Do; drag to Ready for Claude | Live |
| 12–22 | Service picks it up; card In Progress; commits appear | Live |
| 22–26 | Card In Review; report comment; open the PR | Live |
| 26–30 | "What ran last night": earlier cards and PRs | Prepared |

The "while I slept" cards come from running this same service unattended on
the presenter's machine before the session (e.g. the night before). Their PRs
are left **unmerged** until after the demo, and those plans touch different
files from the live plan, so the live PR cannot conflict on stage. No extra
hardening for multi-hour runs beyond the retry/backoff already specified; if
an overnight run fails, it is simply rerun before the session.

Readiness requirements:
- Two full timed rehearsals in the demo setup, end to end.
- A screen recording of a successful live segment as fallback.

## Decided by the owner

- Superpowers in Claude CLI for specs and plans; the queue executes approved
  plans only.
- Jira (not Azure DevOps), because this is a demo.
- A branch per plan, a short report, pushed with a PR opened.
- The service stops at In Review; Done is the owner's call after merge.
- Live progress during a run on all three channels: console, commits pushed
  live to GitHub, and per-task comments on the card.
- Service in `nluangroup/jira`; demo app in `nluangroup/agentic-demo`.

## Designer's calls (owner to check)

- Fresh clone per run instead of reusing a working copy or worktree.
- Draft PR + In Review on failure, rather than a separate failure status.
- Cards created in To Do; dragging to Ready for Claude is the approval.
- Override lives in `agentic-demo`'s `CLAUDE.md`, not the global one.
- Credentials: Jira token in user secrets, GitHub via `gh` login.
- Polling (~5 s) rather than Jira webhooks — no public endpoint needed.
- Demo app is a .NET 10 web API with xUnit.
- "No push to main" applies to unattended runs; the interactive queue skill
  may push plan/spec to `main` with approval. Enforced with
  `--disallowedTools` on headless runs, not branch protection.
- Overnight cards are produced by the same service beforehand; their PRs stay
  unmerged and avoid the live plan's files.

## Left to the builder

- Project structure, class names, Jira REST client choice (plain `HttpClient`
  is fine), JSON handling (`System.Text.Json`).
- Format of the machine-readable block in the card description.
- Exact run prompt wording, within the rules above.
- Contents of the demo app and the example plans.
- Branch slug format details.
