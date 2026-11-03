# Lunch Registration — Design

Date: 2026-11-03
Status: Draft for review
Type: feature

## Intent

A small but real app on the team's own stack (ASP.NET Core + Angular) that the pitch is built around. Employees register the days they had lunch at the office and the beverages they took; the office manager uses the monthly totals for billing or deductions. It is also the first epic run with the `executing-epics` skill: approved once, delivered as one PR per task, with tasks that can run side by side and tasks that wait for merges.

## Goals

- Every employee registers their own lunches and beverages, month by month.
- The office manager sees the totals for any month and exports them.
- The code looks like the team's everyday code: ASP.NET Core Web API with xUnit tests, Angular with Vitest tests, both checked by `verify.ps1` in CI.
- The work splits into tasks with real dependencies, so the epic shows parallel PRs and waiting work.

## Non-goals

- Real authentication or authorisation. Identity is a demo choice (below); the README says so.
- Prices, money, invoices or payroll integration. The manager gets counts.
- Locking a month after export, audit history, notifications.
- Deployment anywhere other than a developer machine.

## Anti-goals

- An employee seeing or changing another employee's registrations through the UI or the API.
- A non-manager reaching the monthly overview or export.
- Registrations lost between app restarts.
- Code that doesn't follow the team standards added to `CLAUDE.md`.

## Users and identity

- Employees are seeded from configuration: id, name, and whether they are the office manager. At least four employees, one of them the office manager.
- **Demo identity:** the Angular app opens on "Who are you?", a list of names. The choice is remembered in the browser and can be changed at any time ("Switch person"). Every API call carries the chosen employee id in a request header; the API trusts it. The README states plainly that this is not security.
- **Status codes:** a missing or unknown employee id → 401; a non-manager on the overview or export → 403. A saved id that no longer exists sends the web app back to "Who are you?".
- Office-manager flags come only from configuration; there is no UI to change them. More than one manager is allowed.

## Employee: my month

- A **month calendar** (Monday-first, the current month by default) with buttons for the previous and next month.
- Each day can be **ticked** (had lunch at the office) or **unticked**. Weekends can't be ticked, and neither can days after today. "Today" is the office's date in Europe/Copenhagen; the API reads it from an injectable clock so tests can fix it.
- A **beverage spinner** for the month: a whole number from 0 to 999, saved for that employee and month. Months after the current month can't be changed.
- The page shows the month's totals: lunch days ticked and beverages.
- Any past month can be corrected.

## Office manager: monthly overview

- A page reachable only by the office manager: pick a month, see one row per employee with lunch days and beverages, and a total row.
- **Export** the month as a CSV file (employee name, lunch days, beverages), opening cleanly in Danish-locale Excel: UTF-8 with a BOM, semicolon-separated, and names starting with `=`, `+`, `-` or `@` escaped so Excel doesn't treat them as formulas.
- The API rejects the overview and export for anyone who isn't the office manager.

## Technical shape

- **API:** ASP.NET Core Web API on .NET 10 in `src/`, with a test project using xUnit and `WebApplicationFactory` for API tests. Storage is SQLite through Entity Framework Core, in one file next to the API; the file is git-ignored.
- **Web:** Angular 22 in `web/`, standalone components and signals, Vitest tests. The dev server proxies `/api` to the API.
- **Rules enforced in the API, tested there:** an employee only reads and writes their own data; no weekend or future lunch days; beverages 0-999; the overview and export only for the office manager.
- **API contract:** the epic plan fixes the API routes, payloads and status codes up front, so T2 (API) and T3 (web) can be built side by side against the same contract. T3's tests stub the API at the HTTP level; the first browser check against real data is at T4, when both are merged.
- **`verify.ps1`:** adds checks for the API (build with warnings as errors, tests) and the web app (install, ESLint via angular-eslint, tests, build), keeping the existing document checks. T1 creates both stacks, so the checks always run after T1. CI installs .NET 10 and Node 24.
- **`README.md`:** how to run the API and the web app together, and the demo-identity warning.
- **`CLAUDE.md`:** C# and Angular coding standards for this repo, taken from the team's existing practice (the presenter's global standards: .NET 10, async/await with `CancellationToken`, XML docs on public types, `System.Text.Json`; Angular: standalone signal-based components, Vitest tests written alongside the code).

## The epic

Five tasks (the plan defines their acceptance criteria and file ordering):

| Task | What | Depends on |
|---|---|---|
| T1 | Solution skeleton: API and test project, Angular app, SQLite, the employee list endpoint and identity header handling, the "Who are you?" picker, `verify.ps1` and CI for both stacks, coding standards in `CLAUDE.md` | — |
| T2 | API: employees, lunch days and monthly beverages for the current employee, with the rules and tests | T1 |
| T3 | Web: month calendar with tickable days | T1 |
| T4 | Web: beverage spinner and month totals | T3 |
| T5 | Manager overview: API totals and CSV export, web page | T1 |

After T1 merges, T2, T3 and T5 can run side by side; T4 waits for T3, whose month page it extends. The plan must keep T4 and T5 from touching the same web files, or order them. (Amended 2026-11-03: dependencies cut to the real ones, so an unattended run gets further; see `docs/decisions.md`.)

Human checks: the presenter looks at the calendar and spinner in a browser (once T2, T3 and T4 are merged) and opens an exported CSV in Excel (once T2, T3 and T5 are merged).

## Decided by the owner

- Stack: C#/.NET and Angular, like the team's.
- The app: lunch registration — a month calendar to tick office lunches, a beverage spinner per month.
- Every employee registers their own; the office manager uses the totals.
- Demo identity: pick your name from a list.
- Full path, organised as an epic (SCRUM-9).

## Designer's calls (owner to check)

- SQLite through EF Core, one file, git-ignored; employees seeded from configuration.
- No weekend or future lunch days; beverages 0-999; past months can be corrected.
- Manager overview with a total row and a CSV export; no prices.
- The app lives in this repo: `src/` (API and tests) and `web/` (Angular).
- The identity travels in a request header; the API trusts it. Missing or unknown id → 401, non-manager on manager routes → 403. Manager flags only from configuration; several allowed.
- T1 owns the employee list endpoint, the identity header handling and the picker, so T2 and T3 can start in parallel.
- The plan fixes the API contract up front; T3 tests against a stubbed API; the first real-data browser check is at T4.
- "Today" is Europe/Copenhagen, from an injectable clock in the API. Beverages can't be set for months after the current one.
- CSV for Danish-locale Excel: UTF-8 with BOM, semicolons, formula-injection escaping.
- ESLint via angular-eslint; Node 24 in CI.
- The task split and dependencies above (dependencies amended by the owner on 2026-11-03).

## Left to the builder

- Project names and component structure (the plan fixes the API contract).
- How the schema is created (migrations or create-on-start).
- Calendar look and layout within the behaviour above.
- Exact `verify.ps1` step names, as long as each stack's build, tests and lint are separate checks.
