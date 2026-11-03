---
name: executing-epics
description: Use when asked to "execute epic SCRUM-X" or "resume epic SCRUM-X" in this repo. Not for a request naming one child card ("fix SCRUM-23", "resume SCRUM-23").
---

# Executing epics

## Overview

Work through an approved epic plan one task at a time (one child card, branch and PR each) without asking per task. `CLAUDE.md` stays the authority: "Executing an epic" lists the rules binding every task.

## When not to use

A request about one child card ("fix SCRUM-23") is not epic execution ("Choosing a path"). Work that card alone as an epic child task ("Executing an epic"), then stop; don't start other children. You may mention that "resume epic SCRUM-X" would continue the rest.

## Procedure

1. **Approval.** `git fetch origin`.
   - First execution (no `Approved commit:` on the epic yet): take the plan from the epic's `Plan:` line as it is on `origin/main`; that `origin/main` SHA becomes the approved commit in step 2.
   - Otherwise the plan must be on `origin/main` at or after the epic's `Approved commit:`.
   - Plan not on `origin/main`: stop, it isn't approved.
2. **Reconcile** Jira with reality:
   - Read the epic and every child; check every recorded PR with `gh pr view`; run the Done sweep ("Keeping the board in sync") for merged children and move their human checks to the epic's outstanding list.
   - First execution: rewrite the epic's description with the approved commit, create the child cards ("The card") and move the epic to **In Progress**.
   - An epic in **In Review** with work to do again goes back to **In Progress**.
   - Before creating a child card for a plan task, search the epic's children for that task ID; never create a duplicate.
   - Mismatches (plan = scope, Jira = progress, GitHub = PR state): plan task without a card → create it; card without a plan task → leave it, tell the user; criteria edited away from the plan → the plan wins, tell the user; card status vs PR → fix the card from the PR; PR closed without merging → a blocker on that card, tell the user.
3. **Resume this execution's own work first:**

   | Child card | Whose | Do |
   |---|---|---|
   | In Progress, assigned to the current user, branch recorded, no unresolved blocker or pending decision on the card | Yours: not "someone else working on it" | Resume without asking: continue at step 5 on that branch (no new branch), then step 6 |
   | Same, but the card records an unresolved blocker or pending decision | Yours, blocked | Leave it; go to step 4 |
   | In Progress, assigned to someone else | Theirs | Skip; don't reassign or touch their branch; tell the user |
   | In Progress, assigned to the current user with no branch recorded, or unassigned | Unclear | Ask the user |

4. **Refresh, then pick.** `git fetch origin` and re-check every recorded PR with `gh pr view`, running the Done sweep for any that merged. Then pick the first eligible task in plan order (see "Eligibility"). If none is eligible, see "Stop conditions".
5. **Run it** as an epic child task ("Executing an epic"): "Executing a plan (full path)" steps 4-9 on branch `<task type>/SCRUM-<child>-<slug>` from the latest `origin/main`, scoped to that task's section and acceptance criteria.
6. **Post the handover** on the epic (see "Handover").
7. **Go back to step 4 without asking.** An open PR is a progress update, not a reason to stop or check in.

One implementer, one task at a time; one session per epic.

## Eligibility

A task is eligible only when:

- it is in the approved plan;
- its child card isn't Done or In Review;
- every dependency's PR is **merged into `origin/main`**. Open, approved or "merging soon" is not merged. Never branch from or stack on an unmerged task's branch;
- no unresolved blocker or pending decision affects it;
- no other person or session is working on it.

## Blockers, overlap and decisions

- **Blocker on one task:** comment the reason and the action needed on its card, which stays out of In Review. Continue the unaffected tasks.
- **An acceptance criterion that can't be met** is a blocker: no PR as ready, no In Review, no moving it to `TODO.md` or "Deferred". Pushing the branch is fine.
- **Unexpected file overlap** with a task in review or in progress: record it on both cards; defer the affected task until the other merges.
- **Material decision** (scope, architecture, acceptance criteria, conflicting requirements): don't make it yourself, however obvious. Raise it on the card and to the user, pause the affected tasks and their dependants, continue the rest. A plan change follows "When the work changes the plan".

## Stop conditions

Stop when step 4 finds nothing eligible: post the handover and tell the user which applies:

- **Waiting on merges:** every remaining task depends on unmerged PRs. Name the merges that would unblock the most tasks.
- **Decision needed** that blocks all remaining work.
- **Blocker stops all progress:** missing access, an unavailable tool, or a verify failure shared by all remaining tasks.
- **Implementation ready:** every task is merged or In Review. Move the epic to **In Review** and list the outstanding human checks.

In every stop report, also name children whose PRs have changes requested.

The epic moves to Done only by the rule in "The card". General praise ("looks good") doesn't confirm a human check: list the checks and ask.

## Handover

After every task and before stopping, post one progress comment on the epic:

```
Epic progress — <date>
Merged: <task IDs and card keys, or none>
In review: <task ID, card, PR link — one per line>
In progress: <task ID, card, branch, uncommitted changes or none>
Blocked: <task ID, card, reason, action needed>; pending decisions: <...>
Next: <next eligible task, or why none is eligible>
```

Detailed reports stay on the child cards. "Resume epic SCRUM-X" restarts at step 1, so a missing handover loses nothing.

## Red flags

| Thought | Reality |
|---|---|
| "T1 is approved and will merge soon; start T3." | Not merged, not eligible. |
| "A PR is open; I'll check in with the user." | Start the next eligible task. |
| "The card shows a branch, so I must stop and ask." | In Progress, yours, with your recorded branch: resume it. |
| "Open the PR and defer the criterion to `TODO.md`." | An unmet criterion is a blocker. Don't open the PR as ready. |
