# Quickstart: Validating Funds Management & Summary

## Prerequisites

- .NET 8 SDK installed
- Solution restored: `dotnet restore` from the repo root
- PostgreSQL database migrated (adds `Funds` and `FundEarmarks` tables to the existing database):
  `dotnet ef database update --project src/FamilyBudget.Infrastructure --startup-project src/FamilyBudget.Api`

## Run

```bash
dotnet run --project src/FamilyBudget.Api
```

## Validate User Story 1 (Define Funds)

```bash
curl -X POST http://localhost:5000/api/funds -H "Content-Type: application/json" -d '{"name":"Meitav","totalBalance":15000}'
curl http://localhost:5000/api/funds
```

Expected: the created fund appears with `totalBalance: 15000`, `earmarkedTotal: 0`, and
`discrepancy: 15000` (fully unearmarked, per the edge case in spec.md).

## Validate User Story 2 (View Fund Summary Screen)

Open the "קרנות" (Funds) tab in the client (`http://localhost:5000/`) and verify each fund appears
as its own card/table with its name and balance, per [contracts/api.md](./contracts/api.md).

## Validate User Story 3 (Earmarks & Reconciliation)

```bash
curl -X POST http://localhost:5000/api/funds/{fundId}/earmarks -H "Content-Type: application/json" -d '{"purposeLabel":"פאה","amount":10000}'
curl -X POST http://localhost:5000/api/funds/{fundId}/earmarks -H "Content-Type: application/json" -d '{"purposeLabel":"שנתי 26","amount":5000}'
curl http://localhost:5000/api/funds
```

Expected: `earmarkedTotal: 15000`, `discrepancy: 0` — fully reconciled. Adding a third earmark line
should push `discrepancy` negative, flagged as an over-earmarked mismatch per FR-010.

## Run the Core unit tests

```bash
dotnet test tests/FamilyBudget.Core.Tests --filter FullyQualifiedName~Fund
```

Expected: tests proving the discrepancy calculation for the reconciled, under-earmarked, and
over-earmarked cases (Constitution Principle IV).

## Full test suite

```bash
dotnet test
```

All Core and API tests (see [plan.md](./plan.md) Project Structure) must pass before this feature
is considered done.
