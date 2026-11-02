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

- **Plans are reviewed before code.** A teammate reviews the spec and plan as a small PR. Merging it is the approval.
- **Claude documents as it works.** Each approved plan gets a Jira card in `SCRUM` with the plan, what was done, the decisions made and the PR link.
- **Big decisions are kept in the repo.** [`docs/decisions.md`](docs/decisions.md) records the decisions a teammate needs months later.
- **Humans finish.** Claude never merges and never moves a card to Done.

## Setup

- [Claude Code](https://claude.com/claude-code) with the Superpowers plugin
- The Atlassian connector in Claude, with access to `nlundgaard.atlassian.net`
- GitHub CLI: `gh auth login`

## Repo layout

| Path | What |
|---|---|
| `CLAUDE.md` | The team workflow |
| `docs/decisions.md` | Big, long-lived decisions |
| `docs/superpowers/specs/` | Specs from brainstorming |
| `docs/superpowers/plans/` | Implementation plans |

## Verify

No app yet, so there is no verify command. Until one is added here, Claude stops at the verify step and reports it as a blocker.
