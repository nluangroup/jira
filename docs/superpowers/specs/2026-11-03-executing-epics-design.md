# Executing Epics — Design

Date: 2026-11-03
Status: Draft for review
Type: feature

## Intent

Today every full-path plan ends in one implementation PR, and Claude checks in with the user at each step. For work that needs several separately reviewable PRs, that turns the user into a dispatcher. An **epic** lets the team approve the scope once and then lets Claude work through the approved tasks on its own, opening one PR per task, while humans keep the gates that matter: approving the plan, deciding material changes, and merging PRs.

This also gives the pitch its strongest moment: approve an epic once, then review PRs as they arrive.

## Goals

- A repo skill, `executing-epics`, that executes or resumes an approved epic. `CLAUDE.md` already routes to it and stays the authority on approval, verification and merge rules.
- `CLAUDE.md` classifies agreed work as light or full, and full work as a task or an epic, in one proposal the user confirms once.
- One approved epic plan covers all its tasks; Claude doesn't ask again per task.
- Claude keeps working while PRs await review and pulls the user back in only when nothing more can move without a human.
- A session can end at any time and a later "resume epic SCRUM-X" continues from what Jira records.

## Non-goals

- Restarting or scheduling sessions automatically, or guaranteeing a handover after an abrupt interruption.
- Stacked branches (a task built on another task's unmerged branch).
- Running two implementation subagents in parallel within one session.
- Two sessions executing the same epic at the same time. One person runs an epic in one session at a time; another person may take tasks only by explicit agreement.
- Claude approving or merging PRs, or deciding human verification checks.

## Anti-goals

- Working on a task the approved epic plan doesn't contain, or expanding a request about one child task into executing the whole epic.
- Taking over work that belongs to another person or session.
- Marking a task ready for review with an unmet acceptance criterion deferred to make it look done.
- Calling an epic complete while PRs are unmerged or human checks are outstanding.
- A second card for work that already has one.

## Classifying work

When work is agreed, Claude makes one proposal covering both choices and asks for one confirmation, for example "I recommend the full path, organised as an epic." The choices stay separate:

- **Light or full** — as in "Choosing a path". Epics are always full path.
- **Task or epic** (full path only):
  - **Task:** one bounded change, delivered in one implementation PR, however many steps it takes.
  - **Epic:** several tasks contributing to one outcome, each with its own acceptance criteria and possibly dependencies.
- If the work already belongs to a Jira epic, that grouping is used. Otherwise Claude proposes an epic only when delivery needs several separately reviewable PRs; many steps alone never make an epic. A complex single task can take the full path without being an epic.
- The confirmed classification is recorded on the card: `Path: light`, `Path: full`, or `Path: full — epic`.

Belonging to an epic doesn't widen a request. "Fix SCRUM-23" works on that one child task under the normal rules; only an explicit "execute epic SCRUM-X" or "resume epic SCRUM-X" authorises working through the epic's plan.

## The epic plan

An epic goes through the normal full path: brainstorm, spec, plan, `docs/decisions.md` entries, one plan PR. Merging it approves every task it lists.

The plan contains:

- The epic's goal, scope and explicit exclusions.
- For each task: a stable ID (`T1`, `T2`, …), title, type (feature, fix or chore), acceptance criteria, and dependencies by task ID.
- The intended execution order.
- Any verification that needs a human, per task.
- **File ordering:** when two tasks touch the same files, the plan says which goes first; the second depends on the first.

Each task is small enough for one reviewable PR. The plan's task text is what the task's implementation follows, using the full-path execution for that task (Superpowers `subagent-driven-development` within the task).

## Jira structure

- **The epic card.** The work's card is the epic. When the classification is confirmed as an epic, an existing card for the work (for example a To Do **Task** created when the work was agreed) is changed to the **Epic** issue type; a new card is created as an Epic. If Jira refuses the type change, stop and ask the user. The plan PR links to the epic card as usual.
- **Child cards.** One child **Task** per plan task, created under the epic the first time the epic is executed, assigned to the person executing it. Existing child cards are reused, never duplicated.
- **The approved commit** is the `origin/main` commit that contains the merged epic plan when the epic is first executed, exactly as for a single plan. If the plan is amended later ("When the work changes the plan"), the epic and the children of affected tasks get the new commit; tasks the amendment adds get new child cards.
- The epic's description holds the spec and plan links, the approved commit, the mapping from plan task IDs to child card keys, and the human checks still outstanding.
- Each child card's description holds its plan task ID, the spec and plan links at the approved commit, its acceptance criteria, any human checks, and its dependencies (as task IDs and card keys); its branch and PR are recorded as comments, as for any card.
- **Source of truth:** the approved plan defines scope; Jira holds progress; GitHub decides PR state. When they disagree:
  - a plan task without a child card: create the card;
  - a child card with no plan task: leave it and tell the user;
  - a child card's acceptance criteria edited away from the plan: the plan wins; tell the user, and use the plan-amendment route if the change is wanted;
  - a card status that disagrees with its PR: fix the card from the PR (the Done sweep rule).
- **Epic status:** To Do until first execution, **In Progress** while tasks are worked, **In Review** at "implementation ready", **Done** at "epic complete".
- **Child Done and human checks:** a child moves to Done when its PR merges (the Done sweep). Its human checks move to the epic's "outstanding human checks" list. The epic moves to Done only when every child is Done and the user has confirmed those checks in the conversation; Claude records the confirmation as a comment on the epic (`Human checks confirmed by <name>, <date>`) and never assumes it.

## Executing or resuming an epic

On "execute epic SCRUM-X" or "resume epic SCRUM-X":

1. `git fetch origin`; check the epic plan is on `origin/main` at or after the approved commit. If not, stop: it isn't approved.
2. Reconcile: read the epic and its children, check every recorded PR with `gh pr view`, and run the Done sweep for children whose PRs are merged. Jira must match reality before choosing work.
3. Resume unfinished work that belongs to this execution before starting anything new. A child that is **In Progress**, assigned to the current user, with a branch recorded on the card, belongs to this execution: Claude's own recorded branch is not "someone else working on it". A child In Progress assigned to someone else is theirs: skip it. A child In Progress with no recorded branch: ask the user.
4. Otherwise pick the first **eligible** task in plan order. A task is eligible when:
   - it's in the approved plan;
   - its card isn't Done or In Review;
   - every dependency's PR is merged into `origin/main`;
   - no unresolved blocker or pending decision affects it;
   - no other person or session is working on it.
5. Implement it as one child card, one branch (`<type>/SCRUM-<child>-<slug>` from the latest `origin/main`), one PR, following "Executing a plan (full path)" steps 4-9 for that task: card to In Progress with the branch, implement, verify, review, PR, final report, In Review. Progress comments name the plan task ID instead of `Task <i>/<n>`.
6. Update the epic's progress comment (see "Handover") after each task, so an abrupt stop loses at most the task in hand.
7. Continue with the next eligible task without asking. An open PR is a progress update, not a reason to stop.

### Blockers, overlap and decisions

- **A blocker on one task:** record the reason and the action needed on its card, and continue unaffected tasks.
- **Unexpected file overlap** with a task that is in review or in progress: record it on both cards, and defer the affected task until the other merges.
- **A material decision** (scope, architecture, acceptance criteria, conflicting requirements): raise it when discovered, on the card and to the user; pause the affected tasks, continue unaffected approved ones. A change to the approved plan follows "When the work changes the plan" for the affected tasks.
- Never defer an unmet acceptance criterion to make a task ready for review.

### When Claude pulls the user back in

Claude stops the session when no useful authorised work remains, and says why:

- **Waiting on merges:** every remaining task depends on unmerged PRs. Name the merges that would unblock the most work.
- **A decision is needed** that blocks all remaining work.
- **A blocker stops all progress:** missing access, an unavailable required tool, or a verification failure shared by all remaining tasks.
- **Implementation ready:** every task is merged or awaiting review. List the outstanding human checks.

Two endpoints are reported differently:

- **Implementation ready:** all tasks merged or in review; human checks listed.
- **Epic complete:** all implementation PRs merged and all acceptance criteria met, including human checks the user has confirmed. Only then does the epic move to Done.

### Handover

After each task, and before stopping, Claude updates the epic with one progress comment:

- tasks merged;
- tasks awaiting review, with PR links;
- work in progress, with branch and any uncommitted changes;
- blocked tasks and the action needed, and pending decisions;
- the next eligible task, or why none is eligible.

Detailed reports stay on the child cards. A later "resume epic SCRUM-X" starts from step 1 above, so it continues correctly even if the last handover is missing.

## Changes to make

- **New skill** `.claude/skills/executing-epics/SKILL.md` with the procedure above (classification and plan requirements stay in `CLAUDE.md`; execution lives in the skill). Built with Superpowers `writing-skills`, including pressure scenarios that check a fresh session follows it.
- **`CLAUDE.md`:**
  - "Choosing a path": the combined proposal, task vs. epic, `Path: full — epic`, and that epic membership doesn't widen a request.
  - "Spec and plan": what an epic plan must contain.
  - "Executing an epic": name the rules the skill stays subject to (resolves `TODO.md` SCRUM-17/1) and how epic resume relates to card resume (resolves SCRUM-17/2).
  - "Keeping the board in sync" / "The card": epic and child card formats; the epic's own Done rule.
- **`README.md`:** one line in "How we work" about epics.
- **`verify.ps1`:** nothing new required.

## Decided by the owner

- Build the epic procedure as a repo skill, `executing-epics`, routed from `CLAUDE.md`.
- Classification rule: use an existing Jira epic, otherwise propose an epic only when several separately reviewable PRs are needed; the user confirms; path and epic are separate choices proposed together.
- Claude continues past open PRs and pulls the user back in only when nothing more can move without a human (the four conditions above).
- Dependent tasks wait for real merges; no stacked branches.
- Shared files: the plan orders the tasks; unexpected overlap defers the affected task.
- Material decisions pause affected work, not the whole epic.
- "Implementation ready" and "epic complete" are distinct.
- Handover on the epic plus "resume epic SCRUM-X"; no automatic restarts.
- The demo app (SCRUM-9) is the first real epic.

## Designer's calls (owner to check)

- Plan task IDs are `T1`, `T2`, …; child cards are created when the epic is first executed, not when the plan PR is opened.
- The work's existing card is retyped to Epic when the epic is confirmed; if Jira refuses, Claude asks.
- The approved commit works as for a single plan; amendments refresh the epic and affected children.
- Ownership: In Progress + assigned to the current user + recorded branch = this execution; assigned to someone else = skip; no branch recorded = ask. One session per epic at a time.
- Child cards are assigned to the person executing the epic, so the existing Done sweep finds them.
- Epic status follows To Do → In Progress → In Review (implementation ready) → Done (epic complete).
- Children go Done on merge; their human checks move to the epic, which goes Done only after the user confirms them in conversation and Claude records that on the epic.
- Source of truth: plan for scope, Jira for progress, GitHub for PR state, with the mismatch rules above.
- The epic's progress comment is updated after every task, not only before stopping.
- `Path: full — epic` as the card marker.
- The skill reuses "Executing a plan (full path)" steps 4-9 per task rather than restating them.
- One implementation subagent at a time within a session (no parallel tasks inside a session).

## Left to the builder

- The skill's exact wording and structure, and its pressure scenarios.
- The epic and child card description formats, within the content listed above.
- Exact `CLAUDE.md` wording and placement.
