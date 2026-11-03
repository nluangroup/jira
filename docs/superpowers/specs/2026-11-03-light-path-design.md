# Light Path for Small Changes — Design

Date: 2026-11-03
Status: Draft for review
Type: chore

## Intent

Every change currently goes through a spec, a plan PR, an implementation PR and a full subagent review cycle. For a 16-line change (SCRUM-7) that meant two PRs and five subagent runs. Colleagues will find that too heavy to adopt. Add a light path for small changes that keeps what matters (a card, a reviewed PR, `verify`) and drops the rest.

## Goals

- Two paths: **full** (today's flow, unchanged) and **light**.
- Light path: a card, one PR whose description states what and why, Claude implementing directly, one reviewer subagent at the end, `verify` passing.
- Claude proposes the path when work is agreed; the user confirms.
- Light work that turns out bigger switches to the full path.

## Non-goals

- Changing the full path.
- Skipping the card, the PR or `verify` for any change.
- A size threshold in lines or files.

## Anti-goals

- Design decisions made silently in a light PR. Big decisions still get a `docs/decisions.md` entry, in the same PR.
- Light changes merged without a human reviewing the PR.

## Choosing a path

- **Light:** one clear change whose what and why fit in a PR description. Process changes to `CLAUDE.md` count (SCRUM-7 would have been light).
- **Full:** new features, work spanning several parts of the code, or anything where the approach is still open and benefits from a brainstorm before code exists.
- Claude proposes a path when the work is agreed and records the confirmed path on the card (`Path: light` or `Path: full`). When unsure, ask.

## The light path

1. Find or create the card (as in "Keeping the board in sync"), move it to **In Progress** and comment the branch name.
2. Create `<type>/SCRUM-<n>-<short-slug>` from `origin/main`; `<type>` (feature, fix or chore) is agreed with the path.
3. Make the change directly, without per-task subagents. If it makes a big, long-lived decision, add the `docs/decisions.md` entry in the same branch.
4. Run `verify` before every commit.
5. Dispatch one reviewer subagent on the branch diff. Fix Critical and Important findings; append the rest to `TODO.md`.
6. Push and open the PR. The description says what changes and why, and links the card.
7. Post the final report on the card (same format; `Rulings` may be "none") and move it to **In Review**.

If the change grows beyond light — the approach becomes open, or it spans several parts of the code — stop, comment on the card, and switch to the full path: write a spec and plan and open a plan PR.

## Changes to make

In `CLAUDE.md`:

- "The flow": say there are two paths; the existing steps are the full path.
- New `### Choosing a path` and `### The light path` sections with the content above.
- "Branches": replace "Every change, however small, goes through a plan PR and gets a card, for now" with: every change gets a card and a PR; full-path changes go through a plan PR first; light-path changes go straight to an implementation branch. The type of a light-path branch is the one agreed with the path.
- "Keeping the board in sync": the plan-PR card step applies to the full path only.

In `README.md`: the "Plans are reviewed before code" principle says small changes go through one PR, bigger ones through a plan PR first.

In `docs/decisions.md`: a new entry replacing "Every change goes through a plan PR, for now".

## Decided by the owner

- Make the workflow lighter for small changes.
- SCRUM-7 counts as small.
- One reviewer subagent is enough on the light path.

## Left to the builder

- Exact wording and placement in `CLAUDE.md` and `README.md`.
