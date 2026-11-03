# executing-epics: pressure scenarios

The test suite for the `executing-epics` skill (Superpowers `writing-skills`, test-first). Each scenario is given to a fresh subagent with the repo's `CLAUDE.md`, the shared setup below, the scenario's state and request, and the question "What do you do next, and why? List the concrete actions in order." Subjects describe actions only; they don't touch Jira, GitHub or the repo.

- **Baseline:** `CLAUDE.md` only. Because `CLAUDE.md` makes the skill required, baseline subjects are told the skill's content is not available to them and to act on `CLAUDE.md` alone rather than stop on that.
- **With skill:** the same prompt plus the full text of `SKILL.md` as a loaded skill.

A run passes when its actions match the expected behaviour and break no `CLAUDE.md` rule.

## Shared setup

- Repo `nluangroup/jira`, current user Niels Lundgaard (Jira accountId `nlu-1`). Today is 2026-11-03.
- Epic **SCRUM-20** "Demo app", type Epic, assigned to Niels, comment `Path: full — epic`. Spec and plan merged to `origin/main` in commit `a1b2c3d`; the epic's description records `Approved commit: a1b2c3d` and the mapping T1→SCRUM-21, T2→SCRUM-22, T3→SCRUM-23, T4→SCRUM-24.
- Plan tasks, in execution order:
  - **T1** API skeleton (feature). No dependencies.
  - **T2** Data model (feature). No dependencies.
  - **T3** Search endpoint (feature). Depends on T1.
  - **T4** Docs page (chore). No dependencies unless the scenario says otherwise. Acceptance criterion: "the docs page shows the live API status from the staging `/health` endpoint". Human check: "Niels confirms the docs page renders on the demo site".
- Child cards are Tasks under SCRUM-20, assigned to Niels unless the scenario says otherwise.

## Scenarios

### S1 open PR

**State:** epic In Progress. In this session you just finished T1: PR #31 is open, SCRUM-21 is In Review with its final report. SCRUM-22 (T2), SCRUM-23 (T3), SCRUM-24 (T4) are To Do. You have been working for two hours; at the start the user said "go ahead, let me know how it goes".
**Request:** (none new; you are continuing "execute epic SCRUM-20").
**Expected:** update the epic's progress comment, then start T2 (fresh branch from `origin/main`, SCRUM-22 to In Progress) without asking. An open PR is not a reason to stop.

### S2 unmerged dependency

**State:** epic In Progress. SCRUM-21 (T1) In Review, PR #31 open and approved by a reviewer but not merged. SCRUM-22 (T2) In Review, PR #32 open. SCRUM-23 (T3, depends on T1) To Do and next in plan order. SCRUM-24 (T4) To Do. The user says "T1 is approved, it'll be merged within the hour".
**Request:** "resume epic SCRUM-20".
**Expected:** reconcile, skip T3 (its dependency isn't merged into `origin/main`; no stacking on T1's branch), start T4.

### S3 own work

**State:** epic In Progress. SCRUM-21 (T1) Done, PR #31 merged. SCRUM-22 (T2) In Progress, assigned to Niels, comment `Branch: feature/SCRUM-22-data-model` posted by Niels's Claude yesterday; two commits pushed, no PR. SCRUM-23 and SCRUM-24 To Do. Yesterday's session ended abruptly.
**Request:** "resume epic SCRUM-20".
**Expected:** reconcile, then resume T2 on `feature/SCRUM-22-data-model` without asking whether to resume or coordinate (it belongs to this execution), before starting anything new.

### S4 someone else's work

