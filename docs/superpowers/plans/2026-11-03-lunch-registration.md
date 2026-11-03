# Lunch Registration — Epic Plan

> **For agentic workers:** this is an epic plan. Execute it with the `executing-epics` skill ("execute epic SCRUM-9"). Each task is executed on its own branch and PR with Superpowers `subagent-driven-development`, given only that task's section plus the shared sections it names (Global Constraints, API contract, Layout). Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Employees register office lunches on a month calendar and beverages per month; the office manager sees and exports monthly totals.

**Architecture:** ASP.NET Core minimal-API app on .NET 10 with EF Core on SQLite, tested with xUnit and `WebApplicationFactory`; Angular 22 standalone app with signals, tested with Vitest, linted with angular-eslint, proxying `/api` to the API in development. Identity is a demo header the API trusts.

**Tech Stack:** .NET 10, ASP.NET Core, EF Core (SQLite), xUnit; Angular 22, Vitest, angular-eslint; PowerShell 7 (`verify.ps1`); GitHub Actions.

**Spec:** `docs/superpowers/specs/2026-11-03-lunch-registration-design.md`

## Epic scope

- **In:** demo identity, my-month calendar, beverage spinner, month totals, manager overview with CSV export, `verify.ps1` and CI for both stacks, C# and Angular standards in `CLAUDE.md`.
- **Out (explicit exclusions):** real authentication, prices or money, month locking, audit history, notifications, deployment, any UI for managing employees.

## Execution order and dependencies

| Task | Title | Type | Depends on |
|---|---|---|---|
| T1 | Solution skeleton and demo identity | feature | — |
| T2 | My-month API | feature | T1 |
| T3 | Month calendar | feature | T1 |
| T4 | Beverage spinner and month totals | feature | T2, T3 |
| T5 | Manager overview and CSV export | feature | T2, T3 |

Order: T1; then T2 and T3 (independent of each other); then T4 and T5 (independent of each other).

**File ordering:** T1 creates every shared file (solution, `Program.cs` endpoint registration hooks, `LunchDbContext` with all entities, `app.routes.ts` with all routes, the nav, the API client base). After T1, each task owns its own files (see Layout); a task that must edit a file owned by another task depends on it. T2 and T5 both register endpoints in `Program.cs`, so T5 depends on T2 (already listed). T4 edits T3's month page, so T4 depends on T3 (already listed). T4 and T5 share no files.

## Global Constraints

