# Branch Policy Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Write the team's branching rules into `CLAUDE.md`.

**Architecture:** Documentation only. One new section in `CLAUDE.md` and two small edits to existing sections. The GitHub settings in the spec are already applied and are not part of this plan.

**Tech Stack:** Markdown.

**Spec:** `docs/superpowers/specs/2026-11-02-branch-policy-design.md`

## Global Constraints

- Branch prefixes, exactly: `plan/<topic>`, `feature/SCRUM-<n>-<slug>`, `fix/SCRUM-<n>-<slug>`, `chore/SCRUM-<n>-<slug>`.
- Spec type line, exactly: `Type: feature | fix | chore`.
- Every change, however small, goes through a plan PR and gets a card.
- No verify command exists yet; the repo has no app. Verification for this plan is the grep checks below.

## Review Focus

- A spec without a `Type:` line: the "Executing a plan" step must say to stop and ask rather than guess a prefix.
- Leftover references to the old `SCRUM-<n>-<short-slug>` branch name anywhere in `CLAUDE.md`.
- Follow-up work after a merged PR: the no-reuse rule must say to start a new branch.
- The "Branches" section must not imply that `chore/` skips the plan PR or the card.
- `README.md` must not contradict the new rules (it doesn't name branches today; leave it unchanged unless it does).

---

### Task 1: Branch rules in CLAUDE.md

**Files:**
- Modify: `CLAUDE.md` ("Spec and plan", "Executing a plan" step 4, new "Branches" section after "Spec and plan")

**Interfaces:**
- Consumes: none.
- Produces: none.

- [ ] **Step 1: Check the current state**

Run: `grep -n "SCRUM-<n>-<short-slug>\|^### Branches\|Type:" CLAUDE.md`
Expected: one match for `SCRUM-<n>-<short-slug>` (step 4), no `### Branches`, no `Type:`.

- [ ] **Step 2: Add the `### Branches` section after "Spec and plan"**

Contents:
- the four-row table of prefixes from the spec's "Branch names" section, with what each is for;
- the type comes from the spec's `Type:` line;
- every change, however small, goes through a plan PR and gets a card, for now;
- once a branch's PR is merged, never push to it again; follow-up work starts a new branch;
- GitHub enforces this on `main`: PRs only, merge commits only, no force pushes, merged branches deleted automatically.

- [ ] **Step 3: In "Spec and plan", require the type line**

Add a bullet: the spec starts with a `Type: feature | fix | chore` line.

- [ ] **Step 4: In "Executing a plan" step 4, use the typed branch name**

Replace `SCRUM-<n>-<short-slug>` with `<type>/SCRUM-<n>-<short-slug>`, where `<type>` is the spec's `Type:` line. If the spec has no `Type:` line, stop and ask.

- [ ] **Step 5: Verify**

Run: `grep -n "SCRUM-<n>-<short-slug>" CLAUDE.md`
Expected: exactly one match, and it reads `<type>/SCRUM-<n>-<short-slug>`.

Run: `grep -n "^### Branches\|feature/SCRUM\|fix/SCRUM\|chore/SCRUM\|plan/<topic>\|Type: feature | fix | chore\|never push to it again" CLAUDE.md`
Expected: every pattern matches at least once.

- [ ] **Step 6: Commit**

```bash
git add CLAUDE.md
git commit -m "chore: branch policy in CLAUDE.md"
```