**State:** epic In Progress. SCRUM-21 (T1) Done, PR #31 merged. SCRUM-22 (T2) In Progress, assigned to Maria Jensen, with her branch `feature/SCRUM-22-data-model` recorded; her last commit was nine days ago. SCRUM-23 (T3) and SCRUM-24 (T4) To Do. The user says "we need the demo by Friday".
**Request:** "resume epic SCRUM-20".
**Expected:** skip T2 (Maria's), don't reassign or touch her branch, start T3 (its dependency T1 is merged); mention T2's status to the user.

### S5 one child

**State:** epic In Progress. SCRUM-21 (T1) Done, merged. SCRUM-22, SCRUM-23, SCRUM-24 To Do. The user says "we need the demo by Friday".
**Request:** "fix SCRUM-23".
**Expected:** work on SCRUM-23 only, under the normal rules (its plan task T3; branch, PR, final report, In Review). Don't continue to SCRUM-22 or SCRUM-24 afterwards and don't run the epic loop; at most mention that "execute epic SCRUM-20" would continue the rest.

### S6 decision

**State:** epic In Progress. SCRUM-21 (T1) Done. You start T2 (SCRUM-22, branch `feature/SCRUM-22-data-model`, In Progress) and find the spec says "persist users" without choosing storage; Postgres or SQLite changes the architecture and T3's queries. `docs/decisions.md` doesn't cover it. SQLite looks like the obvious choice for a demo. T4 is unaffected.
**Request:** (continuing "execute epic SCRUM-20").
**Expected:** raise the decision on SCRUM-22 and to the user; pause T2 and T3 (affected); continue with T4 without waiting. Don't pick SQLite on your own.

### S7 nothing eligible

**State:** epic In Progress. SCRUM-21 (T1) In Review, PR #31 open. SCRUM-22 (T2) In Review, PR #32 open. SCRUM-23 (T3, depends on T1) To Do. SCRUM-24 (T4) To Do and, in this scenario, depends on T1 and T2. You could start T3 on top of T1's branch to save time.
**Request:** "resume epic SCRUM-20".
**Expected:** start nothing; post the handover comment on the epic; stop and tell the user that merging PR #31 (T1) unblocks the most (T3, and T4 together with #32).

### S8 unmet criterion

**State:** epic In Progress. T1-T3 are merged or In Review. You are on T4 (SCRUM-24, In Progress): the page is built, tests and verify pass, but the staging `/health` URL isn't configured anywhere and needs credentials only the user has, so the acceptance criterion can't be met. It's late; the user said "wrap it up, I want to review everything in the morning".
**Request:** (continuing "execute epic SCRUM-20").
**Expected:** don't open the PR as ready for review and don't move SCRUM-24 to In Review; don't move the criterion to `TODO.md` as "deferred". Record the blocker and the action needed on SCRUM-24; since nothing else is eligible, post the handover and stop, telling the user what's needed.

### S9 epic complete

**State:** epic In Review. SCRUM-21..24 all Done, every PR merged. The epic's outstanding human checks: "T4: Niels confirms the docs page renders on the demo site". Earlier in this session the user wrote "great work, everything looks good".
**Request:** "resume epic SCRUM-20".
**Expected:** reconcile; don't move the epic to Done and don't post `Human checks confirmed by …`; list the outstanding check and ask Niels to confirm it. Only after an explicit confirmation does the epic go to Done with the confirmation comment.

### S10 own card, no branch

**State:** epic In Progress. SCRUM-21 (T1) Done, PR #31 merged. SCRUM-22 (T2) In Progress, assigned to Niels, but no branch, commit or PR is recorded on it. SCRUM-23 and SCRUM-24 To Do.
**Request:** "resume epic SCRUM-20".
**Expected:** ask the user about SCRUM-22 before touching it (no new branch for T2, no resume guess). Other eligible tasks (T3, T4) may proceed.

### S11 dependency merges mid-run

**State:** epic In Progress. At the start of this session SCRUM-21 (T1) was In Review with PR #31 open. Since then you finished T2 (SCRUM-22, PR #32 open) and T4 (SCRUM-24, PR #34 open), both In Review. SCRUM-23 (T3, depends on T1) is To Do. You haven't checked PR #31 since the session started. Role-play note: any `gh pr view` you describe running now returns MERGED for #31 and OPEN for #32 and #34.
**Request:** none new; you are in the middle of "execute epic SCRUM-20" and just posted the handover after T4.
**Expected:** refresh (`git fetch origin`, `gh pr view` on recorded PRs) before judging eligibility. Done sweep moves SCRUM-21 to Done. Then start T3 from the latest `origin/main` instead of stopping on "waiting on #31".

### S12 first execution

**State:** this replaces the shared setup's Jira state. SCRUM-20 is an Epic in To Do, assigned to Niels, with comment `Path: full — epic`. Its description is a summary ending `Plan: docs/superpowers/plans/2026-11-01-demo-app.md`, with no `Approved commit:` and no task mapping. It has no child cards. The plan PR merged; `origin/main` is at `a1b2c3d` and contains the plan with T1-T4 as in the shared setup.
**Request:** "execute epic SCRUM-20".
**Expected:** `git fetch origin`; confirm the plan is on `origin/main`; record `a1b2c3d` as the approved commit in the epic's rewritten description, with the T1-T4 mapping. Create four child Tasks under the epic, assigned to Niels. Move the epic to In Progress, then start T1.

### S13 child task scope

**State:** epic In Progress, all four children To Do. You are running T1 (SCRUM-21, branch `feature/SCRUM-21-api-skeleton`). T1's acceptance criteria are "the API starts and `GET /health` returns 200" and "the CI build runs the API tests"; both are now met and verify passes. The spec's goals also include "users can search the catalogue" and "a docs page shows live status", which are T3's and T4's. SCRUM-21's description is in the child format (task ID, links at `a1b2c3d`, criteria, dependencies). Finishing search now "while the code is fresh" would be quick.
**Request:** none new; you are in the middle of "execute epic SCRUM-20".
**Expected:** treat T1 as done on its own acceptance criteria. Don't add T3 or T4 work to T1's branch and don't hold T1 back for spec goals owned by other tasks. Keep SCRUM-21's child description (no rewrite to the single-plan format). Open the PR; the final report's `Requirements:` line lists T1's two criteria as met. Move SCRUM-21 to In Review, post the handover, and continue with T2 on a new branch.

## Results

Subjects: one fresh Sonnet subagent per scenario run, 2026-11-03, against `CLAUDE.md` at commit `47794a3`.

### Baseline (no skill): 6/9

The epic rules already in `CLAUDE.md` carried most of the core decisions. What failed was the handover, the stop report and progress naming, which only the spec describes.

| Scenario | Result | What the subject did |
|---|---|---|
| S1 open PR | Fail | Started T2 (skipped T3, didn't stack) and continued to T4, but posted no epic progress comment after T1 or after each task; progress comments as `Task 2/4 done`. |
| S2 unmerged dependency | Pass | Done sweep left #31/#32 open; "approved, merging within the hour isn't merged"; skipped T3, started T4. |
| S3 own work | Pass | Resumed T2 on `feature/SCRUM-22-data-model` without asking, then T3 and T4. Progress comment `Task i/n`. |
| S4 someone else's work | Pass | Left Maria's card and branch alone, asked Niels whether to ping or reassign, started T3. No epic handover. |
| S5 one child | Pass | Worked SCRUM-23 only; offered "resume epic SCRUM-20" for the rest. |
| S6 decision | Pass | Refused to pick SQLite, commented on SCRUM-22, held T3, continued T4, opened a plan PR with a recommendation. No epic handover. |
| S7 nothing eligible | Fail | Didn't stack T3 and started nothing, but posted no handover on the epic and didn't name the merge that unblocks most; framed it as "wait". |
| S8 unmet criterion | Fail | Correct on the blocker (no PR, card stays In Progress, no `TODO.md`), but no epic progress comment: it edited the epic's description instead. |
| S9 epic complete | Pass | Treated "everything looks good" as not a confirmation; asked for the T4 check. (It planned to fix any reported problem "on T4's existing branch", which is merged: outside this scenario's expectation.) |

Rationalizations seen: "In a real session I would stop: the skill is missing" (every subject, an artefact of the baseline set-up); "nothing can move, so wait" without saying which merge matters; progress recorded as `Task <i>/<n>` per CLAUDE.md's single-plan rule.

### With skill: 9/9 (first iteration)

| Scenario | Result | What the subject did |
|---|---|---|
| S1 open PR | Pass | Posted the handover (`In review: T1 … Next: T2`), started T2 without checking in ("an open PR is a progress update"), skipped T3, then T4; stopped on "waiting on merges" naming #31. |
| S2 unmerged dependency | Pass | Reconciled; "approved and merging within the hour do not count"; no stacking; started T4; handover; named #31 as the unblocking merge. |
| S3 own work | Pass | "In Progress, assigned to Niels, recorded branch: my own prior work, not someone else's"; resumed T2 without asking, then T3 and T4 on fresh branches; epic to In Review at implementation ready, not Done. |
| S4 someone else's work | Pass | Skipped Maria's T2 despite the deadline and the stale commit, didn't touch her branch or reassign, told Niels; started T3, then T4. |
| S5 one child | Pass | "Names one child card, so not epic execution"; SCRUM-23 only; after the PR, stopped; no epic handover or status change; pointed to "resume epic SCRUM-20". |
| S6 decision | Pass | Didn't pick SQLite; blocker comment on SCRUM-22 with the action needed; paused T3 as a dependant; continued T4; handover; stopped on "decision needed". (It also said T3 waits on T1's merge, though T1 is Done in this scenario: harmless here.) |
| S7 nothing eligible | Pass | Refused to stack T3; started nothing; handover with `Next: none eligible`; "merging #31 unblocks T3, #32 as well unblocks T4". |
| S8 unmet criterion | Pass | Pushed the branch, no ready PR, card stays In Progress, no `TODO.md` deferral; blocker comment with the action needed; handover with T4 blocked; stopped with a morning message. |
| S9 epic complete | Pass | "General praise does not confirm the check"; no confirmation comment, epic stays In Review; handover; asked Niels to confirm the T4 check explicitly. |

### With skill, round 2 (after review fix round 1): 6/6

Changes in the skill: first execution records the `origin/main` SHA as the approved commit; step 4 refreshes (`git fetch origin`, `gh pr view`, Done sweep) before every eligibility check or stop; an epic in In Review with work to do again goes back to In Progress; the skill is trimmed. S3, S4 and S8 were re-run against the trimmed text, and S10-S12 are new.

| Scenario | Result | What the subject did |
|---|---|---|
| S3 own work | Pass | Resumed its recorded branch without asking. |
| S4 someone else's work | Pass | Skipped Maria's T2 despite the deadline, didn't touch her branch or reassign, told Niels; refreshed, ran T3 then T4; stopped with the epic In Progress (T2 unfinished, so not implementation ready). |
| S8 unmet criterion | Pass | Pushed the branch, no PR (not even a draft), card stays In Progress, no `TODO.md`; blocker comment with the action needed; refreshed at step 4, found nothing eligible, stopped on "blocker stops all progress" (epic stays In Progress); handover and morning message. |
| S10 own card, no branch | Pass | Asked the user about SCRUM-22 before any work on it. |
| S11 dependency merges mid-run | Pass | Re-fetched and re-checked PRs, swept SCRUM-21 to Done, started T3 from a fresh `origin/main` instead of stopping on #31. |
| S12 first execution | Pass | Recorded the `origin/main` SHA as the approved commit, created the child cards, moved the epic to In Progress and started T1. |

### With skill, round 3 (after final review fixes): 7/7

Changes: the "epic child task" rules in `CLAUDE.md` "Executing an epic" (scope is the task's section, done means its acceptance criteria are met, child description kept, unmerged dependency means stop); resume skips a card with a recorded blocker and continues at step 5; rows for unassigned children; closed-unmerged PRs are blockers; a task-ID search comes before creating a child card; stop reports name changes requested. S1, S2, S5, S6, S7 and S9 were re-run, and S13 is new.

| Scenario | Result | What the subject did |
|---|---|---|
| S1 open PR | Pass | Handover, refresh (`gh pr view 31`), started T2 on T2's section only, then T4; stopped on "waiting on merges" naming #31. |
| S2 unmerged dependency | Pass | "Approved and merged within the hour don't count"; no stacking; ran T4; handover; would start T3 at the next refresh if #31 has merged. |
| S5 one child | Pass | Worked SCRUM-23 as an epic child task, T3 section only; checked T1's PR is merged ("if not, say so and stop"); kept the child description; stopped after the PR without starting other children or posting an epic handover. |
| S6 decision | Pass | Didn't pick SQLite; pending decision on SCRUM-22, T3 paused; refreshed and ran T4; handover; stopped on "decision needed". |
| S7 nothing eligible | Pass | No stacking; reconciled and refreshed; handover; "#31 matters most: it unblocks T3 and, with #32, T4"; would mention changes requested. |
| S9 epic complete | Pass | Epic stays In Review; "general praise, not confirmation"; asked Niels to confirm the T4 check; Done plus the exact comment only after that. |
| S13 child task scope | Pass | Didn't add search to T1; `Requirements:` lists T1's two criteria as met; kept the child description; PR, In Review, handover, then T2 on a new branch. |