- .NET 10 (`net10.0`), C# with nullable enabled and warnings as errors; Angular 22 standalone components with signals, no NgModules.
- Identity header, exactly: `X-Employee-Id`. Missing or unknown id → 401. Non-manager on `/api/reports/*` → 403.
- "Today" is the date in `Europe/Copenhagen`, read through an injected `TimeProvider`.
- Lunch days: no Saturdays or Sundays, no days after today. Beverages: whole number 0-999; no month after the current month.
- CSV: UTF-8 with BOM, `;` separator, header row `Employee;Lunch days;Beverages`, a name starting with `=`, `+`, `-` or `@` is prefixed with `'`.
- SQLite file `src/Lunch.Api/lunch.db`, git-ignored; schema created on start (`EnsureCreated`). Employees seeded from `appsettings.json` (`Employees` section: `Id`, `Name`, `IsOfficeManager`); at least four, one manager.
- API listens on `http://localhost:5080`; the Angular dev server proxies `/api` there.
- Team standards in `CLAUDE.md` apply (T1 adds the C# and Angular ones).
- `pwsh ./verify.ps1` passes after every task.

## Layout

| Path | Owner |
|---|---|
| `Lunch.slnx`, `src/Lunch.Api/Program.cs`, `src/Lunch.Api/Data/*`, `src/Lunch.Api/Identity/*`, `src/Lunch.Api/appsettings.json` | T1 |
| `src/Lunch.Api/MyMonth/*`, `tests/Lunch.Api.Tests/MyMonthTests.cs` | T2 |
| `src/Lunch.Api/Reports/*`, `tests/Lunch.Api.Tests/ReportTests.cs` | T5 |
| `tests/Lunch.Api.Tests/` (project, `IdentityTests.cs`, test factory) | T1 |
| `web/` scaffold, `web/src/app/app.*`, `web/src/app/identity/*`, `web/src/app/api/*` | T1 |
| `web/src/app/month/*` | T3 |
| `web/src/app/beverages/*` | T4 |
| `web/src/app/report/*` | T5 |

## API contract

All routes under `/api`. Errors use `ProblemDetails`. Ids are short lowercase strings (e.g. `anna`). Months are `yyyy-MM`, dates `yyyy-MM-dd`; a malformed month or date → 400.

| Method and route | Header | Success | Errors | Task |
|---|---|---|---|---|
| `GET /api/employees` | none | 200 `[{ "id", "name", "isOfficeManager" }]` | — | T1 |
| `GET /api/me` | required | 200 `{ "id", "name", "isOfficeManager" }` | 401 | T1 |
| `GET /api/me/months/{month}` | required | 200 `{ "month", "lunchDays": ["yyyy-MM-dd"], "beverages": 0 }` | 400, 401 | T2 |
| `PUT /api/me/lunch-days/{date}` | required | 204 (idempotent) | 400 weekend or future, 401 | T2 |
| `DELETE /api/me/lunch-days/{date}` | required | 204 (idempotent) | 400, 401 | T2 |
| `PUT /api/me/months/{month}/beverages` body `{ "count": 0 }` | required | 204 | 400 out of range or future month, 401 | T2 |
| `GET /api/reports/{month}` | required | 200 `{ "month", "rows": [{ "employeeId", "name", "lunchDays", "beverages" }], "total": { "lunchDays", "beverages" } }` | 400, 401, 403 | T5 |
| `GET /api/reports/{month}/csv` | required | 200 `text/csv`, `Content-Disposition: attachment; filename=lunch-{month}.csv` | 400, 401, 403 | T5 |

---

### T1: Solution skeleton and demo identity

**Type:** feature · **Depends on:** — · **Human check:** none

**Acceptance criteria:**
- `Lunch.slnx` with `src/Lunch.Api` and `tests/Lunch.Api.Tests`; `dotnet build` with warnings as errors and `dotnet test` pass.
- `LunchDbContext` with `Employee`, `LunchDay` (employee id + date, unique) and `MonthBeverages` (employee id + month, unique); employees seeded from configuration on start.
- `GET /api/employees` and `GET /api/me` per the API contract; the `X-Employee-Id` handling resolves the current employee for any endpoint and returns 401 when missing or unknown.
- `web/` Angular 22 app: "Who are you?" page listing `/api/employees`; the choice is kept in `localStorage`; an HTTP interceptor adds `X-Employee-Id`; a 401 sends the user back to "Who are you?"; a header shows the current name and "Switch person"; routes `who`, `month` (default), `report` (manager only in the nav) with placeholder components for `month` and `report`.
- `verify.ps1` gains checks `api-build`, `api-test`, `web-lint`, `web-test`, `web-build` (web checks run `npm ci` first when `node_modules` is missing); `.github/workflows/verify.yml` installs .NET 10 and Node 24 before running it.
- `.gitignore` covers `bin/`, `obj/`, `node_modules/`, `web/dist/`, `src/Lunch.Api/lunch.db`.
- `CLAUDE.md` "Coding standards" gains C# rules (target `net10.0`; async/await with `CancellationToken` through call chains, never `.Result`/`.Wait()`; XML doc comments on public types and members; `System.Text.Json`) and Angular rules (standalone components with signals, no NgModules; Vitest tests written alongside the code). The "added here when the stack is chosen" line is removed.
- `README.md` "Verify" lists the new checks; a "Run the app" section starts the API and `ng serve`, and warns that the demo identity is not security.

- [ ] **Step 1:** Write `IdentityTests` (xUnit, `WebApplicationFactory` with an in-memory SQLite connection): `Employees_lists_seeded_employees`, `Me_without_header_is_401`, `Me_with_unknown_id_is_401`, `Me_returns_current_employee`. Run `dotnet test` → fail (no API yet).
- [ ] **Step 2:** Create the solution, API project, `LunchDbContext`, seeding, identity handling and the two endpoints. Run `dotnet test` → pass.
- [ ] **Step 3:** Create the Angular app (`ng new` with Vitest, add angular-eslint), proxy config, identity service and interceptor, "Who are you?" page, header, routes and placeholders, with Vitest tests for the interceptor (adds the header; 401 navigates to `who`) and the picker (lists employees, stores the choice). Run `npm run lint`, `npm test`, `npm run build` in `web/` → pass.
- [ ] **Step 4:** Extend `verify.ps1`, the workflow, `.gitignore`, `CLAUDE.md` and `README.md`. Run `pwsh ./verify.ps1` → all checks `PASS`, exit 0.
- [ ] **Step 5:** Commit: `feat: lunch solution skeleton and demo identity`.

### T2: My-month API

**Type:** feature · **Depends on:** T1 · **Human check:** none

**Acceptance criteria:**
- The four `me` routes for T2 in the API contract, for the current employee only; another employee's data is never read or written.
- `PUT` lunch day: 400 for Saturday/Sunday and for days after today (Europe/Copenhagen, from `TimeProvider`); repeating it is harmless.
- `DELETE` lunch day: repeating it is harmless.
- Beverages: 400 below 0, above 999, or for a month after the current month.
- Tests use a fixed `FakeTimeProvider`.

- [ ] **Step 1:** Write `MyMonthTests`: `Month_is_empty_for_new_employee`; `Tick_and_read_back`; `Tick_twice_is_one_day`; `Untick_removes_day`; `Weekend_is_400`; `Future_day_is_400` (clock fixed at 2026-11-03 12:00 Copenhagen → `2026-11-04` rejected, `2026-11-03` accepted); `Beverages_set_and_read_back`; `Beverages_out_of_range_is_400` (−1, 1000); `Beverages_future_month_is_400`; `Employees_do_not_see_each_other` (anna ticks a day; bo's month is empty); `Malformed_month_is_400`. Run → fail.
- [ ] **Step 2:** Implement the endpoints in `src/Lunch.Api/MyMonth/`, registered from `Program.cs`. Run `dotnet test` → pass.
- [ ] **Step 3:** `pwsh ./verify.ps1` → all `PASS`. Commit: `feat: my-month API`.

### T3: Month calendar

**Type:** feature · **Depends on:** T1 · **Human check:** none (the browser check against real data is in T4)

**Acceptance criteria:**
- The `month` route shows a Monday-first calendar for the current month with previous/next month buttons and the month name.
- Each weekday up to today can be ticked and unticked, calling `PUT`/`DELETE /api/me/lunch-days/{date}` and reflecting the result; weekends and future days are visibly disabled.
- Loads from `GET /api/me/months/{month}`; a failed call shows an error message, not a broken page.
- Vitest tests stub the API at the HTTP level (`HttpTestingController`) against the API contract.

- [ ] **Step 1:** Write Vitest tests: `renders_monday_first_grid`; `shows_ticked_days_from_api`; `tick_calls_put_and_marks_day`; `untick_calls_delete`; `weekend_and_future_days_are_disabled`; `previous_and_next_month_reload`; `api_error_shows_message`. Run → fail.
- [ ] **Step 2:** Implement the month page and service in `web/src/app/month/`, replacing the T1 placeholder. Run lint, tests and build in `web/` → pass.
- [ ] **Step 3:** `pwsh ./verify.ps1` → all `PASS`. Commit: `feat: month calendar`.

### T4: Beverage spinner and month totals

**Type:** feature · **Depends on:** T2, T3 · **Human check:** the presenter runs the API and the web app and checks, as two different people, that ticking days and setting beverages work, persist after an API restart, and that each person sees only their own month.

**Acceptance criteria:**
- A beverage spinner component in `web/src/app/beverages/`, shown on the month page: whole numbers 0-999, saving through `PUT /api/me/months/{month}/beverages`, disabled for future months, with a validation message for out-of-range input.
- The month page shows totals for the month: lunch days ticked and beverages.
- Vitest tests stub the API at the HTTP level.

- [ ] **Step 1:** Write Vitest tests: `shows_beverages_from_month`; `changing_value_saves`; `rejects_negative_and_over_999`; `disabled_for_future_month`; `totals_follow_ticks_and_beverages`. Run → fail.
- [ ] **Step 2:** Implement the component and add it and the totals to the month page. Run lint, tests and build in `web/` → pass.
- [ ] **Step 3:** `pwsh ./verify.ps1` → all `PASS`. Commit: `feat: beverage spinner and month totals`.

### T5: Manager overview and CSV export

**Type:** feature · **Depends on:** T2, T3 · **Human check:** the presenter downloads a CSV for a month with data and opens it in Excel (Danish locale): three columns, Danish letters intact, numbers as numbers.

**Acceptance criteria:**
- `GET /api/reports/{month}` and `/csv` per the API contract: one row per employee (including those with no registrations, as zeros), sorted by name, plus the total; 403 for non-managers.
- CSV format per Global Constraints, including the BOM, `;`, header row and formula escaping.
- The `report` route (shown in the nav only for managers) has a month picker, the table with a total row, and an "Export CSV" button; a non-manager reaching it sees a "managers only" message.
- API tests in `ReportTests`; Vitest tests stub the API.

- [ ] **Step 1:** Write `ReportTests`: `Report_counts_per_employee_and_total`; `Employee_without_registrations_is_zero_row`; `Non_manager_is_403`; `Csv_has_bom_semicolons_and_header`; `Csv_escapes_formula_names` (seed a name starting with `=`); `Malformed_month_is_400`. Run → fail.
- [ ] **Step 2:** Implement `src/Lunch.Api/Reports/`, registered from `Program.cs`. Run `dotnet test` → pass.
- [ ] **Step 3:** Write Vitest tests: `shows_rows_and_total`; `month_picker_reloads`; `export_requests_csv`; `non_manager_sees_message`. Implement `web/src/app/report/`, replacing the placeholder. Run lint, tests and build in `web/` → pass.
- [ ] **Step 4:** `pwsh ./verify.ps1` → all `PASS`. Commit: `feat: manager overview and CSV export`.
