# jira

How our team collaborates with Claude: the thinking is shared and reviewed, the doing is delegated and documented.

The workflow lives in [`CLAUDE.md`](CLAUDE.md). Every Claude session in this repo follows it, so everyone's Claude works the same way. To change how we work, change that file in a PR.

## How we work

```
brainstorm ──► spec + plan + decisions ──► plan PR ──► merged = approved
                                                           │
             human merges, card → Done ◄── PR + card ◄── Claude executes,
                                                         updates Jira card
```

That's the full path, for features and open-ended work. Small, agreed fixes and chores take the light path: a card and one PR, with no plan PR.

- **Plans are reviewed before code.** Features and open-ended work go through a plan PR first: a teammate reviews the spec and plan, and merging it is the approval. Small, agreed fixes and chores go through one PR.
- **Claude documents as it works.** Each piece of agreed work gets a Jira card in `SCRUM` with what was planned, what was done, the decisions made and the PR link.
- **Bigger work can be an epic.** An epic is approved once in a plan PR, then delivered as one PR per task.
- **Big decisions are kept in the repo.** [`docs/decisions.md`](docs/decisions.md) records the decisions a teammate needs months later.
- **Humans finish.** A human merges the PR, and Claude then moves the card to Done. Claude never merges.

## Setup

- [Claude Code](https://claude.com/claude-code) with the Superpowers plugin
- The Atlassian connector in Claude, with access to `nlundgaard.atlassian.net`
- GitHub CLI: `gh auth login`
- [PowerShell 7](https://learn.microsoft.com/powershell/scripting/install/installing-powershell) (`pwsh`), for the verify script

## Repo layout

| Path | What |
|---|---|
| `CLAUDE.md` | The team workflow |
| `TODO.md` | Known follow-ups and review findings not yet fixed |
| `src/Lunch.Api/`, `tests/Lunch.Api.Tests/` | The lunch API and its tests |
| `web/` | The Angular app |
| `docs/decisions.md` | Big, long-lived decisions |
| `docs/superpowers/specs/` | Specs from brainstorming |
| `docs/superpowers/plans/` | Implementation plans |

## Verify

Run `pwsh ./verify.ps1`. It prints `PASS` or `FAIL` for each check and exits 1 if any fail.

| Check | What it makes sure of |
|---|---|
| `links` | Relative links in every tracked `.md` file point to files that exist. |
| `todo` | `TODO.md` items and headings use the agreed format. Card-scoped IDs (`SCRUM-<n>/<k>`) count up from 1 per card, and legacy `#N` items count up from 1, with no gaps. |
| `decisions` | Each entry in `docs/decisions.md` has a dated heading plus What, Why and Links, newest first. |
| `specs` | Every spec has a `Type:` line (feature, fix or chore). |
| `plans` | Every plan has a `**Spec:**` line that names a spec file that exists. |
| `api-build` | `dotnet build Lunch.slnx` succeeds with warnings as errors. |
| `api-test` | `dotnet test Lunch.slnx` passes. |
| `web-lint` | `npm run lint` in `web/` passes (runs `npm ci` first if `node_modules` is missing). |
| `web-test` | `npm test` in `web/` runs the Vitest tests once and they pass. |
| `web-build` | `npm run build` in `web/` succeeds. |

CI runs the same script on every PR and on pushes to `main`, with .NET 10 and Node 24 installed. The `verify` check must pass to merge.

## Run the app

The lunch registration demo is an ASP.NET Core API (`src/Lunch.Api`) and an Angular app (`web/`). You need the .NET 10 SDK and Node 24.

```
dotnet run --project src/Lunch.Api     # http://localhost:5080
cd web && npm ci && npm start          # http://localhost:4200, proxies /api to the API
```

The API creates `src/Lunch.Api/lunch.db` on first start and seeds the employees listed in `src/Lunch.Api/appsettings.json`.

The demo identity is not security. The "Who are you?" page lets anyone pick any employee, and the API trusts the `X-Employee-Id` header it is sent.
