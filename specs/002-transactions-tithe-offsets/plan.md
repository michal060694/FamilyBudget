# Implementation Plan: Transactions & Tithe (Chomesh) Offsets

**Branch**: `002-transactions-tithe-offsets` | **Date**: 2026-07-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/002-transactions-tithe-offsets/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Deliver a transaction ledger (income/expense entries tagged by type and payment method, with an
explicit tithe-applicable flag on income) and a protected tithe (chomesh) calculation engine that
nets a month's gross tithe target against that month's fixed donations and the not-yet-offset
small-charity remainder carried from the prior month — never bypassable, always floored at zero,
with any leftover deduction capacity carried forward as a credit. Exposes both through a single
Monthly Overview screen (income split into tithe-applicable/non-tithe-applicable, donations +
"remaining to give", fixed/regular expense tables, a debt-repayments placeholder line, and a
bottom outflow/savings summary) and feeds the computed net tithe-due figure into the existing
Monthly Dashboard (feature 001), replacing its external-input placeholder.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core (Minimal APIs) for the API layer; Entity Framework Core 8
(Npgsql.EntityFrameworkCore.PostgreSQL) for persistence; the existing single-page vanilla
HTML/CSS/JS client (`src/FamilyBudget.Api/wwwroot/index.html`) is extended with a new Monthly
Overview view — no JS framework is introduced.

**Storage**: PostgreSQL, accessed exclusively through EF Core (Constitution Principle III). Adds
two new tables (`Transactions`, `TitheSettings`) alongside feature 001's existing tables in the
same database/DbContext.

**Testing**: xUnit for Core domain unit tests (tithe engine math, small-charity offset
carry-forward, credit carry-forward, income tithe-applicable split); `Microsoft.AspNetCore.Mvc.Testing`
(`WebApplicationFactory`) for API integration tests against an in-memory SQLite connection used as
a test double, consistent with feature 001.

**Target Platform**: Self-hosted local ASP.NET Core Web API (single-household deployment, no
cloud dependency required for this feature) — same host as feature 001.

**Project Type**: web-service (API backend) + the existing single-page HTML client.

**Performance Goals**: Not a high-throughput system — single household; monthly overview and
transaction-list endpoints must respond well under 1s with realistic data volumes (see
Scale/Scope).

**Constraints**: Persistence requires a reachable PostgreSQL instance (no other external network
calls); the tithe calculation must be deterministic and side-effect-free when queried, and must
recompute correctly on demand whenever a transaction is created, edited, or deleted (FR-003).

**Scale/Scope**: Single household/user context; on the order of tens to low hundreds of
transactions per month, evaluated across however many months the household has been using the
system (the carry-forward walk described in research.md scales linearly with month count, which
stays small in practice).

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Proactive Budget Smoothing | Not this feature's core purpose, but it supplies the real income/expense inputs the smoothing dashboard previously assumed as external placeholders (FR-013) — no conflict | PASS (scoped) |
| II. Clean Layered Architecture (NON-NEGOTIABLE) | New `Transaction`/`TitheSetting` entities and the `TitheEngine` calculation service live in `FamilyBudget.Core` with zero EF/ASP.NET references; `FamilyBudget.Infrastructure` owns persistence; `FamilyBudget.Api` stays a thin translation layer | PASS |
| III. Fixed Technology Stack | C#/.NET 8, PostgreSQL via EF Core only, same solution/repo as feature 001 | PASS |
| IV. Financial Data Integrity (NON-NEGOTIABLE) | This feature's entire purpose is the protected tithe calculation (FR-006, FR-008, FR-009, FR-011) with a stored, user-configurable rate (FR-006) — never hardcoded — covered by dedicated Core unit tests before being considered done | PASS |
| V. Auditability & Transparency | Monthly overview screen shows income split, donations vs. remaining-to-give, expense tables, and outflow/savings summary as separate traceable sections (FR-012, FR-014–FR-018), never a single opaque total | PASS |

No violations identified. Complexity Tracking table below is not needed and has been omitted.

## Project Structure

