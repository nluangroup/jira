# Branch Policy — Design

Date: 2026-11-02
Status: Draft for review
Type: chore

## Intent

Make the team's branching rules explicit in `CLAUDE.md` and enforced by GitHub, so "never push to `main`" no longer depends on Claude following an instruction, and branch names say what kind of work they hold.

## Goals

- Every change reaches `main` through a PR, enforced by GitHub.
- Branch names show the kind of work and, for implementation branches, the Jira card.
- A branch is never reused after its PR is merged.

## Non-goals

- Required reviewer approval. The author may approve their own PR (owner's decision in the team workflow design).
- Required status checks. Added once the repo has an app and a verify command that CI can run.
- A lighter path for small changes. For now every change goes through the full flow.

## Anti-goals

- A rule that stops the owner from merging their own PRs.
- Squash merges that orphan the task commit SHAs recorded on Jira cards.

## Branch names

| Branch | For |
|---|---|
| `plan/<topic>` | The plan PR: spec, plan and decision entries |
| `feature/SCRUM-<n>-<slug>` | Implementing an approved feature plan |
| `fix/SCRUM-<n>-<slug>` | Implementing an approved fix plan |
| `chore/SCRUM-<n>-<slug>` | Implementing an approved chore plan: tooling, dependencies, docs, `CLAUDE.md` |

The type comes from a `Type: feature | fix | chore` line in the spec. Every change, however small, goes through a plan PR and gets a card. Revisit if this slows the team down.

Once a branch's PR is merged, nothing more is pushed to it; follow-up work starts a new branch. (On 2026-11-02 a commit pushed to an already-merged plan branch needed a second PR.)

## GitHub settings (applied 2026-11-02)

Applied directly by the owner's go-ahead, since they live outside the repo:

- Ruleset `main` on the default branch, active, no bypass: pull request required (0 approvals), merge method `merge` only, force pushes blocked, deletion blocked.
- Repository: merge commits only (squash and rebase merges off); head branches deleted automatically after merge.

## Changes to make

In `CLAUDE.md`:

- A new "Branches" section with the table above, the `Type:` line, the full-flow rule and the no-reuse rule, and a note that GitHub enforces PR-only merges to `main`.
- "Executing a plan" step 4: create `<type>/SCRUM-<n>-<short-slug>` instead of `SCRUM-<n>-<short-slug>`.
- "Spec and plan": the spec starts with a `Type:` line.

## Left to the builder

- Exact wording and placement in `CLAUDE.md`.
