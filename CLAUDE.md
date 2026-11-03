# How we work with Claude

This file is the team's workflow. Every Claude session in this repo follows it. Changing the process means changing this file in a PR the team reviews.

Humans think, decide and review. Claude does the work and documents it.

## The flow

There are two paths (see "Choosing a path"). The steps below are the full path; the light path is in "The light path".

1. **Think together.** Brainstorm a spec and a plan with Superpowers (`brainstorming`, then `writing-plans`).
2. **Record big decisions** in `docs/decisions.md` while writing the spec.
3. **Review intent.** Spec, plan and decision entries go up as one plan PR. A human reviews it. Merging it is the approval.
4. **Card.** Claude creates a To Do card when work is agreed, so it may already exist. If not, Claude creates it when the plan is executed.
5. **Do the work** on an implementation branch. Claude updates the card as tasks finish.
6. **Hand over.** Claude opens the implementation PR, posts the final report and moves the card to **In Review**.
7. **Humans finish.** A human reviews and merges the PR. Claude then moves the card to **Done**.

## Rules for Claude

### Before designing

- Read `docs/decisions.md`. Don't re-propose what the team has decided against; if a decision needs revisiting, say so explicitly.

### Spec and plan

- Spec in `docs/superpowers/specs/`, plan in `docs/superpowers/plans/`.
- While writing the spec, add an entry to `docs/decisions.md` for each big, long-lived decision a teammate would need months later (format below). Small rulings don't go there.
- The spec's header includes a `Type: feature | fix | chore` line (pick one).
- Put spec, plan and decision entries on a branch `plan/<topic>`, push it and open a PR with `gh pr create`. Then stop: no code is written until that PR is merged.
- When opening the plan PR, link the work's card as in "Keeping the board in sync".
- An epic plan goes through the same plan PR; merging it approves every task it lists. It contains:
  - the epic's goal, scope and explicit exclusions;
  - for each task: an ID (`T1`, `T2`, …), title, type (feature, fix or chore), acceptance criteria, and dependencies by task ID;
  - the intended execution order;
  - any verification that needs a human, per task;
  - file ordering: when two tasks touch the same files, which goes first (the second depends on the first).
- Each epic task is small enough for one reviewable PR.

### Branches

| Branch | For |
|---|---|
| `plan/<topic>` | The plan PR: spec, plan and decision entries |
| `feature/SCRUM-<n>-<slug>` | Implementing an approved feature plan |
| `fix/SCRUM-<n>-<slug>` | Implementing an approved fix plan or an agreed light fix |
| `chore/SCRUM-<n>-<slug>` | Implementing an approved chore plan or an agreed light chore: tooling, dependencies, docs, `CLAUDE.md` |

- The type comes from the spec's `Type:` line (full path) or is the one agreed with the path (light path).
- Every change gets a card and a PR. Full-path changes go through a plan PR first; light-path changes go straight to an implementation branch.
- Once a branch's PR is merged, never push to it again. Follow-up work starts a new branch. Create every new branch, including a new `plan/<topic>` for a revised plan, fresh from `origin/main`; don't reuse an old local branch of the same name.
- GitHub enforces PR-only changes to `main`: PRs only, merge commits only, no force pushes or deletion; merged branches are deleted automatically. GitHub does not stop a push that recreates a merged branch.

### Choosing a path

- **Light:** bounded fixes and chores whose approach is agreed, and whose what and why fit in a PR description. Process changes to `CLAUDE.md` count.
- **Full:** every new feature, and any fix or chore whose approach is still open and benefits from a brainstorm before code exists.
- The number of files touched doesn't decide the path.
- On the full path, work is a **task** or an **epic**. A task is one bounded change delivered in one implementation PR, however many steps it takes. An epic is several tasks contributing to one outcome, each with its own acceptance criteria and possibly dependencies. Epics are always full path.
- If the work already belongs to a Jira epic, use that grouping. Otherwise propose an epic only when delivery needs several separately reviewable PRs; many steps alone never make an epic, and a complex single task can take the full path without being an epic.
- When the work is agreed, make one proposal covering both choices (path, and task or epic for full work), for example "I recommend the full path, organised as an epic", and let the user confirm once. Record it on the card as a comment, exactly `Path: light`, `Path: full` or `Path: full — epic`. When unsure, ask.
- Belonging to an epic doesn't widen a request. "Fix SCRUM-23" works on that one child task, as an epic child task ("Executing an epic"); only an explicit "execute epic SCRUM-X" or "resume epic SCRUM-X" authorises working through the epic's plan.

