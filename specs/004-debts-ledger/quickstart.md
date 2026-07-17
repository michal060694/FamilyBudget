# Quickstart: Validating Debts Ledger (Debts & Loans)

## Prerequisites

- .NET 8 SDK installed
- Solution restored: `dotnet restore` from the repo root
- PostgreSQL database migrated (adds the `Debts` table): `dotnet ef database update --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Api`

## Run

```bash
dotnet run --project src/FamilyBudget.Api
```

## Validate User Story 1 (Record & Manage Debts)

```bash
curl -X POST http://localhost:5000/api/debts -H "Content-Type: application/json" -d \
  '{"direction":"Receivable","counterpartyName":"Yossi Cohen","originalAmount":1000,"targetDate":"2026-09-01"}'
curl http://localhost:5000/api/debts
```

Expected: the created debt appears under `receivables`, with `status: "Open"` and
`currentBalance: 1000`.

## Validate User Story 2 (Split Ledger Screen)

Open the "ספר חובות" tab in the client (`http://localhost:5000/`) and verify the receivable and
payable tables render with the correct columns per [contracts/api.md](./contracts/api.md).

## Validate User Story 3 (Repayment + Automatic Transaction)

```bash
curl -X POST http://localhost:5000/api/debts/{debtId}/repayments -H "Content-Type: application/json" -d \
  '{"amount":400,"paymentMethod":"BankTransfer"}'
curl "http://localhost:5000/api/transactions?year=2026&month=7"
curl "http://localhost:5000/api/monthly-overview?year=2026&month=7"
```

Expected: the debt's `currentBalance` drops to `600` (still `Open`); a new `Income` transaction
for `400` (marked `isTitheApplicable: false`) appears in that month's transaction list. Repeat
against a `Payable` debt and verify a `DebtRepayment`-type transaction appears instead, and that
`monthly-overview`'s `debtRepaymentsSummary` includes it.

## Validate over-payment rejection and auto-close

```bash
curl -X POST http://localhost:5000/api/debts/{debtId}/repayments -H "Content-Type: application/json" -d \
  '{"amount":9999999,"paymentMethod":"Cash"}'
```

Expected: `400 Bad Request` — a repayment larger than the current balance is rejected. Recording a
repayment exactly equal to the remaining balance instead should flip `status` to `"Closed"`.

## Run the Core unit tests

```bash
dotnet test tests/FamilyBudget.Core.Tests --filter FullyQualifiedName~Debt
```

Expected: tests proving balance/status transitions, over-payment rejection, and that repayments
generate the correct transaction type/amount/tithe-flag per direction (Constitution Principle IV).

## Full test suite

```bash
dotnet test
```

All Core and API tests (see [plan.md](./plan.md) Project Structure) must pass before this feature
is considered done.
