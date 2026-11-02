# Team Claude Workflow — Design

Date: 2026-11-02
Status: Draft for review
Type: feature

## Intent

A pitch to colleagues for **how we collaborate as a team using Claude**. It
replaces the earlier ClaudeQueue idea (an unattended .NET service that ran
approved plans from a Jira column); that service and its repo contents are
deleted.

The pitch: the team's way of working with Claude is written down in a shared,
checked-in `CLAUDE.md` and runs alongside Superpowers. Humans think, decide
and review; Claude does the work and documents it. Two ideas carry it:

1. **Collaboration starts before code.** A spec and plan are reviewed by a
   teammate as a small PR. Merging that PR is the approval. Teammates review
   intent, not just diffs.
2. **The process is code.** The workflow lives in a versioned project
   `CLAUDE.md`. Everyone's Claude follows the same process; changing the
   process is a PR the team reviews.

## Goals

- A project `CLAUDE.md` that makes Claude follow the team flow below without
  being asked at each step.
- A `docs/decisions.md` log for big, long-lived decisions a teammate needs
  months later.
- A Jira card per approved plan that, a week after merge, holds: the plan,
  what was done, the decisions made, and the PR link.
- Demo material for a pitch session (see "The pitch").

## Non-goals

- Any service, queue, poller or unattended/headless execution.
- Azure DevOps integration (Jira for now; the team's real tracker is Azure
  DevOps, and moving is a later step).
- A separate Jira user for Claude. Cards are assigned to the human who owns
  the work; Claude updates them as that user via the Atlassian connector.
- Recording every small ruling in `decisions.md`.

## Anti-goals (failure even if it technically works)

- Claude merging a PR or moving a card to Done.
- Code written before the plan PR is merged.
- Decisions that only exist in a chat transcript.
- A pitch that looks like "Claude writes documentation" rather than a way of
  working together.
- The team process living in one person's personal/global `CLAUDE.md`.

## The flow

1. **Think together.** Someone brainstorms a spec and plan with Claude and
   Superpowers (`brainstorming`, then `writing-plans`).
2. **Record big decisions.** While writing the spec, Claude adds an entry to
   `docs/decisions.md` for each big, long-lived decision (see below).
3. **Review intent.** Spec, plan and the `decisions.md` entries go up as one
   small PR. A teammate reviews it. **Merging it is the approval.**
4. **Card at approval.** When the plan is approved, Claude creates a Jira card
   in project `SCRUM`, assigned to the person who owns the work, with a short
   summary and links to the spec and plan on GitHub at the approved commit.
5. **Do the work.** Claude executes the plan in a normal interactive session
   on a feature branch (Superpowers execution skills), updating the card as
   plan tasks finish.
6. **Close the loop.** Claude opens the implementation PR and completes the
   card: what was done (per task, plus the verify/test result), rulings made
   during the run, the PR link and branch.
7. **Humans finish.** A human reviews and merges the PR and moves the card to
   Done.

### Card contents

| Section | Content |
|---|---|
| Plan | Short summary + links to spec and plan at the approved commit (not the plan text pasted in) |
| What was done | Plan tasks and their outcome; final verification result |
| Decisions | Rulings Claude made during the run (Superpowers' "Rulings I made"); links to any `decisions.md` entries |
| PR | Implementation PR link and branch |

### Where decisions go

| Where | What |
|---|---|
| `docs/decisions.md` | Big, long-lived decisions: date, one line of what, one line of why, links to spec and card |
| Spec | The full reasoning behind them |
| Jira card | Rulings made during the run, what was done, PR link |

`decisions.md` entries are written while writing the spec, so they are
reviewed in the plan PR. Future Claude sessions read `decisions.md` so they
don't re-propose what the team already decided against.

### Where the rules live

In the project's checked-in `CLAUDE.md`, written from scratch. It must be
self-contained: colleagues don't have the presenter's personal global
`CLAUDE.md`. Where the presenter's global file conflicts (its subagent
Default Workflow, `TODO.md` rules), the project file wins.

## The pitch

| Beat | Live / prepared |
|---|---|
| A brainstorming session on a small feature: how thinking happens with Claude, how decisions land in the spec and `decisions.md` | Prepared and rehearsed; run live from a script, with a recording as backup |
| The plan PR: spec, plan, `decisions.md` entry, with review comments (by the owner, standing in for a teammate) | Prepared |
| **Live moment:** merge a small plan PR on stage, say "start the plan", and Claude creates the card by itself | Live (~30 s) |
| The finished implementation PR and the filled-in card | Prepared |
| Close on the project `CLAUDE.md`: "this file is how our team works; changing it is a PR" | Live |

Rehearse at least twice end to end.

## Constraints

- Jira site `https://nlundgaard.atlassian.net` (cloudId
  `2cf169bb-d580-472b-b2b0-49d9c66c6364`), project `SCRUM`, team-managed
  simple board. Statuses: To Do, Ready for Claude, In Progress, In Review,
  Done. The board is empty.
- Claude reaches Jira through the claude.ai Atlassian connector, and GitHub
  through `gh`.
- Superpowers is installed for everyone who uses the workflow.

## Decided by the owner

- Ditch the queue service; use `CLAUDE.md` workflows with Superpowers.
- Jira for now.
- Cards assigned to the owner's own Jira user; no separate agent user.
- Card created when the plan is approved.
- Card holds plan, what was done, decisions, PR link.
- Plan review as a PR (merge = approval); shared project `CLAUDE.md` as the
  team's process.
- `docs/decisions.md` for big decisions, added while writing the spec.
- Pitch: prepared brainstorm, prepared plan PR and finished PR, plus the live
  card-creation moment.
- Delete the old ClaudeQueue folder entirely and start from scratch here
  (`C:\Users\nlu\jira`, fresh git repo, remote `nluangroup/jira`, recreated
  empty on 2026-11-02).
- This repo is the demo codebase: the workflow `CLAUDE.md`, `decisions.md`,
  specs, plans and the app the demo feature changes all live here.
  `nluangroup/agentic-demo` is not used.
- A `TODO.md` in the repo root holds review findings not fixed in the PR
  and out-of-scope follow-ups; the card's final report lists their numbers.
  Its rules are in `CLAUDE.md`.
- The owner reviews the prepared plan PR themselves; on stage they explain
  that this is the teammate's role.

## Designer's calls (owner to check)

- Card statuses: created in **To Do**; Claude moves it to **In Progress** when
  execution starts and **In Review** when the PR opens; never to Done. The
  **Ready for Claude** column is no longer used and is removed by hand.
- Trigger for card creation: Claude has no way to notice a merge, so the rule
  is "when asked to execute a plan, check the plan PR is merged to `main`;
  if no card exists for that plan yet, create it first". The live moment
  relies on this. Claude finds an existing card by searching `SCRUM` for the
  plan path in the description, so the check is safe to repeat across
  rehearsals.
- Code is hosted on GitHub (where the existing repos are), even though the
  team's real tooling is Azure DevOps; both move together later.
- Card updates: one comment per finished plan task, plus a final report
  comment; the description holds the plan section.
- If rehearsals show Claude skipping card updates, add a small Claude Code
  hook as a reminder. Not built up front.
- This design follows its own process: its decisions are logged in
  `docs/decisions.md`.

## Left to the builder

- Wording and structure of `CLAUDE.md`, and the exact card comment format.
- The `decisions.md` entry format, within the fields above.
- The small app in this repo that the demo feature changes (kept tiny, with
  tests and a one-command verify), and the feature used for the prepared
  brainstorm and PRs.
- Branch naming.
