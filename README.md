# ClaudeQueue

A .NET 10 worker that watches the SCRUM Jira board for cards in **Ready for Claude**. For each card it clones the demo repo, has Claude Code execute the approved plan the card points to, runs `verify.ps1`, pushes a `plan/SCRUM-<n>-<slug>` branch, opens a pull request (draft if verification or Claude failed) and posts a report comment. The card ends in **In Review**. One card runs at a time.

## Prerequisites

- .NET 10 SDK
- PowerShell 7 (`pwsh`)
- git
- GitHub CLI: `gh auth login` then `gh auth setup-git`
- Claude Code CLI, logged in, with the Superpowers plugin installed

## Configuration

Jira credentials live in user secrets:

```
dotnet user-secrets set Queue:JiraEmail <email> --project src/ClaudeQueue
dotnet user-secrets set Queue:JiraApiToken <token> --project src/ClaudeQueue
```

The service refuses to start without them. Other settings are in `src/ClaudeQueue/appsettings.json` (section `Queue`).

## Board setup

On the SCRUM board add a column/status **Ready for Claude** between **To Do** and **In Progress**.

## Card format

The card description must contain these three lines (anywhere):

```
Queue-Repo: nluangroup/agentic-demo
Queue-Plan: docs/plans/<plan>.md
Queue-Commit: <full 40-character SHA that contains the plan>
```

Cards naming any other repo, or with a missing or malformed block, are reported as `Invalid card` and moved to In Review.

## Run

```
dotnet run --project src/ClaudeQueue
```

Drag a card to Ready for Claude; it is picked up within a few seconds.

## Logs

Run files land in `RunsDirectory` (default `%LOCALAPPDATA%\ClaudeQueue\runs`): one clone directory per run, with `<run>-claude.jsonl` (Claude's event stream) and `<run>-verify.log` siblings.

## Rerun a card

Drag it from In Review back to Ready for Claude. The next run uses a new `-run<N>` branch and opens a new PR.

## Stop

Press Ctrl+C. A card that is mid-run stays In Progress; move it by hand.
