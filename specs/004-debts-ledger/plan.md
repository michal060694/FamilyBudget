# Implementation Plan: Debts Ledger (Debts & Loans)

**Branch**: `004-debts-ledger` | **Date**: 2026-07-15 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/004-debts-ledger/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Deliver a `Debt` domain model (bidirectional receivable/payable, current balance, status) with
CRUD endpoints, a repayment operation that reduces the balance, auto-closes the debt at zero, and
automatically generates the matching transaction in feature 002's transaction ledger (an Income
transaction for a receivable collected, a distinguishable outflow transaction for a payable paid),
and updates feature 002's Monthly Overview screen to source its previously-hardcoded
debt-repayments placeholder from the real sum of this month's payable-repayment transactions.
Exposed as a new "ספר חובות" tab in the existing single-page client.

## Technical Context

**Language/Version**: C# 12 / .NET 8

**Primary Dependencies**: ASP.NET Core (Minimal APIs) for the API layer; Entity Framework Core 8
(Microsoft.EntityFrameworkCore.Sqlite) for persistence; the existing single-page vanilla
HTML/CSS/JS client (`src/FamilyBudget.Api/wwwroot/index.html`) gains a fourth tab — no JS framework
is introduced.

**Storage**: SQLite, single local file, accessed exclusively through EF Core (Constitution
Principle III). Adds one new table (`Debts`) to the same database/DbContext used by features
001-003; no new table is needed for repayments (each repayment's history lives in the existing
`Transactions` table via the transaction it generates).

**Testing**: xUnit for Core domain unit tests (`Debt` balance/status transitions, rejection of
over-payment); `Microsoft.AspNetCore.Mvc.Testing` (`WebApplicationFactory`) for API integration
tests against a real SQLite connection, consistent with features 001-003.

**Target Platform**: Self-hosted local ASP.NET Core Web API (single-household deployment) — same
host as features 001-003.

**Project Type**: web-service (API backend) + the existing single-page HTML client.

**Performance Goals**: Not a high-throughput system — single household; a handful of debts. Screen
must respond well under 1s.

**Constraints**: Fully offline-capable (local SQLite file); recording a repayment must atomically
update the debt and create the transaction — both MUST be reflected together, never one without
the other.

**Scale/Scope**: Single household/user context; on the order of a handful of open debts at any
time; no concurrent-user or multi-tenant concerns.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Check | Result |
|---|---|---|
| I. Proactive Budget Smoothing | Not this feature's purpose; no conflict | PASS (scoped) |
| II. Clean Layered Architecture (NON-NEGOTIABLE) | `Debt` entity and the repayment operation live in `FamilyBudget.Core` with zero EF/ASP.NET references; `FamilyBudget.Infrastructure` owns persistence; `FamilyBudget.Api` stays a thin translation layer | PASS |
| III. Fixed Technology Stack | C#/.NET 8, SQLite via EF Core only, same solution/repo as features 001-003 | PASS |
| IV. Financial Data Integrity (NON-NEGOTIABLE) | This feature's core invariant is its own: a debt's balance MUST NEVER go negative and its generated transaction MUST always reflect exactly the amount actually paid (FR-008, FR-009) — covered by dedicated Core unit tests. It also directly feeds a Principle-V-relevant figure into feature 002 (the Monthly Overview's debt-repayments line, FR-012), which itself remains a traceable, separately-shown figure, not folded into an opaque total | PASS |
| V. Auditability & Transparency | Each debt's original amount, current balance, and status are always shown as separate, traceable figures; every repayment is traceable to the specific transaction it generated in feature 002's ledger | PASS |

No violations identified. Complexity Tracking table below is not needed and has been omitted.

## Project Structure

### Documentation (this feature)

```text
specs/004-debts-ledger/
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
│   │   ├── Debt.cs               # NEW: direction, counterparty, original/current amounts, status
│   │   └── Transaction.cs        # EXTENDED: adds TransactionType.DebtRepayment (FR-011)
│   ├── Services/
│   │   ├── DebtRepaymentService.cs   # NEW: records a repayment, updates Debt, creates the
│   │   │                             # matching Transaction (FR-010, FR-011) — the one place
│   │   │                             # that must keep both writes consistent
│   │   └── MonthlyOverviewQueryService.cs # EXTENDED: DebtRepaymentsSummary sourced from real
│   │                                       # DebtRepayment-type transactions (FR-012)
│   └── Abstractions/
│       └── IDebtRepository.cs        # NEW
├── FamilyBudget.Infrastructure/
│   ├── Persistence/         # FamilyBudgetDbContext gains a Debts DbSet + EF Core configuration
│   │                        # (enum-to-string conversions for Direction/Status) + a new migration
│   └── Repositories/
│       └── DebtRepository.cs         # NEW
└── FamilyBudget.Api/
    ├── Endpoints/
    │   └── DebtEndpoints.cs   # NEW: GET/POST/PUT/DELETE /api/debts, POST /api/debts/{id}/repayments
    └── Contracts/
        └── DebtContracts.cs   # NEW

src/FamilyBudget.Api/wwwroot/
└── index.html                # EXTENDED: new "ספר חובות" (Debts Ledger) tab — two tables
                               # (receivables / payables), add/edit/delete, "record repayment"

tests/
├── FamilyBudget.Core.Tests/
│   ├── DebtTests.cs                    # NEW: validation, balance/status transitions, over-payment rejection
│   └── DebtRepaymentServiceTests.cs    # NEW: repayment creates the correct transaction type/amount/tithe-flag
└── FamilyBudget.Api.Tests/
    ├── DebtEndpointsTests.cs           # NEW: CRUD + repayment flow, over-payment rejection
    └── MonthlyOverviewEndpointsTests.cs # EXTENDED: debt-repayments summary reflects real payable repayments
```

**Structure Decision**: Reuses the existing three-project Clean Architecture solution unchanged —
no new projects. `Debt` follows the same flat top-level entity pattern already established by
every other entity in this codebase. The one piece of real business logic — recording a repayment
must atomically update the `Debt` and create the matching `Transaction` — is centralized in a new
`DebtRepaymentService` in `FamilyBudget.Core`, keeping that consistency rule independently
unit-testable without a database or HTTP host, and preventing the API layer from ever performing
one write without the other. `FamilyBudget.Infrastructure` adds one new table and one additive
migration. `FamilyBudget.Api` adds new endpoint/contract files and contains no business logic of
its own. The existing single-file HTML client is extended in place with a fourth tab.

## Complexity Tracking

*No constitution violations — this section is not applicable.*
