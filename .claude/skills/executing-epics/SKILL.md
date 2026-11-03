---
name: executing-epics
description: Use when asked to "execute epic SCRUM-X" or "resume epic SCRUM-X" in this repo, or when continuing through the tasks of an approved epic plan. Not for a request that names one child card ("fix SCRUM-23", "resume SCRUM-23").
---

# Executing epics

## Overview

Work through an approved epic plan one task at a time, one child card, branch and PR per task, without asking the user per task. Keep going while PRs wait for review; stop only when nothing more can move without a human, and say why.

`CLAUDE.md` stays the authority. This skill adds the epic loop; everything else follows the `CLAUDE.md` sections named below ("Executing an epic" lists the rules that bind every task).

## When not to use

A request about one child card ("fix SCRUM-23", "resume SCRUM-23") is not epic execution, even if the card belongs to an epic ("Choosing a path"). Work that card alone under "Executing a plan (full path)", following its plan task, then stop. Don't start other children afterwards; you may mention that "resume epic SCRUM-X" would continue the rest.

## Procedure

1. **Approval.** `git fetch origin`. The epic plan must be on `origin/main` at or after the epic's `Approved commit:`. If not, stop: it isn't approved.
2. **Reconcile** Jira with reality before choosing work:
   - Read the epic and every child. Check every recorded PR with `gh pr view`. Run the Done sweep ("Keeping the board in sync") for children whose PR merged, and move their human checks to the epic's outstanding list.
   - First execution: rewrite the epic's description, create the missing child cards (formats in "The card") and move the epic to **In Progress**.
   - Mismatches. The plan defines scope, Jira holds progress, GitHub decides PR state:
     - plan task without a child card: create the card (never a duplicate);
     - child card with no plan task: leave it, tell the user;
     - child acceptance criteria edited away from the plan: the plan wins; tell the user (a wanted change goes through "When the work changes the plan");
     - card status disagrees with its PR: fix the card from the PR.
3. **Resume this execution's own work first**, before anything new:

   | Child card | Whose | Do |
   |---|---|---|
   | In Progress, assigned to the current user, branch recorded | This execution's. Your own recorded branch is not "someone else working on it". | Resume on that branch without asking |
   | In Progress, assigned to someone else | Theirs | Skip it. Don't reassign it or touch their branch; tell the user |
   | In Progress, no branch recorded | Unclear | Ask the user |

4. **Pick the first eligible task** in plan order (see "Eligibility"). None eligible: see "Stop conditions".
5. **Run it** per "Executing a plan (full path)" steps 4-9: branch `<task type>/SCRUM-<child>-<slug>` from the latest `origin/main`, child card to In Progress with the branch, implement, verify, review, PR, final report, child to In Review. The progress comment names the plan task ID: `T2 done — <task title>` and the short SHA.
6. **Post the handover** comment on the epic (see "Handover").
7. **Go back to step 4 without asking.** An open PR is a progress update, not a reason to stop or check in.

One implementer at a time, one task at a time; one session per epic.

## Eligibility

A task is eligible only when all of these hold:

- it is in the approved plan;
- its child card isn't Done or In Review;
- every dependency's PR is **merged into `origin/main`**. Open, approved or "merging soon" is not merged. Never branch from or stack on an unmerged task's branch;
- no unresolved blocker or pending decision affects it;
- no other person or session is working on it.

## Blockers, overlap and decisions

- **Blocker on one task:** comment the reason and the action needed on its card; the card doesn't move to In Review. Continue the unaffected tasks.
- **An acceptance criterion that can't be met** is a blocker. Don't open the PR as ready, don't move the card to In Review, and don't move the criterion to `TODO.md` or "Deferred". Pushing the branch to keep the work is fine.
- **Unexpected file overlap** with a task in review or in progress: record it on both cards and defer the affected task until the other merges.
- **Material decision** (scope, architecture, acceptance criteria, conflicting requirements): don't make it yourself, however obvious the answer looks. Raise it on the card and to the user when found, pause the affected tasks (and tasks that depend on them), and continue the unaffected ones. A change to the plan follows "When the work changes the plan".

## Stop conditions

Stop only when no authorised work remains. Post the handover, then tell the user which condition applies:

- **Waiting on merges:** every remaining task depends on unmerged PRs. Name the PR merges that would unblock the most tasks.
- **Decision needed** that blocks all remaining work.
- **Blocker stops all progress:** missing access, an unavailable required tool, or a verify failure shared by all remaining tasks.
- **Implementation ready:** every task is merged or In Review. Move the epic to **In Review** and list the outstanding human checks.

**Epic complete** is different: every child is Done and the user has confirmed each outstanding human check in this conversation. Only then comment exactly `Human checks confirmed by <name>, <date>` and move the epic to **Done**. General praise ("looks good") doesn't confirm a check: list the checks and ask.

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

Detailed reports stay on the child cards. "Resume epic SCRUM-X" starts again at step 1, so it works even if the last handover is missing.

## Red flags

| Thought | Reality |
|---|---|
| "A PR is open; I'll check in with the user." | Start the next eligible task. |
| "T1 is approved and will merge soon; start T3." / "Branch T3 off T1's branch." | Not merged means not eligible. No stacking. |
| "The card shows a branch, so I must stop and ask." | If it's In Progress, yours, with your recorded branch: resume it. |
| "Their card is stale and the deadline is close; I'll take it." | Skip it and tell the user. |
| "SCRUM-23 is in an epic; I'll carry on with the rest." | One card was asked for. Do that card only. |
| "Open the PR and defer the criterion to `TODO.md`." | Unmet criterion = blocker. No PR as ready. |
| "The answer is obvious; I'll decide it." | Material decisions go to the user; pause affected tasks only. |
| "The user said it all looks good, so the checks are confirmed." | Ask for each outstanding check explicitly. |
| "Run two tasks in parallel to save time." | One implementer at a time. |

Never approve or merge a PR, never push to `main`, and move the epic to Done only after the user's confirmation.