### The light path

1. Find or create the card (as in "Keeping the board in sync"). Comment `Path: light` if it isn't there yet, and replace a trailing `No plan yet` in the description with `Path: light — no plan`.
2. If the card already shows a branch, an open PR or someone working on it, stop and ask whether to resume or coordinate. If the user asked to resume this card's work, use its existing branch instead of creating one.
3. `git fetch origin`, then create `<type>/SCRUM-<n>-<short-slug>` from `origin/main`; `<type>` is agreed with the path. If the checkout has unrelated uncommitted work, use a separate worktree. Never discard anyone's changes.
4. Move the card to **In Progress** and comment the branch name.
5. Make the change directly, without per-task subagents. If it makes a big, long-lived decision, add the `docs/decisions.md` entry in the same branch.
6. Run the repo's verify command (see `README.md`) before every commit and before opening the PR. The work is done when the PR's "done when" is met, verify passes, and the card and docs are up to date; verify passing alone isn't enough.
7. Dispatch one reviewer subagent on the branch diff, via Superpowers `requesting-code-review`. Fix Critical and Important findings; add the rest to `TODO.md`.
8. Push and open the PR with `gh pr create`. The description says what changes, why, and what must be true when it's done, and links the card.
9. Post the final report on the card (format in "The card") and move it to **In Review**.

If the change outgrows the light path (the approach becomes open), stop and comment `Path: full` on the card, then switch to the full path on the same card:

- Keep the light branch local: don't push it, and close its PR if one is open. Don't delete it. Comment on the card: `Light branch <name> kept local; superseded by the plan`.
- Write the spec and plan on a fresh `plan/<topic>` branch from `origin/main`, with documentation only, and open the plan PR.
- After it's merged, create a fresh implementation branch from `origin/main` and carry over only the light-branch changes that fit the approved plan. Then delete the light branch.

Never approve or merge a PR; move the card to **Done** only after a human has merged it.

### Executing a plan (full path)

When asked to execute a plan:

1. `git fetch origin`. Execute the plan and spec **as they are on `origin/main`**. If the plan isn't there, it isn't approved: say so and stop. Local or unmerged changes to either document are not approved; don't execute them.
2. Find the card: use the work's card if it's already known; otherwise search `SCRUM` with JQL `project = SCRUM AND text ~ "\"<plan path>\""`. If there is none, create it (see "The card") and tell the user its key. If the card is an Epic or says `Path: full — epic`, use "Executing an epic" instead. Update its description to the format in "The card" (spec and plan links, `Approved commit:`), replacing a bare `Plan:` line.
3. If the card shows an implementation branch, an open PR or someone already working on it, stop and ask whether to resume or coordinate. A light branch the card marks as superseded by the plan doesn't count. If the user asked to resume this card's work, use its existing branch and skip step 4.
4. Create a branch `<type>/SCRUM-<n>-<short-slug>` from `origin/main`, where `<type>` is the spec's `Type:` line. If the spec has no `Type:` line, stop and ask. If the current checkout has unrelated uncommitted work, use a separate worktree. Never discard or overwrite anyone's changes. If the card switched from the light path, carry over the kept light branch's changes as "The light path" says.
5. Move the card to **In Progress** and comment the branch name.
6. Execute the plan with Superpowers (`subagent-driven-development`). After each plan task, comment on the card: `Task <i>/<n> done — <task title>` and the commit short SHA.
7. Run the repo's verify command (see `README.md`) before every commit and before opening the PR. The work is done when every goal in the spec is met, verify passes, and the card and docs are up to date; verify passing alone isn't enough.
8. Push the branch and open the PR with `gh pr create`. The PR body links the card.
9. Post the final report comment on the card and move it to **In Review**.

Never approve or merge a PR. Move a card to **Done** only after a human has merged its PR. Never push to `main`.

### Executing an epic

When asked to execute or resume an epic ("execute epic SCRUM-X", "resume epic SCRUM-X"), use the `executing-epics` skill. It stays subject to the rules of "Executing a plan (full path)", for every task it runs:

- execute only the plan and spec as on `origin/main`; an unmerged plan is not approved;
- run verify before every commit and before opening a PR;
- never approve or merge a PR, and never push to `main`;
- move a card to **Done** only after a human has merged its PR (the epic itself: see "The card").

"Resume epic SCRUM-X" resumes the whole epic. "Resume" for a single card still means that card's work only.

The skill is required. If it's unavailable, stop and report the blocker (see "When something is missing or fails"); don't fall back to "Executing a plan".

An epic child task, whether run by the skill or asked for on its own ("Fix SCRUM-23"), follows "Executing a plan (full path)" with these differences:

- "the plan" is that task's section of the epic plan; `subagent-driven-development` gets only that section;
- the card is the child card; step 2 keeps its child description (see "The card") and doesn't rewrite it to the single-plan format;
- if any of the task's dependency PRs isn't merged into `origin/main`, say so and stop;
- progress comments name the task ID: `T2 done — <task title>` and the short SHA;
- the work is done when the task's acceptance criteria are met (not every spec goal), verify passes and the card is up to date; the final report's `Requirements:` line lists those criteria, each met or not.

### When the work changes the plan

Make implementation choices within the approved scope, and list them as rulings in the final report. If a discovery materially changes scope, acceptance criteria, architecture or a recorded decision, stop the affected work, comment on the card, and open an updated spec and plan as a plan PR, on a fresh `plan/<topic>` branch and linked to the same card. Once it's merged, merge `origin/main` into the implementation branch, refresh the card's `Spec:`, `Plan:` and `Approved commit:` lines to the new approved commit, and resume. For an epic, those lines are on the epic; also refresh the affected children's spec and plan links to the new commit and create child cards for tasks the amendment adds.

### When review asks for changes

The person who owns the card resumes the work on the same branch. Move the card back to **In Progress**, make the changes, run verify, push, post a new final report comment and move the card to **In Review** again.

### When something is missing or fails

If a required skill, connector, permission or the verify command is unavailable or fails, stop and report the blocker. Don't substitute another tool, skip the step or claim the work is done. A verify failure counts as failing whether this work caused it or it was already there; say which.

### Keeping the board in sync

- **Cards for agreed work.** When the user agrees to a piece of work that has no card, search `SCRUM` first so the same work doesn't get two cards. If none exists, create a **To Do** card (see "The card"): assignee, summary and a short description ending `No plan yet`. The summary is the agreed work's name. Tell the user its key.
- **Plan PR (full path).** When opening a plan PR, use the work's card if it's already known (from earlier in the session, a path change or a revised plan), keeping its links and history. Otherwise search `SCRUM` for it: assigned to the user, description ending `No plan yet` or `Path: light — no plan`, matching summary (or ask the user). If none exists, create it as in "Cards for agreed work". Replace a trailing `No plan yet` or `Path: light — no plan` with `Plan: <plan path>`, and comment the plan PR link on the card.
- **Epics.** The epic card is the work's card: an existing card is changed to the **Epic** issue type when the epic is confirmed (if Jira refuses, stop and ask the user), and a new one is created as an Epic. The retype happens when the classification is confirmed, before the plan PR. The plan PR step above still replaces `No plan yet` with `Plan: <plan path>` on the epic. At first execution the epic's description is rewritten to the epic format in "The card", which keeps the spec and plan links, so step 2 of "Executing a plan (full path)" still finds it by plan path. Child cards are described in "The card".
- **Done when merged.** At the start of each session's work in this repo, and whenever asked, search the user's own cards in **In Review**: `project = SCRUM AND status = "In Review" AND assignee = currentUser()`. Take the PR link from the card's latest final report `PR:` line. Skip cards whose PR belongs to another repository than this one (`gh repo view --json nameWithOwner`). Skip Epic-type cards: their Done rule is in "The card". Check the PR with `gh pr view <link> --json state`. If merged, move the card to **Done** and comment `PR merged — moved to Done`. If closed without merging, leave the card and tell the user. If still open, leave it. If a card has no `PR:` line, leave it and tell the user.

### The card

