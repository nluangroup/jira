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

### Branches

| Branch | For |
|---|---|
| `plan/<topic>` | The plan PR: spec, plan and decision entries |
| `feature/SCRUM-<n>-<slug>` | Implementing an approved feature plan |
| `fix/SCRUM-<n>-<slug>` | Implementing an approved fix plan |
| `chore/SCRUM-<n>-<slug>` | Implementing an approved chore plan: tooling, dependencies, docs, `CLAUDE.md` |

- The type comes from the spec's `Type:` line (full path) or is the one agreed with the path (light path).
- Every change gets a card and a PR. Full-path changes go through a plan PR first; light-path changes go straight to an implementation branch.
- Once a branch's PR is merged, never push to it again. Follow-up work starts a new branch. Create every new branch, including a new `plan/<topic>` for a revised plan, fresh from `origin/main`; don't reuse an old local branch of the same name.
- GitHub enforces PR-only changes to `main`: PRs only, merge commits only, no force pushes or deletion; merged branches are deleted automatically. GitHub does not stop a push that recreates a merged branch.

### Choosing a path

- **Light:** one clear change whose what and why fit in a PR description. Process changes to `CLAUDE.md` count.
- **Full:** new features, work spanning several parts of the code, or anything where the approach is still open and benefits from a brainstorm before code exists.
- When the work is agreed, propose a path and let the user confirm. Record it on the card as a comment, exactly `Path: light` or `Path: full`. When unsure, ask.

### The light path

1. Find or create the card (as in "Keeping the board in sync"), move it to **In Progress** and comment the branch name.
2. Create `<type>/SCRUM-<n>-<short-slug>` from `origin/main`; `<type>` is agreed with the path.
3. Make the change directly, without per-task subagents. If it makes a big, long-lived decision, add the `docs/decisions.md` entry in the same branch.
4. Run the repo's verify command before every commit.
5. Dispatch one reviewer subagent on the branch diff. Fix Critical and Important findings; append the rest to `TODO.md`.
6. Push and open the PR with `gh pr create`. The description says what changes and why, and links the card.
7. Post the final report on the card (format in "The card"; `Rulings` may be "none") and move it to **In Review**.

If the change grows beyond light (the approach becomes open, or it spans several parts of the code), stop, comment on the card, and switch to the full path: write a spec and plan and open a plan PR.

Never approve or merge a PR; move the card to **Done** only after a human has merged it.

### Executing a plan

When asked to execute a plan:

1. `git fetch origin`. Execute the plan and spec **as they are on `origin/main`**. If the plan isn't there, it isn't approved: say so and stop. Local or unmerged changes to either document are not approved; don't execute them.
2. Find the card: search `SCRUM` with JQL `project = SCRUM AND text ~ "\"<plan path>\""`. If there is none, create it (see "The card") and tell the user its key. If one is found, update its description to the format in "The card" (spec and plan links, `Approved commit:`), replacing the bare `Plan:` line.
3. If the card shows an implementation branch, an open PR or someone already working on it, stop and ask whether to resume or coordinate.
4. Create a branch `<type>/SCRUM-<n>-<short-slug>` from `origin/main`, where `<type>` is the spec's `Type:` line. If the spec has no `Type:` line, stop and ask. If the current checkout has unrelated uncommitted work, use a separate worktree. Never discard or overwrite anyone's changes.
5. Move the card to **In Progress** and comment the branch name.
6. Execute the plan with Superpowers (`subagent-driven-development`). After each plan task, comment on the card: `Task <i>/<n> done — <task title>` and the commit short SHA.
7. Run the repo's verify command (see `README.md`) before every commit and before opening the PR. It passing is the definition of done.
8. Push the branch and open the PR with `gh pr create`. The PR body links the card.
9. Post the final report comment on the card and move it to **In Review**.

Never approve or merge a PR. Move a card to **Done** only after a human has merged its PR. Never push to `main`.

### When the work changes the plan

