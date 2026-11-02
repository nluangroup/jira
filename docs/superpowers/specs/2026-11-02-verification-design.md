# Verification — Design

Date: 2026-11-02
Status: Draft for review
Type: chore
Revised: 2026-11-02 — the owner dropped Pester. There is no `tests` check and no PowerShell test dependency; `verify.ps1` is checked by hand against throwaway fixtures while it is built.

## Intent

`CLAUDE.md` makes "the verify command passes" the definition of done, but the repo has no verify command, so every plan stops at a blocker or invents its own checks. Add one now. Until there is an app, the repo is its process documents, so verify checks that they follow the team's rules. CI runs it on every PR and GitHub requires it to pass before merging: the process is enforced, not just documented.

## Goals

- One command, `pwsh ./verify.ps1`, run the same way locally and in CI.
- It checks the process documents (below).
- It needs nothing beyond PowerShell 7 and git.
- A GitHub Actions workflow runs it on every PR to `main` and on pushes to `main`.
- The `main` ruleset requires it to pass.
- `README.md` describes how to run it and what it checks.
- When the app arrives, its build and tests are added to the same script; `CLAUDE.md` and CI stay the same.

## Non-goals

- Markdown style linting, spell checking, external link checking.
- Checking Jira (card exists, status) from verify.
- Automated tests for `verify.ps1` itself, and any PowerShell test framework (Pester). Each check is exercised by hand against throwaway fixtures while it is built, and the results go in the implementation report.

## Anti-goals

- A check that fails on the repo as it is after the branch-policy plan is executed.
- A check that trips on the templates in `CLAUDE.md` (placeholders inside code blocks).
- Different results locally and in CI.

## Checks

Each check prints `PASS: <name>` or `FAIL: <name> — <file>:<line> <reason>` (one line per problem). The script exits 1 if any check fails, else 0.

| Name | Rule |
|---|---|
| `links` | In every tracked `*.md` file, every relative Markdown link target exists (path relative to the file; `#fragment` stripped). Ignore `http:`, `https:`, `mailto:` and pure `#anchor` links. Ignore fenced code blocks and inline code spans. |
| `todo` | In `TODO.md`: item lines match `- [ ] **#N — <text>**` or `- [x] **#N — <text>**`; numbers start at 1 and increase by 1 in file order; section headings match `## <Category> — YYYY-MM-DD` with Category one of Critical, High Priority, Medium Priority, Low Priority, Architecture. A file with no items passes. |
| `decisions` | In `docs/decisions.md`: every `## ` heading matches `## YYYY-MM-DD — <title>`; each entry has `**What:**`, `**Why:**` and `**Links:**` lines; dates never increase going down the file (newest first). |
| `specs` | Every `docs/superpowers/specs/*.md` has a line `Type: feature`, `Type: fix` or `Type: chore`. |
| `plans` | Every `docs/superpowers/plans/*.md` has a `**Spec:**` line naming a file that exists. |

The existing team-workflow spec has no `Type:` line; add `Type: feature` to it as part of this work.

## CI

- `.github/workflows/verify.yml`, job id `verify`, on `pull_request` to `main` and `push` to `main`, `ubuntu-latest`, runs `./verify.ps1` with PowerShell 7 (preinstalled on the runner).
- After the workflow has run green on the implementation PR and that PR is merged, add `verify` as a required status check on the `main` ruleset. This is a GitHub setting, applied with the owner's go-ahead.

## README

The "Verify" section says: run `pwsh ./verify.ps1` (PowerShell 7); one line per check; CI runs it on every PR and it must pass to merge; the app's build and tests will be added to the same script. "Setup" lists PowerShell 7.

## Order

Execute after the branch-policy plan (it introduces the `Type:` line this checks).

## Left to the builder

- Internal structure of `verify.ps1` (functions, regexes), as long as the output format and check names above hold.
