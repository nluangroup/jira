# Decisions

Big, long-lived decisions a teammate needs months later. One entry per decision, newest first. Added while writing the spec, reviewed in the plan PR.

## 2026-11-03 — Claude's effort goes in the final report, not in Jira worklogs

**What:** Every final report ends with an `Effort:` line (Claude's elapsed time and tokens, main session and subagents) from `scripts/claude-usage.ps1`, which reads the local Claude Code transcripts. Claude never logs time as a Jira worklog.
**Why:** The team wants to see what each card cost in Claude time and tokens; worklogs are booked as the signed-in person and would mix Claude's time into theirs.
**Links:** card SCRUM-24, [PR #20](https://github.com/nluangroup/jira/pull/20)

## 2026-11-03 — The demo app is lunch registration on the team's stack, in this repo

**What:** An ASP.NET Core API (`src/`, SQLite via EF Core, xUnit) and an Angular app (`web/`, Vitest) live in this repo; `verify.ps1` and CI check both. Employees tick office lunches on a month calendar and count beverages per month; the office manager sees and exports monthly totals.
**Why:** Colleagues see code that looks like theirs, and the work splits into an epic with real dependencies for the pitch.
**Links:** [spec](superpowers/specs/2026-11-03-lunch-registration-design.md), card SCRUM-9

## 2026-11-03 — Demo identity: pick your name, the API trusts it

**What:** No real authentication: the app asks "Who are you?", remembers the choice, and sends the employee id in a request header. The README states this is not security.
**Why:** Real sign-in (Entra ID) needs app registrations and setup that add nothing to the pitch.
**Links:** [spec](superpowers/specs/2026-11-03-lunch-registration-design.md), card SCRUM-9

## 2026-11-03 — Epics: approve once, one PR per task, Claude continues past open PRs

**What:** Work needing several separately reviewable PRs becomes an epic: one plan PR approves all its tasks, each task gets a child card and a PR, and the `executing-epics` skill works through eligible tasks without asking per task. Dependent tasks wait for real merges (no stacked branches); shared files are ordered in the plan. Claude pulls the user back in only when nothing more can move without a human.
**Why:** Humans keep the gates that matter (plan approval, material decisions, merges) without acting as a dispatcher between tasks.
**Links:** [spec](superpowers/specs/2026-11-03-executing-epics-design.md), card SCRUM-16

## 2026-11-03 — Path and epic are separate choices, proposed together

**What:** When work is agreed, Claude proposes light or full and, for full work, task or epic, in one proposal the user confirms once. An existing Jira epic is used as the grouping; otherwise an epic is proposed only when several reviewable PRs are needed. Belonging to an epic doesn't widen a request about one task.
**Why:** Many steps don't make an epic, and a complex task can need the full path without one; one confirmation keeps it light.
**Links:** [spec](superpowers/specs/2026-11-03-executing-epics-design.md), card SCRUM-16

## 2026-11-03 — TODO.md items get card-scoped IDs

**What:** New `TODO.md` items are numbered `SCRUM-<n>/<k>`, counting from 1 per card; `verify.ps1` checks them. Items `#1`–`#17` keep their numbers.
**Why:** Global `#N` numbers collide when two branches each add an item: both PRs pass verify, then `main` fails.
**Links:** card SCRUM-15, [PR #12](https://github.com/nluangroup/jira/pull/12)

## 2026-11-03 — Small changes take a light path: card, one PR, one review

**What:** Small changes skip the spec and plan PR: a card, one PR whose description states what and why, Claude implementing directly, one reviewer subagent, `verify`. Bigger or open-ended work keeps the full path. Claude proposes the path; the user confirms. This replaces "Every change goes through a plan PR, for now". Tightened on 2026-11-03 (SCRUM-15): every new feature takes the full path; the light path is for bounded fixes and chores with an agreed approach.
**Why:** Two PRs and five subagent runs for a 16-line change (SCRUM-7) is too heavy for the team to adopt; the card, the reviewed PR and `verify` are what matter.
**Links:** [spec](superpowers/specs/2026-11-03-light-path-design.md), card SCRUM-14

## 2026-11-03 — Claude keeps the board in sync, including Done after a merge

**What:** Claude creates a To Do card when work is agreed, links the plan PR to it, and moves a card to Done once a human has merged its PR. This replaces "Claude never moves a card to Done".
**Why:** The board should reflect the work without manual dragging; the human gate is the merge, and Done only mirrors it.
**Links:** [spec](superpowers/specs/2026-11-03-jira-sync-design.md), card SCRUM-13

## 2026-11-02 — No PowerShell test framework; verify.ps1 has no automated tests of its own

**What:** Pester is dropped. `verify.ps1` needs only PowerShell 7 and git; its checks are exercised by hand against throwaway fixtures while it is built. This replaces "runs the Pester tests" in the entry below.
**Why:** Avoid a test-framework dependency (Windows ships an outdated Pester) for a small script; the app's own tests will cover real code.
**Links:** [spec](superpowers/specs/2026-11-02-verification-design.md), card SCRUM-8

## 2026-11-02 — verify.ps1 checks the process documents and is required in CI

**What:** `pwsh ./verify.ps1` checks links, `TODO.md`, `decisions.md`, specs and plans; CI runs it on every PR and the `main` ruleset requires it to pass. (Originally also ran Pester tests; see the entry above.)
**Why:** "Verify passes" is the definition of done, so it has to exist before the app does; enforcing it in CI means the rules hold for everyone, Claude included.
**Links:** [spec](superpowers/specs/2026-11-02-verification-design.md)

## 2026-11-02 — Every change goes through a plan PR, for now

**What:** Features, fixes and chores alike get a spec, a plan PR and a Jira card before implementation. Branches are `plan/`, `feature/`, `fix/` or `chore/` prefixed.
**Why:** One path is simpler to learn and to show; revisit if it slows the team down for small changes.
**Links:** [spec](superpowers/specs/2026-11-02-branch-policy-design.md)

## 2026-11-02 — GitHub enforces PR-only merges to main, with merge commits

**What:** A ruleset on `main` requires a PR (0 approvals) and blocks force pushes and deletion; only merge commits are allowed.
**Why:** "Never push to main" is enforced rather than an instruction; merge commits keep the task SHAs recorded on Jira cards reachable from `main`.
**Links:** [spec](superpowers/specs/2026-11-02-branch-policy-design.md)

## 2026-11-02 — Work with Claude through a shared CLAUDE.md, not a queue service

**What:** The team's Claude workflow is described in a checked-in project `CLAUDE.md` and runs alongside Superpowers. The ClaudeQueue service (unattended runs from a Jira column) is dropped.
**Why:** The pitch is about how the team collaborates; a versioned, reviewable process is easier to adopt than a service, and nothing runs unattended.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)

## 2026-11-02 — Plan review is a PR; merging it is the approval

**What:** Spec, plan and decisions go up as a small PR reviewed by a teammate before any code is written. The Jira card is created at approval.
**Why:** Teammates review intent before code, not only diffs after.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)

## 2026-11-02 — Big decisions live in docs/decisions.md; run rulings live on the card

**What:** Long-lived decisions go in this file, written while writing the spec. Rulings made during a run go on the Jira card.
**Why:** Cards are hard to find months later; this file is searchable, reviewed, and read by future Claude sessions.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)

## 2026-11-02 — Jira for now

**What:** Cards live in Jira project `SCRUM`, assigned to the human who owns the work.
**Why:** Already set up; Azure DevOps is a later step.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)