Make implementation choices within the approved scope, and list them as rulings in the final report. If a discovery materially changes scope, acceptance criteria, architecture or a recorded decision, stop the affected work, comment on the card, and open an updated spec and plan as a plan PR. Resume that work after it is merged.

### When review asks for changes

The person who owns the card resumes the work on the same branch. Move the card back to **In Progress**, make the changes, run verify, push, post a new final report comment and move the card to **In Review** again.

### When something is missing or fails

If a required skill, connector, permission or the verify command is unavailable or fails, stop and report the blocker. Don't substitute another tool, skip the step or claim the work is done. A verify failure counts as failing whether this work caused it or it was already there; say which.

### Keeping the board in sync

- **Cards for agreed work.** When the user agrees to a piece of work that has no card, search `SCRUM` first so the same work doesn't get two cards. If none exists, create a **To Do** card (see "The card"): assignee, summary and a short description ending `No plan yet`. The summary is the agreed work's name. Tell the user its key.
- **Plan PR (full path).** When opening a plan PR, first search `SCRUM` for the work's card: assigned to the user, description ending `No plan yet`, matching summary (or ask the user). If none exists, create it as in "Cards for agreed work". Then replace `No plan yet` with `Plan: <plan path>` and comment the plan PR link on the card.
- **Done when merged.** At the start of each session's work in this repo, and whenever asked, search the user's own cards in **In Review**: `project = SCRUM AND status = "In Review" AND assignee = currentUser()`. Take the PR link from the card's latest final report `PR:` line and check it with `gh pr view <link> --json state`. If merged, move the card to **Done** and comment `PR merged — moved to Done`. If closed without merging, leave the card and tell the user. If still open, leave it. If a card has no `PR:` line, leave it and tell the user.

### The card

- Jira site `https://nlundgaard.atlassian.net`, cloudId `2cf169bb-d580-472b-b2b0-49d9c66c6364`, project `SCRUM`, issue type Task. Use the Atlassian connector.
- Assignee: the current user's accountId from `atlassianUserInfo`. Cards belong to the person doing the work.
- Summary: the plan title.
- Description (`<repo>` from `gh repo view --json nameWithOwner`; `<sha>` is the `origin/main` commit that was executed):

  ```
  <2-3 sentence summary from the plan's Goal>

  Spec: https://github.com/<repo>/blob/<sha>/<spec path>
  Plan: https://github.com/<repo>/blob/<sha>/<plan path>
  Approved commit: <sha>
  ```

- Final report comment:

  ```
  Done: <one line per plan task>
  Verification: <pass/fail, test counts>
  Rulings: <implementation choices made within scope, or "none">
  Deferred: <TODO.md item numbers added by this work, or "none">
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

### TODO.md

`TODO.md` in the repo root is the team's list of known follow-ups: review findings not fixed in the PR, and issues found during the work that are outside the plan's scope.

- Append new items with the work's branch, so they are reviewed in its PR. Read `TODO.md` before planning work in the same area.
- Number items sequentially across the whole file (`#N`); never reuse a number.
- Mark finished items `[x]`; never delete them.
- Only the main Claude session writes to `TODO.md`. Subagents report findings to it.

```
## <Category> — YYYY-MM-DD
- [ ] **#N — <description>** (`path/to/file:~line`)
  <details if needed>
```

Categories: Critical, High Priority, Medium Priority, Low Priority, Architecture.

## Coding standards

- No comments unless the *why* is non-obvious; well-named identifiers explain the what.
- No defensive error handling for scenarios that cannot happen; validate only at system boundaries.
- No speculative abstractions: implement what the plan asks, nothing more.
- Prefer editing existing files over creating new ones.

Language-specific standards are added here when the stack is chosen.

## Precedence

For the team workflow and coding standards, this file takes precedence over personal or global `CLAUDE.md` preferences. It does not override platform security rules or tool permissions.

- The Superpowers workflow replaces personal subagent routing, review-escalation loops and iteration limits.
- `TODO.md` follows the rules in this file.
- Personal preferences may add to this file but never conflict with it. Team standards belong here, so everyone's Claude writes code the same way.
