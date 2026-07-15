# Quickstart: Validating Transactions & Tithe (Chomesh) Offsets

## Prerequisites

- .NET 8 SDK installed
- Solution restored: `dotnet restore` from the repo root
- SQLite database migrated (adds `Transactions` and `TitheSettings` tables to the existing
  feature-001 database): `dotnet ef database update --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Api`

## Set the tithe rate

```bash
curl -X PUT http://localhost:5000/api/tithe-setting -H "Content-Type: application/json" -d '{"rate": 0.2}'
```

## Run

```bash
dotnet run --project src/FamilyBudget.Api
```

## Validate User Story 1 (Record Transactions)

```bash
curl -X POST http://localhost:5000/api/transactions -H "Content-Type: application/json" -d \
  '{"date":"2026-07-15","amount":500,"type":"Income","paymentMethod":"BankTransfer","isTitheApplicable":true,"description":"Salary"}'

curl "http://localhost:5000/api/transactions?year=2026&month=7"
```

Expected: the transaction list includes the recorded salary with its type, payment method, and
tithe-applicable flag; a `POST` with `type: "Income"` and no `isTitheApplicable` field returns
`400` (FR-001a).

## Validate User Story 2 (Monthly Overview Screen)

Record a fixed donation and a small-charity expense in the prior month, then a tithe-applicable
income and a fixed donation in the current month:

```bash
curl "http://localhost:5000/api/monthly-overview?year=2026&month=7"
```

Expected (per [contracts/api.md](./contracts/api.md) and [data-model.md](./data-model.md)):
`titheApplicableIncome.subtotal` and `nonTitheApplicableIncome.subtotal` are reported separately;
`donations.remainingToGive` equals `titheObligation.netTitheDue - donations.givenThisMonth`,
floored at zero; `debtRepaymentsSummary` is `0`; `remainingToSave` reconciles `totalIncome -
totalOutflow`.

## Validate User Story 3 / carry-forward edge cases

Run the Core unit tests, which cover the multi-month forward-walk algorithm without needing the
API running:

```bash
dotnet test tests/FamilyBudget.Core.Tests
```

Expected: tests demonstrating (a) a prior month's unoffset small-charity remainder reduces the
current month's tithe target (FR-008b), (b) deductions exceeding the gross target floor the net
due at zero and carry the excess forward as credit (FR-009), and (c) small-charity amounts are
never lost or double-counted across a sequence of months (SC-003).

## Validate dashboard integration (FR-013)

```bash
curl "http://localhost:5000/api/dashboard?year=2026&month=7"
```

Expected: the response now includes a `titheDue` object alongside the existing
income/expense/allocation/free-balance fields, matching the same `netTitheDue` value returned by
`/api/monthly-overview` for the same month.

## Full test suite

```bash
dotnet test
```

All Core and API tests (see [plan.md](./plan.md) Project Structure) must pass before this feature
is considered done.
