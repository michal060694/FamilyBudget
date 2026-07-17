# Implementation Plan: Funds Management & Summary

**Branch**: `003-funds-management` | **Date**: 2026-07-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/003-funds-management/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Deliver a `Fund`/`FundEarmark` domain model and CRUD endpoints that let the user record each
investment/savings fund's name and current total balance, break that balance down into named
earmarked purposes, and see — per fund — whether the earmarked sum reconciles exactly with the
recorded balance (Constitution Principle IV, Fund balance integrity), with any mismatch clearly
surfaced rather than silently allowed to drift. Exposed as a new "Funds" tab in the existing
single-page client, alongside the Annual Budget and Monthly Overview tabs.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core (Minimal APIs) for the API layer; Entity Framework Core 8
(Npgsql.EntityFrameworkCore.PostgreSQL) for persistence; the existing single-page vanilla
HTML/CSS/JS client (`src/FamilyBudget.Api/wwwroot/index.html`) gains a third tab — no JS framework
is introduced.

**Storage**: PostgreSQL, accessed exclusively through EF Core (Constitution Principle III). Adds
two new tables (`Funds`, `FundEarmarks`) to the same database/DbContext used by features 001 and
002.

**Testing**: xUnit for Core domain unit tests (the balance-vs-earmark reconciliation calculation);
`Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for API integration tests against an
in-memory SQLite connection used as a test double, consistent with features 001/002.

**Target Platform**: Self-hosted local ASP.NET Core Web API (single-household deployment) — same
host as features 001/002.

**Project Type**: web-service (API backend) + the existing single-page HTML client.

**Performance Goals**: Not a high-throughput system — single household; a handful of funds, each
with a handful of earmark lines. Screen must respond well under 1s.

**Constraints**: Persistence requires a reachable PostgreSQL instance (no external network/
brokerage feed); the reconciliation figure (balance minus earmarked sum) must recompute
deterministically on every read, with no cached/stale state.

**Scale/Scope**: Single household/user context; on the order of 2-10 funds, each with a handful of
earmark lines; no concurrent-user or multi-tenant concerns.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Proactive Budget Smoothing | Not this feature's purpose; no conflict — funds are a standalone balance/earmark view, explicitly not auto-reconciled against the smoothing engine (spec.md Assumptions) | PASS (scoped) |
| II. Clean Layered Architecture (NON-NEGOTIABLE) | `Fund`/`FundEarmark` entities and `FundSummaryQueryService` live in `FamilyBudget.Core` with zero EF/ASP.NET references; `FamilyBudget.Infrastructure` owns persistence; `FamilyBudget.Api` stays a thin translation layer | PASS |
| III. Fixed Technology Stack | C#/.NET 8, PostgreSQL via EF Core only, same solution/repo as features 001/002 | PASS |
| IV. Financial Data Integrity (NON-NEGOTIABLE) | This feature's entire purpose is the fund-balance invariant itself (FR-009–FR-012): earmarked sum vs. recorded balance, discrepancy always visible, never silently drifting — covered by dedicated Core unit tests before being considered done | PASS |
| V. Auditability & Transparency | Each fund's balance, full earmark list, and the computed discrepancy are always shown as separate, traceable figures — never a single opaque total | PASS |

No violations identified. Complexity Tracking table below is not needed and has been omitted.

## Project Structure

### Documentation (this feature)

```text
specs/003-funds-management/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
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
│   │   ├── Fund.cs                  # NEW: name, total balance
│   │   └── FundEarmark.cs           # NEW: fund id, purpose label, amount
│   ├── Services/
│   │   └── FundSummaryQueryService.cs # NEW: per-fund earmarked sum + discrepancy
│   └── Abstractions/
│       ├── IFundRepository.cs         # NEW
│       └── IFundEarmarkRepository.cs  # NEW
├── FamilyBudget.Infrastructure/
│   ├── Persistence/         # FamilyBudgetDbContext gains Funds/FundEarmarks DbSets +
│   │                        # EF Core configurations (cascade delete Fund → FundEarmarks) +
│   │                        # a new migration
│   └── Repositories/
│       ├── FundRepository.cs         # NEW
│       └── FundEarmarkRepository.cs  # NEW
└── FamilyBudget.Api/
    ├── Endpoints/
    │   └── FundEndpoints.cs   # NEW: GET/POST/PUT/DELETE /api/funds, earmark sub-routes
    └── Contracts/
        └── FundContracts.cs   # NEW

src/FamilyBudget.Api/wwwroot/
└── index.html                # EXTENDED: new "קרנות" (Funds) tab alongside the existing
                               # "תקציב שנתי" and "סקירה חודשית" tabs

tests/
├── FamilyBudget.Core.Tests/
│   ├── FundTests.cs                    # NEW: validation (balance >= 0, name required)
│   ├── FundEarmarkTests.cs             # NEW: validation (amount > 0)
│   └── FundSummaryQueryServiceTests.cs # NEW: earmarked-sum vs. balance discrepancy math
└── FamilyBudget.Api.Tests/
    └── FundEndpointsTests.cs           # NEW: CRUD for funds + earmarks, discrepancy reconciliation
```

**Structure Decision**: Reuses the existing three-project Clean Architecture solution unchanged —
no new projects. `Fund` and `FundEarmark` follow the same flat top-level entity pattern already
established by `AnnualBudgetItem`, `Transaction`, `MonthlyExpenseBudgetItem`, and
`FixedDonationStandingOrder` in this codebase (no aggregate-root nesting). `FamilyBudget.Core`
gains the entities and `FundSummaryQueryService` with zero new package references, keeping the
reconciliation math independently unit-testable without a database or HTTP host.
`FamilyBudget.Infrastructure` adds the two new tables to the same `FamilyBudgetDbContext` and ships
one additive EF Core migration, configuring cascade delete so removing a fund removes its earmark
lines. `FamilyBudget.Api` adds new endpoint/contract files, containing no business logic of its
own. The existing single-file HTML client is extended in place with a third tab.

## Complexity Tracking

*No constitution violations — this section is not applicable.*
