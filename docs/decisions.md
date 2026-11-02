# Decisions

Big, long-lived decisions a teammate needs months later. One entry per decision, newest first. Added while writing the spec, reviewed in the plan PR.

## 2026-11-02 — Work with Claude through a shared CLAUDE.md, not a queue service

**What:** The team's Claude workflow is described in a checked-in project `CLAUDE.md` and runs alongside Superpowers. The ClaudeQueue service (unattended runs from a Jira column) is dropped.
**Why:** The pitch is about how the team collaborates; a versioned, reviewable process is easier to adopt than a service, and nothing runs unattended.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)

## 2026-11-02 — Plan review is a PR; merging it is the approval

**What:** Spec, plan and decisions go up as a small PR reviewed by a teammate before any code is written. The Jira card is created at approval.
**Why:** Teammates review intent before code, not only diffs after.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)

## 2026-11-02 — Big decisions live in docs/decisions.md; run rulings live on the card

**What:** Long-lived decisions go in this file, written while writing the spec. Rulings made during a run go on the Jira card.
**Why:** Cards are hard to find months later; this file is searchable, reviewed, and read by future Claude sessions.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)

## 2026-11-02 — Jira for now

**What:** Cards live in Jira project `SCRUM`, assigned to the human who owns the work.
**Why:** Already set up; Azure DevOps is a later step.
**Links:** [spec](superpowers/specs/2026-11-02-team-claude-workflow-design.md)
