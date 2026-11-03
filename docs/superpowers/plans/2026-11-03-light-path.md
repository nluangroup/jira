# Light Path Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a light path for small changes to `CLAUDE.md` and `README.md`.

**Architecture:** Documentation only: two new subsections in `CLAUDE.md`, edits to "The flow", "Branches" and "Keeping the board in sync", one README line.

**Tech Stack:** Markdown.

**Spec:** `docs/superpowers/specs/2026-11-03-light-path-design.md`

## Global Constraints

- The full path is unchanged.
- Every change keeps a card, a PR and a passing `verify`.
- Card comment for the confirmed path, exactly: `Path: light` or `Path: full`.
- Light path review: exactly one reviewer subagent on the branch diff; Critical and Important fixed, the rest to `TODO.md`.
- `pwsh ./verify.ps1` must pass.

## Review Focus

- No sentence left saying every change goes through a plan PR.
- The light path still says to find an existing card before creating one.
- The switch-to-full rule names what to do (comment on the card, write a spec and plan, open a plan PR).
- The "Executing a plan" section still reads as the full path and isn't contradicted by the light path.
- Big decisions on the light path still get a `docs/decisions.md` entry.

---

### Task 1: Light path in CLAUDE.md and README.md

**Files:**
- Modify: `CLAUDE.md` ("The flow", new `### Choosing a path` and `### The light path` after "Branches", "Branches" bullets, "Keeping the board in sync")
- Modify: `README.md` ("Plans are reviewed before code" bullet)

**Interfaces:**
- Consumes: none.
- Produces: none.

- [ ] **Step 1: Check the current state**

Run: `grep -n "however small" CLAUDE.md`
Expected: one match in "Branches".

- [ ] **Step 2: "The flow"**: one line before the steps saying there are two paths (see "Choosing a path") and that the steps below are the full path; the light path is in "The light path".

- [ ] **Step 3: Add `### Choosing a path` and `### The light path`** after "Branches", with the content of the spec's "Choosing a path" and "The light path" sections, including the switch-to-full rule.

- [ ] **Step 4: "Branches"**: replace the "however small" bullet as the spec's "Changes to make" says; the type bullet covers both the spec's `Type:` line (full) and the agreed type (light).

- [ ] **Step 5: "Keeping the board in sync"**: the plan-PR card rule applies to the full path.

- [ ] **Step 6: `README.md`**: update the "Plans are reviewed before code" bullet as the spec says.

- [ ] **Step 7: Verify**

Run: `grep -n "however small" CLAUDE.md`
Expected: no matches.

Run: `grep -n "### Choosing a path\|### The light path\|Path: light\|one reviewer subagent" CLAUDE.md`
Expected: every pattern matches.

Run: `pwsh ./verify.ps1`
Expected: five `PASS`, exit 0.

- [ ] **Step 8: Commit**

```bash
git add CLAUDE.md README.md
git commit -m "chore: light path for small changes"
```
