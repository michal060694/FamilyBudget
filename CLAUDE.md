# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A private household finance web app (Hebrew-first, RTL) for one family: annual budget smoothing, monthly
income/expense tracking, tithe (מעשר/חומש) accounting, savings funds, and a debts ledger. See `PRODUCT.md`
for product intent/tone and `DESIGN.md` for the visual design system (colors, typography, component rules)
— consult `DESIGN.md` before touching anything in `wwwroot/index.html`'s markup or CSS.

The governing spec is `.specify/memory/constitution.md`. It is the source of truth for domain rules
(budget smoothing formula, tithe formula, fund balance invariants) — read it before changing any
calculation logic in `FamilyBudget.Core`. The database is PostgreSQL, accessed exclusively through
EF Core (Constitution Principle III, amended 2026-07-17 to match the actual production deployment);
SQLite is used only as the in-memory test double — see Testing below.

## Commands

Build, from the repo root:
```
dotnet build
```

Run all tests:
```
dotnet test
```

Run a single test (xUnit fully-qualified name filter):
```
dotnet test --filter "FullyQualifiedName~BudgetSmoothingEngineTests.CalculateAllocation_WhenOverrun_AddsToRequiredAmount"
```

Run the API locally (serves the SPA from `wwwroot/` and the JSON API on the same origin):
```
dotnet run --project src/FamilyBudget.Api
```
Basic Auth middleware is only wired up when `BasicAuth:Username`/`BasicAuth:Password` are
configured; the checked-in `Development` config leaves them empty, so local runs have no auth by
default (see Program.cs) — set them via `dotnet user-secrets` if you need to exercise auth
locally. No frontend build step exists —
`wwwroot/index.html` is a single hand-written HTML/CSS/vanilla-JS file with no bundler; edit it directly
and reload.

Apply/generate EF Core migrations (run from repo root, targeting the API project so config resolves):
```
dotnet ef migrations add <Name> --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Api
dotnet ef database update --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Api
```
Migrations are also applied automatically at startup via `Database.Migrate()` in `Program.cs`.

Docker image (matches the Render deployment): `docker build .` — see `Dockerfile`. The container listens
on `$PORT` (default 8080) and expects `ASPNETCORE_ENVIRONMENT=Production`.

## Architecture

Three-project clean layering, dependencies point inward only (enforced by Constitution Principle II):

- **`FamilyBudget.Core`** — domain entities (`Entities/`), repository interfaces (`Abstractions/`), and
  all business logic (`Services/`): budget smoothing, tithe engine, fund/debt calculations. Zero
  dependency on Infrastructure or API. Every domain service here must be unit-testable without a
  database or HTTP host — that's what `tests/FamilyBudget.Core.Tests` exercises directly.
- **`FamilyBudget.Infrastructure`** — EF Core `DbContext` (`Persistence/FamilyBudgetDbContext.cs`),
  migrations (`Migrations/`), and repository implementations (`Repositories/`), one per Core interface.
  Uses Npgsql (PostgreSQL) as the EF provider.
- **`FamilyBudget.Api`** — ASP.NET Core minimal API. `Endpoints/*.cs` map HTTP routes (one file per
  feature area: AnnualBudget, Dashboard, Transaction, MonthlyOverview, MonthlyExpenseBudget,
  FixedDonationStandingOrder, Fund, Debt, MonthlyTemplate, Export); `Contracts/*.cs` hold the
  request/response DTOs per feature area, kept separate from Core entities. `Program.cs` wires up DI,
  runs migrations at startup, gates the whole site behind a single shared Basic Auth credential (no
  per-user accounts — see below), and serves the static `wwwroot/` SPA. `Services/ExcelExportService.cs`
  (ClosedXML) is the one Infrastructure-ish concern that lives in Api rather than Infrastructure.

Each feature area typically spans all three layers plus tests: an entity/service in Core, a repository
in Infrastructure, an endpoint+contract in Api, and a test file per project. When adding a feature,
follow that same vertical slice rather than introducing a new architectural pattern.

**Auth model**: no user accounts or per-request authorization exist. A single username/password pair
(`BasicAuth:Username`/`BasicAuth:Password` config, via env vars or user-secrets locally) gates every
route except `/healthz` (which must stay open for the Render health check). The app refuses to start in
`Production` if these aren't configured. `Development` runs with no auth at all.

**Frontend**: `src/FamilyBudget.Api/wwwroot/index.html` is the entire client — no separate frontend
project, no npm/build pipeline. It calls the JSON API via `fetch` on relative paths and re-renders
sections client-side. Follow `DESIGN.md`'s component/color/spacing rules exactly when editing it (e.g.
one board color per card by money-direction meaning, tabular numerals on all money values, RTL Hebrew).

## Testing

- `tests/FamilyBudget.Core.Tests` — pure unit tests against Core services/entities, no database.
- `tests/FamilyBudget.Api.Tests` — integration tests via `WebApplicationFactory<Program>` (see
  `FamilyBudgetApiFactory` in `DashboardEndpointTests.cs`, shared as an `IClassFixture` across the test
  files in that project). It swaps the DbContext to an in-memory SQLite connection for the test run;
  this works because the Postgres-flavored migration SQL (`uuid`, `numeric(18,2)`) still applies under
  SQLite's loose type affinity.
- Constitution Principle IV requires that any change touching fund balance or tithe calculation include
  a test proving the invariant still holds (fund earmark sum == fund balance; `NetTitheDue` nets out this
  month's fixed donations + prior month's ad-hoc donations, never further back).

## Domain rules worth knowing before touching calculation code

- **Budget smoothing** (`BudgetSmoothingEngine`): `AllocatedMonthly = (RemainingToDeposit + Overrun) /
  MonthsRemaining`, recomputed every month so no single month gets hit with an unplanned expense burden.
  `MonthsRemaining` is relative to the end of the Gregorian year (December) — calendar-agnostic, not
  Hebrew-calendar-based, per an explicit constitution amendment.
- **Tithe** (`TitheEngine`): flat one-month lookback only —
  `NetTitheDue = max(0, GrossTitheTarget − FixedDonationsThisMonth − PriorMonthSmallCharityTotal)`.
  There's a separate, intentionally-unprotected `StillToDonateAfterFixed` display figure that must never
  be substituted for `NetTitheDue` in the dashboard or any export. The tithe rate is a stored, configurable
  `TitheSetting`, never a hardcoded constant.
- **Budget overrun**: when an annual item's spend exceeds its budget, the excess becomes an internal debt
  added into the remaining months' allocation requirement (not silently absorbed or ignored).
- Full formulas and rationale live in `.specify/memory/constitution.md`; per-feature detail lives in
  `specs/<NNN-feature-name>/{spec,plan,data-model}.md`.

## Spec-Kit workflow

This repo uses the Spec-Kit slash-command workflow (`/speckit-specify`, `/speckit-clarify`,
`/speckit-plan`, `/speckit-tasks`, `/speckit-implement`, `/speckit-analyze`) for new features, one
cohesive capability at a time (see `specs/001-*` through `specs/004-*` for prior examples of the
spec → plan → tasks → implement artifact chain). Any plan touching Principle IV invariants must list the
specific tests that verify the invariant before implementation is considered complete.
