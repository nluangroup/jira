# TODO

Known follow-ups and review findings not yet fixed. Rules and format are in `CLAUDE.md`.

## Medium Priority — 2026-11-02
- [ ] **#1 — Links in `<...>` with spaces are never checked** (`verify.ps1`, `links`)
  `[x](<my file.md>)` is skipped, so a broken link of that form passes verify.
- [ ] **#2 — Wrong-case paths pass on Windows but fail in CI** (`verify.ps1`, `links` and `plans`)
  Linux paths are case-sensitive. Say so in the README's Verify section, or compare case-sensitively everywhere.

## Low Priority — 2026-11-02
- [ ] **#3 — Fences toggle on any ``` or ~~~ line** (`verify.ps1`, `links`)
  Remember the opening marker and close only on the same one; nested or mixed fences mis-track today.
- [ ] **#4 — Parenthesised link targets and reference-style links are not handled** (`verify.ps1`, `links`)
- [ ] **#5 — File names with `"`, `\`, tab or newline are still quoted by git** (`verify.ps1`, `Get-TrackedFiles`)
  Use `git ls-files -z` and split on NUL.
- [ ] **#6 — A tracked file deleted from the working tree crashes verify** (`verify.ps1`, `links`)
  Only on a dirty local tree; the crash exits non-zero.
- [ ] **#7 — TODO item pattern is not anchored at the end** (`verify.ps1`, `todo`)
  Text after the closing `**` is accepted.
- [ ] **#8 — Duplicate TODO numbers are reported as "found #N, expected #M"** (`verify.ps1`, `todo`)
  Say "duplicate" when the number was already used.
- [ ] **#9 — A `Type:` line inside a code block or prose counts** (`verify.ps1`, `specs`)
- [ ] **#10 — `**Spec:**` as a Markdown link is rejected; unquoted paths with spaces don't resolve; a folder target says "not found"** (`verify.ps1`, `plans`)
- [ ] **#11 — The verify workflow has no `permissions: contents: read`** (`.github/workflows/verify.yml`)

## Low Priority — 2026-11-03
- [x] **#12 — Card summary rule differs for early cards** (`CLAUDE.md`, "Keeping the board in sync" and "The card")
  A card created when work is agreed uses the work's name; "The card" says the summary is the plan title, and "Find the card" doesn't update the summary.
- [x] **#13 — Templates assume a plan** (`CLAUDE.md`, "The card" final report and "decisions.md entries")
  "Done: <one line per plan task>" and `[spec](...)` don't fit light-path changes; say what to use instead (one line per change, link the PR).
- [x] **#14 — Branch table says "approved … plan" for every row** (`CLAUDE.md`, "Branches")
  The bullet below clarifies it; the table could read "approved plan or agreed light change".
- [x] **#15 — README still describes only the plan-PR flow** (`README.md`, "How we work")
  The diagram and "Each approved plan gets a Jira card" predate the light path.
- [x] **#16 — "Executing a plan" heading isn't labelled full path** (`CLAUDE.md`)
  Its trigger and "The flow" already scope it; append "(full path)" next time the section is edited.
- [x] **#17 — Switching light to full leaves `Path: light — no plan` on the card** (`CLAUDE.md`, "The light path" and "Keeping the board in sync")
  The plan-PR lookup matches `No plan yet`, so a later session might not find the card. Restore `No plan yet` (or name the card key) when switching.
- [ ] **SCRUM-15/1 — After a light-to-full switch the card stays In Progress** (`CLAUDE.md`, "The light path")
  On the full path a card waits in To Do until execution; say whether to move it back while the plan PR is in review.
- [ ] **SCRUM-15/2 — Switching to full doesn't say what to do with a pushed light branch** (`CLAUDE.md`, "The light path")
  If its PR was open, the branch is on the remote; say whether to leave or delete it.
- [ ] **SCRUM-15/3 — Resuming on the light path comments the branch name again** (`CLAUDE.md`, "The light path" step 4)
- [x] **SCRUM-17/1 — "approval, verification and merge rules" doesn't name the rules** (`CLAUDE.md`, "Executing an epic")
  They live in "Executing a plan (full path)": approved plan on `origin/main`, verify before every commit and PR, never approve or merge, never push to `main`. Name them, or say they apply to every task the skill runs, when the skill lands (SCRUM-16).
- [x] **SCRUM-17/2 — Epic "resume" vs. card "resume"** (`CLAUDE.md`, "Executing an epic"; the `executing-epics` skill)
  The light path and "Executing a plan" use "resume this card's work" for one card; the skill should say how resuming an epic relates.
- [ ] **SCRUM-16/1 — Mixed stops are covered only by the general stop line** (`.claude/skills/executing-epics/SKILL.md`, "Stop conditions")
  One task blocked while the rest wait on merges; S8 shows it copes, but the stop report could name both reasons explicitly.
- [ ] **SCRUM-16/2 — Retyping to Epic assumes the work's card already exists** (`CLAUDE.md`, "Keeping the board in sync")
  "Cards for agreed work" normally creates it first; say so in the Epics bullet.
- [ ] **SCRUM-18/1 — `/api/employees/` (trailing slash) returns 401** (`src/Lunch.Api/Identity/`)
  The identity exemption matches the exact path only.
- [ ] **SCRUM-18/2 — Changed employee names or manager flags never reach an existing `lunch.db`** (`src/Lunch.Api/Data/EmployeeSeeder.cs`)
  The seeder only inserts missing ids; update on start, or note it in the README.
- [ ] **SCRUM-18/3 — The stored employee object can hold a stale manager flag** (`web/src/app/identity/`)
  Refresh it from `/api/me` on start.
- [ ] **SCRUM-18/4 — The interceptor adds `X-Employee-Id` to every request, not only `/api`** (`web/src/app/identity/`)
- [ ] **SCRUM-18/5 — No foreign key from lunch days and beverages to employees** (`src/Lunch.Api/Data/LunchDbContext.cs`)

## Low Priority — 2026-11-05
- [ ] **SCRUM-20/1 — "Today" is computed once per calendar page** (`web/src/app/month/month.ts:~50`)
  A page left open past Copenhagen midnight keeps the new day disabled until reload.
- [ ] **SCRUM-20/2 — The grid stays clickable after a failed load** (`web/src/app/month/month.html`)
  Ticked days show unticked after a load error; hide or disable the grid while `loadFailed()` is set.
- [ ] **SCRUM-20/3 — Missing calendar tests: pending state, save error, Dec→Jan and Jan→Dec** (`web/src/app/month/month.spec.ts`)
  The code handles all three; only coverage is missing.
- [ ] **SCRUM-20/4 — Day buttons are named only by the day number** (`web/src/app/month/month.html`)
  Add an `aria-label` with the full date for screen readers.
