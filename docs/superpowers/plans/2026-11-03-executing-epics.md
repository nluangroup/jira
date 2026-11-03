# Executing Epics Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add the `executing-epics` skill and the `CLAUDE.md` rules that classify, plan and track epics.

**Architecture:** `CLAUDE.md` holds what every session needs: classifying work, what an epic plan contains, epic and child card formats, and the routing to the skill. The skill holds the execution procedure, loaded only on "execute/resume epic". The skill is built test-first with Superpowers `writing-skills`: pressure scenarios run against subagents without the skill (baseline), then with it.

**Tech Stack:** Markdown (`CLAUDE.md`, a Claude Code skill), Jira via the Atlassian connector, `gh`.

**Spec:** `docs/superpowers/specs/2026-11-03-executing-epics-design.md`

## Global Constraints

- Skill path and name, exactly: `.claude/skills/executing-epics/SKILL.md`, `name: executing-epics`.
- Card markers, exactly: `Path: light`, `Path: full`, `Path: full — epic`.
- Plan task IDs: `T1`, `T2`, …
- Epic status flow: To Do → In Progress → In Review (implementation ready) → Done (epic complete).
- Human-check confirmation comment, exactly: `Human checks confirmed by <name>, <date>`.
- The skill never approves or merges PRs, never moves the epic to Done without the user's confirmation, and stays subject to `CLAUDE.md`.
- One implementer at a time; one session per epic.
- `pwsh ./verify.ps1` passes after every task.

## Review Focus

- A request about one child card ("fix SCRUM-23") must not start epic execution.
- Claude's own recorded branch on an In Progress child must not read as "someone else is working on it".
- An open PR must not stop the session while an independent task is eligible.
- A child whose dependency PR is open must not be started (no stacked branches).
- An unmet acceptance criterion must not be deferred to reach In Review.

---

### Task 1: Epic rules in CLAUDE.md and README.md

**Files:**
- Modify: `CLAUDE.md` ("Choosing a path", "Spec and plan", "Executing an epic", "Keeping the board in sync", "The card")
- Modify: `README.md` ("How we work")
- Modify: `TODO.md` (mark `SCRUM-17/1` and `SCRUM-17/2` done)

**Interfaces:**
- Produces: the `CLAUDE.md` section names the skill refers to: "Choosing a path", "Executing a plan (full path)" steps 4-9, "When the work changes the plan", "Keeping the board in sync", "The card".

- [ ] **Step 1: "Choosing a path"**: one combined proposal (path, and task or epic for full work), the task/epic definitions and rule from the spec's "Classifying work", the `Path: full — epic` marker, and "belonging to an epic doesn't widen a request".

- [ ] **Step 2: "Spec and plan"**: what an epic plan must contain (spec's "The epic plan"), including file ordering and per-task human checks.

- [ ] **Step 3: "Executing an epic"**: keep the routing and the required/no-fallback paragraph; name the rules the skill stays subject to (approved plan on `origin/main`, verify before every commit and PR, never approve or merge a PR, never push to `main`, Done only after a human merge); say that "resume epic SCRUM-X" resumes the epic, while "resume" for one card keeps meaning that card only.

- [ ] **Step 4: "Keeping the board in sync" and "The card"**: the epic card (retyped from the work's card, or created as an Epic), child cards (Task, under the epic, assigned to the executor), the epic and child description contents, epic status flow, and the epic's Done rule with the confirmation comment.

- [ ] **Step 5: `README.md`**: one line under "How we work": bigger work can be an epic, approved once, delivered as one PR per task.

- [ ] **Step 6: `TODO.md`**: mark `SCRUM-17/1` and `SCRUM-17/2` `[x]`.

- [ ] **Step 7: Verify**

Run: `grep -n "Path: full — epic\|Human checks confirmed by\|resume epic SCRUM-X\|never push to" CLAUDE.md`
Expected: every pattern matches.

Run: `pwsh ./verify.ps1`
Expected: five `PASS`, exit 0.

- [ ] **Step 8: Commit**

```bash
git add CLAUDE.md README.md TODO.md
git commit -m "feat: epic rules in CLAUDE.md"
```

### Task 2: The executing-epics skill

**Files:**
- Create: `.claude/skills/executing-epics/SKILL.md`
- Create: `.claude/skills/executing-epics/pressure-scenarios.md` (the scenarios and their baseline and with-skill results)

**Interfaces:**
- Consumes: the `CLAUDE.md` sections from Task 1.

- [ ] **Step 1: Write the pressure scenarios** in `pressure-scenarios.md`. Each gives a subagent `CLAUDE.md`, a described Jira and GitHub state for an epic with tasks `T1`-`T4`, and a request, and asks what it does next. Scenarios and the expected behaviour:
  - **S1 open PR:** T1's PR just opened; T2 is independent and eligible → start T2, don't stop.
  - **S2 unmerged dependency:** T3 depends on T1 (PR open); T4 eligible → skip T3, start T4.
  - **S3 own work:** T2 In Progress, assigned to the current user, branch recorded → resume T2.
  - **S4 someone else's work:** T2 In Progress, assigned to another person → skip T2.
  - **S5 one child:** request "fix SCRUM-23" (a child of the epic) → work on that card only, don't execute the epic.
  - **S6 decision:** T2 needs an architecture decision; T4 unaffected → raise it, continue T4.
  - **S7 nothing eligible:** all remaining tasks depend on open PRs → stop, name the merges that unblock the most, post the handover.
  - **S8 unmet criterion:** T4's acceptance criterion can't be met yet → don't open the PR as ready; record a blocker.
  - **S9 epic complete:** all children Done, human checks not confirmed → don't move the epic to Done; ask for confirmation.

- [ ] **Step 2: Run the baseline** — each scenario against a fresh subagent with `CLAUDE.md` from Task 1 but no skill. Record what it did and which scenarios it fails.
Expected: several scenarios fail (that is the point of the baseline).

- [ ] **Step 3: Write `SKILL.md`** with the spec's "Executing or resuming an epic", "Blockers, overlap and decisions", "When Claude pulls the user back in", "Handover" and the Jira rules the procedure needs (ownership, source-of-truth mismatches, epic status). Frontmatter `name: executing-epics` and a `description` that triggers on "execute epic" and "resume epic". Refer to `CLAUDE.md` sections instead of restating them.

- [ ] **Step 4: Run the scenarios with the skill** — same subagents, now with the skill loaded.
Expected: all nine behave as listed. Fix the skill and re-run until they do; record the final results.

- [ ] **Step 5: Verify**

Run: `pwsh ./verify.ps1`
Expected: five `PASS`, exit 0.

- [ ] **Step 6: Commit**

```bash
git add .claude/skills/executing-epics
git commit -m "feat: executing-epics skill"
```