- Jira site `https://nlundgaard.atlassian.net`, cloudId `2cf169bb-d580-472b-b2b0-49d9c66c6364`, project `SCRUM`, issue type Task (Epic for epic cards). Use the Atlassian connector.
- Assignee: the current user's accountId from `atlassianUserInfo`. Cards belong to the person doing the work.
- Summary: the agreed work's name, or the plan title when a card is created at plan execution. Don't rename an existing card.
- Epic and child cards (see "Executing an epic"):
  - The epic card's description holds the spec and plan links, `Approved commit:`, the mapping from plan task IDs to child card keys, and the human checks still outstanding.
  - Child cards are issue type Task, created under the epic the first time it is executed, one per plan task, and assigned to the person executing the epic (unlike other cards, where the assignee is who is doing the work, this is what lets the Done sweep find them). Reuse existing child cards; never duplicate. A child's description holds its plan task ID, the spec and plan links at the approved commit, its acceptance criteria, any human checks, and its dependencies (task IDs and card keys). Its branch and PR go in comments.
  - Epic status: To Do until first execution, **In Progress** while tasks are worked, **In Review** when implementation is ready, **Done** when the epic is complete.
  - A child moves to **Done** when its PR merges, and its human checks move to the epic's outstanding list. The epic moves to **Done** only when every child is Done and the user has confirmed those checks in the conversation; then comment on the epic exactly `Human checks confirmed by <name>, <date>`. Never assume the confirmation. If no human checks are outstanding, the epic moves to **Done** when every child is Done, without that comment.
- Light-path description: a 2-3 sentence summary of the agreed work, then `Path: light — no plan`.
- Full-path description (`<repo>` from `gh repo view --json nameWithOwner`; `<sha>` is the `origin/main` commit that was executed):

  ```
  <2-3 sentence summary from the plan's Goal>

  Spec: https://github.com/<repo>/blob/<sha>/<spec path>
  Plan: https://github.com/<repo>/blob/<sha>/<plan path>
  Approved commit: <sha>
  ```

- Final report comment:

  ```
  Done: <one line per plan task (full path) or per change (light path)>
  Requirements: <each spec goal (full path) or the PR's "done when" (light path), met or not>
  Verification: <pass/fail, test counts>
  Rulings: <implementation choices made within scope, or "none">
  Deferred: <TODO.md IDs added by this work, or "none">
  Decisions: <links to docs/decisions.md entries, if any>
  PR: <link> (branch <name>)
  ```

### decisions.md entries

Newest first:

```
## YYYY-MM-DD — <decision as a short title>

**What:** <one or two sentences>
**Why:** <one or two sentences>
**Links:** [spec](<relative path>), card <SCRUM-n if known>
```

On the light path there is no spec: link the card and the implementation PR instead.

### TODO.md

`TODO.md` in the repo root is the team's list of known follow-ups: review findings not fixed in the PR, and issues found during the work that are outside the plan's scope.

- Append new items with the work's branch, so they are reviewed in its PR. Read `TODO.md` before planning work in the same area.
- Give each new item a card-scoped ID, `SCRUM-<n>/<k>`: `<n>` is the card the work belongs to, and `<k>` counts from 1 for that card. Branches for different cards can't collide. Items `#1`–`#17` predate this and keep their numbers.
- Mark finished items `[x]`; never delete them.
- Only the main Claude session writes to `TODO.md`. Subagents report findings to it.

```
## <Category> — YYYY-MM-DD
- [ ] **SCRUM-<n>/<k> — <description>** (`path/to/file:~line`)
  <details if needed>
```

Categories: Critical, High Priority, Medium Priority, Low Priority, Architecture.

## Coding standards

- No comments unless the *why* is non-obvious; well-named identifiers explain the what.
- No defensive error handling for scenarios that cannot happen; validate only at system boundaries.
- No speculative abstractions: implement what the plan asks, nothing more.
- Prefer editing existing files over creating new ones.

### C#

- Target `net10.0`.
- Use `async`/`await` and thread a `CancellationToken` through async call chains; never block with `.Result` or `.Wait()`.
- Public types and members have XML doc comments.
- Use `System.Text.Json`.

### Angular

- Standalone components with signals; no NgModules.
- Write Vitest tests alongside the code.

## Precedence

For the team workflow and coding standards, this file takes precedence over personal or global `CLAUDE.md` preferences. It does not override platform security rules or tool permissions.

- The Superpowers workflow replaces personal subagent routing, review-escalation loops and iteration limits.
- `TODO.md` follows the rules in this file.
- Personal preferences may add to this file but never conflict with it. Team standards belong here, so everyone's Claude writes code the same way.