### Documentation (this feature)

```text
specs/002-transactions-tithe-offsets/
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
│   ├── Entities/
│   │   ├── AnnualBudgetItem.cs      # (feature 001, unchanged)
│   │   ├── AnnualReserve.cs         # (feature 001, unchanged)
│   │   ├── Transaction.cs           # NEW: date, amount, type, payment method, tithe-applicable flag
│   │   └── TitheSetting.cs          # NEW: single-row stored tithe rate
│   ├── Services/
│   │   ├── BudgetSmoothingEngine.cs # (feature 001, unchanged)
│   │   ├── CalendarYearCycle.cs     # (feature 001, unchanged)
│   │   ├── AnnualBudgetQueryService.cs # (feature 001, unchanged)
│   │   ├── TitheEngine.cs           # NEW: gross target, deductions, carry-forward, net due
│   │   └── MonthlyOverviewQueryService.cs # NEW: assembles the monthly overview screen shape
│   └── Abstractions/
│       ├── ITransactionRepository.cs   # NEW
│       └── ITitheSettingRepository.cs  # NEW
├── FamilyBudget.Infrastructure/
│   ├── Persistence/         # FamilyBudgetDbContext gains Transactions/TitheSettings DbSets +
│   │                        # EF Core configurations + a new migration
│   └── Repositories/
│       ├── TransactionRepository.cs    # NEW
│       └── TitheSettingRepository.cs   # NEW
└── FamilyBudget.Api/
    ├── Endpoints/
    │   ├── DashboardEndpoints.cs    # (feature 001) EXTENDED: dashboard now includes net tithe due
    │   ├── AnnualBudgetEndpoints.cs # (feature 001, unchanged)
    │   ├── TransactionEndpoints.cs  # NEW: CRUD + filtered list
    │   └── MonthlyOverviewEndpoints.cs # NEW: GET /api/monthly-overview, tithe-setting endpoints
    └── Contracts/
        ├── DashboardContracts.cs    # (feature 001) EXTENDED: + titheDue breakdown fields
        ├── AnnualBudgetContracts.cs # (feature 001, unchanged)
        ├── TransactionContracts.cs  # NEW
        └── MonthlyOverviewContracts.cs # NEW

src/FamilyBudget.Api/wwwroot/
└── index.html                # EXTENDED: new "Monthly Overview" view/tab alongside the existing
                               # annual-budget view, reusing its RTL/CSS conventions

tests/
├── FamilyBudget.Core.Tests/
│   ├── TitheEngineTests.cs             # NEW: gross target, protected deductions, zero floor
│   ├── SmallCharityOffsetCarryForwardTests.cs # NEW: multi-month carry-forward walk (FR-010, US3)
│   ├── TitheCreditCarryForwardTests.cs # NEW: excess-deduction credit carry (FR-009 edge case)
│   └── TransactionValidationTests.cs   # NEW: type/payment-method required, income tithe flag required
└── FamilyBudget.Api.Tests/
    ├── TransactionEndpointsTests.cs        # NEW
    ├── MonthlyOverviewEndpointsTests.cs    # NEW
    └── DashboardEndpointTests.cs           # (feature 001) EXTENDED: asserts new titheDue field
```

**Structure Decision**: Reuses the existing three-project Clean Architecture solution from
feature 001 unchanged — no new projects. `FamilyBudget.Core` gains the `Transaction`/
`TitheSetting` entities and the `TitheEngine`/`MonthlyOverviewQueryService` services with zero new
package references, keeping the tithe math independently unit-testable without a database or HTTP
host. `FamilyBudget.Infrastructure` adds the two new tables to the same `FamilyBudgetDbContext`
and ships one additive EF Core migration. `FamilyBudget.Api` adds new endpoint/contract files and
extends the two existing feature-001 dashboard files to surface the new tithe-due figure,
containing no business logic of its own. The existing single-file HTML client
(`wwwroot/index.html`) is extended in place rather than introducing a second client project,
consistent with feature 001's approach.

## Complexity Tracking

*No constitution violations — this section is not applicable.*
