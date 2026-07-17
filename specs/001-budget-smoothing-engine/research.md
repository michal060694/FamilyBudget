# Phase 0 Research: Annual Budget & Smoothing Engine

All Technical Context fields were resolved with informed defaults during planning (no
`NEEDS CLARIFICATION` markers remained). This document records the reasoning behind the
non-obvious choices.

## Decision: Gregorian calendar arithmetic, plain `int` months 1-12 (superseded from Hebrew)

**Rationale**: Per explicit user direction (2026-07-14), the feature was switched from
Hebrew-calendar-year cycles to Gregorian-calendar-year cycles. A Gregorian year always has
exactly 12 months — unlike the Hebrew calendar, there is no leap-month variability (a Gregorian
leap year only adds a day to February; it never changes the month count or ordering). This
removes the need for `System.Globalization.HebrewCalendar` (or any calendar library) entirely:
`MonthsRemaining` (FR-002) is now plain integer arithmetic against a fixed 12-month year, computed
relative to December.

**Alternatives considered** (from the original Hebrew-calendar version of this feature, kept for
history):
- Hand-rolled Hebrew month tables — moot now that the feature targets the Gregorian calendar.
- A third-party NuGet Hebrew-calendar package — moot for the same reason.
- Keeping `System.Globalization.HebrewCalendar` — rejected: no longer needed once the year cycle
  is Gregorian; removing it also removes a dependency and simplifies `MonthsRemaining` to
  branch-free integer math.

## Decision: ASP.NET Core Minimal APIs for the API layer

**Rationale**: This feature only exposes two read endpoints (dashboard data, annual budget
table). Minimal APIs keep the `FamilyBudget.Api` project a thin translation layer with no
framework ceremony, directly satisfying Constitution Principle II (API contains no business
logic — it only calls into `FamilyBudget.Core`).

**Alternatives considered**:
- MVC Controllers — rejected for now: no material benefit at this scope (two endpoints); would
  add boilerplate. Nothing prevents switching later if the API surface grows substantially.

## Decision: Allocation is computed on-demand, not stored as mutable state

**Rationale**: `AllocatedMonthly` (FR-002) and the overrun/shortfall handling (FR-006, FR-007)
are derived purely from `TotalAmount`, `AmountAlreadySetAside`, and the current calendar month.
Treating this as a pure calculation over stored facts (rather than persisting a separately
mutable "monthly allocation" or "internal debt" record) avoids a second source of truth that
could drift from the underlying `AnnualBudgetItem` data — directly supporting Constitution
Principle V (every number must be traceable to its components).

**Alternatives considered**:
- Persisting a mutable per-month allocation snapshot updated by a background job — rejected:
  introduces a second source of truth and a synchronization/staleness risk; the calculation is
  cheap enough (≤30 items) to compute on every request.

## Decision: Tests run against real SQLite (not the EF Core InMemory provider), as a fast in-memory stand-in for production PostgreSQL

**Rationale**: Constitution Principle III fixes PostgreSQL as the only supported production store,
but requiring a live PostgreSQL instance for every test run would be slow and add CI/local setup
friction. SQLite is used instead as an in-memory test double: the EF Core InMemory provider does
not enforce the same constraints (e.g., column types, SQL translation) that a real relational
provider does and could hide bugs that only surface against real SQL. A SQLite connection with
`DataSource=:memory:` kept open for the test's lifetime is fast and still exercises a real
provider's SQL translation. This works in practice because SQLite's loose type affinity accepts
the Postgres-flavored column types EF Core migrations generate (e.g. `uuid`, `numeric(18,2)`)
without validation, so the same migration files apply cleanly to both databases.

**Alternatives considered**:
- Running integration tests against a real PostgreSQL instance — rejected as the default: much
  slower, and requires a running Postgres server (Docker or hosted) for every local test run and
  in CI.
- EF Core InMemory provider — rejected: diverges further from the production database engine's
  SQL translation, risking false-positive test passes.
- A file-based SQLite test database — rejected as the default: slower and requires cleanup;
  reserved for scenarios that specifically need file-persistence behavior.

## Decision: xUnit as the test framework

**Rationale**: xUnit is the de facto standard for .NET 8 projects, has first-class
`Microsoft.AspNetCore.Mvc.Testing` integration for API integration tests, and requires no extra
justification given no project-specific constraint favors an alternative.

**Alternatives considered**:
- NUnit / MSTest — rejected: no advantage for this project; xUnit is the more common default in
  new .NET projects and keeps tooling/documentation simplest.
