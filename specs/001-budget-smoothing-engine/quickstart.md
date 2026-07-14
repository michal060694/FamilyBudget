# Quickstart: Validating the Annual Budget & Smoothing Engine

## Prerequisites

- .NET 8 SDK installed
- Solution restored: `dotnet restore` from the repo root
- SQLite database created via EF Core migrations: `dotnet ef database update --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Api`

## Seed data (for manual validation)

Create one `AnnualBudgetItem` mapped to December (month 12) with a `TotalAmount` and an
`AmountAlreadySetAside` smaller than the total, and one general item (no `TargetMonth`).
See [data-model.md](./data-model.md) for the exact field shapes.

## Run

```bash
dotnet run --project src/FamilyBudget.Api
```

## Validate User Story 1 (Dashboard)

```bash
curl "http://localhost:5000/api/dashboard?year=2026&month=10"
```

Expected: `allocationLines` includes the December item with `allocatedMonthly` equal to
`(totalAmount - amountAlreadySetAside) / monthsRemaining` (3, per the October→December scenario
in spec.md), and `freeBalance` reconciles `projectedIncome - fixedExpenses - totalRequiredAllocation`
(see [contracts/api.md](./contracts/api.md)).

## Validate User Story 2 (Annual Budget Table)

```bash
curl "http://localhost:5000/api/annual-budget?year=2026"
```

Expected: the December item appears before the general item; `isFullyFunded` is `false` for both
seeded items.

## Validate User Story 3 / edge cases (overrun & shortfall)

Run the Core unit tests, which cover these scenarios without needing the API running:

```bash
dotnet test tests/FamilyBudget.Core.Tests
```

Expected: tests demonstrating (a) an overrun item's excess is spread across remaining months
until the deficit reaches zero by calendar year-end (FR-006), (b) an item whose target month has
passed while still underfunded is treated the same way (FR-007), and (c) a fully-funded item
yields `allocatedMonthly = 0` (edge case).

## Full test suite

```bash
dotnet test
```

All Core and API tests (see [plan.md](./plan.md) Project Structure) must pass before this feature
is considered done.
