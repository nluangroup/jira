# Keep Jira in Sync — Design

Date: 2026-11-03
Status: Draft for review
Type: chore

## Intent

The board should show what the team is doing without anyone dragging cards by hand. Today a card only appears when a plan is executed, and only a human moves it to Done, so the board lags behind the work: upcoming work is invisible, and merged work sits in In Review until someone remembers.

The human gate stays where it is: a human merges the PR. Claude only mirrors that merge on the board.

## Goals

- Agreed work appears on the board as a **To Do** card before it is planned.
- A plan PR is linked to its card, and executing the plan picks up that card instead of creating a new one.
- A card whose PR a human has merged moves to **Done** without anyone dragging it.

## Non-goals

- Claude merging PRs, or moving a card to Done for any reason other than a merged PR.
- A background job or service that watches GitHub. The sync happens in Claude sessions.
- Cards for every small question or idea. A card is for work the person has agreed to do.

## Anti-goals

- A card in Done whose PR was not merged (closed unmerged, or still open).
- Two cards for the same piece of work.

## Changes to make

In `CLAUDE.md`:

1. **Cards for agreed work.** When the user agrees to a piece of work that has no card yet, Claude creates a **To Do** card (assignee, summary, a short description ending "No plan yet") and tells the user its key. Search `SCRUM` first so the same work doesn't get two cards.
2. **Link the plan PR to the card.** When opening a plan PR for work that has a card, Claude adds the plan path to the card description (`Plan: <plan path>`, replacing "No plan yet") and comments the plan PR link on the card. The existing "Find the card" step in "Executing a plan" then finds it by the plan path, and only creates a card when none exists.
3. **Done when merged.** At the start of each session's work in this repo, and whenever asked, Claude checks the cards in **In Review**: if the implementation PR named on the card is merged, Claude moves the card to **Done** and comments `PR merged — moved to Done`. If the PR was closed without merging, Claude leaves the card and tells the user.
4. The flow (steps 4 and 7) and the hard-stop line say so: Claude never merges, and moves a card to Done only after a human has merged its PR.

In `README.md`: the "Humans finish" principle and the flow diagram say the same.

## Decided by the owner

- Claude keeps the board up to date as work happens: cards for agreed work, and Done when a PR is merged.
- SCRUM-7 and SCRUM-8 were moved to Done by Claude as a one-off on 2026-11-03, before this rule existed.

## Left to the builder

- Exact wording in `CLAUDE.md` and `README.md`.
