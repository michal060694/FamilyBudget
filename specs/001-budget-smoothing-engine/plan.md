# Implementation Plan: Annual Budget & Smoothing Engine

**Branch**: `001-budget-smoothing-engine` | **Date**: 2026-07-13 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-budget-smoothing-engine/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Deliver the domain engine and two read endpoints that let a user see, at the start of any
calendar month, exactly how much must be set aside toward each annual budget item this month
(`AllocatedMonthly = (TotalAmount - AmountAlreadySetAside) / MonthsRemaining`), and to review the
full annual budget table ordered by calendar month. Implemented as a Core domain library
(calculation engine, entities, calendar-month arithmetic) with an Infrastructure layer persisting
`AnnualBudgetItem` records in SQLite via EF Core, exposed through a thin API layer with no
business logic of its own.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core (Minimal APIs) for the API layer; Entity Framework Core 8
(Microsoft.EntityFrameworkCore.Sqlite) for persistence; no UI framework in this feature's scope.

**Storage**: SQLite, single local file, accessed exclusively through EF Core (Constitution
Principle III).

**Testing**: xUnit for Core domain unit tests (calculation engine, calendar-month arithmetic,
overrun/shortfall logic); `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for API
integration tests against an in-memory SQLite connection.

**Target Platform**: Self-hosted local ASP.NET Core Web API (single-household deployment, no
cloud dependency required for this feature).

**Project Type**: web-service (API backend); a presentation client for the dashboard/table
screens is out of scope for this feature and will consume these endpoints later.

**Performance Goals**: Not a high-throughput system — single household, dashboard and table
endpoints must respond well under 1s with realistic data volumes (see Scale/Scope).

**Constraints**: Fully offline-capable (local SQLite file, no external network calls); monthly
allocation recomputation must be deterministic and side-effect-free when just querying (FR-002,
FR-005).

**Scale/Scope**: Single household/user context; on the order of 10-30 annual budget items per
calendar year (always 12 months); no concurrent-user or multi-tenant concerns.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Proactive Budget Smoothing | Feature's entire purpose is the smoothing engine (FR-002, FR-006, FR-007) | PASS |
| II. Clean Layered Architecture (NON-NEGOTIABLE) | Solution split into `FamilyBudget.Core` (calculation engine, entities — no EF/ASP.NET references), `FamilyBudget.Infrastructure` (EF Core + SQLite), `FamilyBudget.Api` (thin endpoints) | PASS |
| III. Fixed Technology Stack | C#/.NET 8, SQLite via EF Core only, new GitHub repo (already initialized) | PASS |
| IV. Financial Data Integrity (NON-NEGOTIABLE) | Fund-balance invariant itself belongs to the future Asset Allocation feature; this feature only guarantees its own invariant — allocation math always resolves overruns/shortfalls to zero by calendar year-end (FR-006, FR-007, SC-002/SC-003) — covered by dedicated Core unit tests | PASS (scoped) |
| V. Auditability & Transparency | Dashboard endpoint returns income, fixed expenses, allocation, and free balance as distinct fields (FR-003), never a single aggregate | PASS |

No violations identified. Complexity Tracking table below is not needed and has been omitted.

## Project Structure

### Documentation (this feature)

```text
specs/[###-feature]/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md        # Phase 1 output (/speckit-plan command)
├── quickstart.md        # Phase 1 output (/speckit-plan command)
├── contracts/           # Phase 1 output (/speckit-plan command)
└── tasks.md             # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
FamilyBudget.sln

src/
├── FamilyBudget.Core/
│   ├── Entities/            # AnnualBudgetItem, CalendarYearCycle
│   ├── Services/            # BudgetSmoothingEngine (AllocatedMonthly calc, overrun/shortfall)
│   └── Abstractions/        # IAnnualBudgetItemRepository (implemented in Infrastructure)
├── FamilyBudget.Infrastructure/
│   ├── Persistence/         # FamilyBudgetDbContext, EF Core configurations, SQLite migrations
│   └── Repositories/        # EF Core implementation of Core abstractions
└── FamilyBudget.Api/
    ├── Endpoints/           # Minimal API endpoints: GET /dashboard, GET /annual-budget
    └── Contracts/           # Request/response DTOs (mirrors contracts/ artifacts below)

tests/
├── FamilyBudget.Core.Tests/        # unit tests: smoothing formula, calendar month arithmetic,
│                                   # overrun (Story 3) and shortfall (edge case) handling
└── FamilyBudget.Api.Tests/         # integration tests against the two endpoints (WebApplicationFactory)
```

**Structure Decision**: Single-solution, three-project Clean Architecture layout mandated by
Constitution Principle II. `FamilyBudget.Core` has zero package references beyond the BCL — it is
directly unit-testable without a database or HTTP host. `FamilyBudget.Infrastructure` depends on
`FamilyBudget.Core` and owns all EF Core/SQLite concerns. `FamilyBudget.Api` depends on both and
contains no business logic — endpoints only translate HTTP requests into calls against
`FamilyBudget.Core` services. This structure is reused unchanged by every subsequent feature
(002-006); later features add new entities/services within the same three projects rather than
new projects.

## Complexity Tracking

*No constitution violations — this section is not applicable.*
