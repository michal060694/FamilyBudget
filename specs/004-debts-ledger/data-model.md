# Phase 1 Data Model: Debts Ledger (Debts & Loans)

## Debt

A single mutual-debt record, in either direction (spec.md Key Entities; FR-001-FR-009).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Direction` | `DebtDirection` enum (`Receivable`, `Payable`) | `Receivable` = owed to the household; `Payable` = owed by the household |
| `CounterpartyName` | `string` | Person/gemach/institution name. MUST NOT be empty |
| `OriginalAmount` | `decimal` | The debt's original amount. MUST be strictly positive |
| `CurrentBalance` | `decimal` | Starts equal to `OriginalAmount`; decreases only via `RecordRepayment`. MUST be >= 0 |
| `Status` | `DebtStatus` enum (`Open`, `Closed`) | Automatically `Closed` when `CurrentBalance` reaches exactly `0`; automatically `Open` otherwise |
| `TargetDate` | `DateOnly?` | Optional — primarily meaningful for `Receivable` (FR-002), display-only |
| `RepaymentRate` | `decimal?` | Optional — primarily meaningful for `Payable` (FR-002), display-only |
| `Notes` | `string?` | Optional free text |

**Validation rules**:
- `CounterpartyName` must not be empty.
- `OriginalAmount` must be strictly positive.
- `RepaymentRate`, when present, must be > 0.

**Methods**:
- `RecordRepayment(amount)` — throws if `Status == Closed`, if `amount <= 0`, or if
  `amount > CurrentBalance` (FR-006, FR-008, FR-009); otherwise `CurrentBalance -= amount`, and
  `Status` becomes `Closed` if the new balance is exactly `0`.
- `Update(counterpartyName, originalAmount, targetDate, repaymentRate, notes)` — recomputes
  `CurrentBalance` preserving the amount already paid so far (research.md): `CurrentBalance =
  max(0, originalAmount - (OriginalAmount_before_edit - CurrentBalance_before_edit))`, and
  re-evaluates `Status` from the recomputed balance.

## Transaction (extended — feature 002)

Adds one new member to the existing `TransactionType` enum (research.md):

| Value | Meaning |
|---|---|
| `DebtRepayment` | An outflow transaction generated when a `Payable` debt's repayment is recorded (FR-011). Distinguishable from `FixedExpense`/`RegularExpense` so it can be summed separately for the Monthly Overview's debt-repayments figure (FR-012). |

A `Receivable` debt's repayment instead generates a plain `Income` transaction with
`IsTitheApplicable = false` (FR-010) — no new type needed for that direction (research.md).

## MonthlyOverview (extended — feature 002)

`MonthlyOverviewQueryService`'s `DebtRepaymentsSummary` (previously a hardcoded `0m` placeholder)
becomes: the sum of the viewed month's `Transaction` rows where `Type == TransactionType.DebtRepayment`
(FR-012). No response shape change — the field already existed as a placeholder in feature 002's
`MonthlyOverviewResponse`; only its computed value changes from always-zero to real data.

## Debts Ledger Screen (response shape, not a stored entity)

Backs User Story 2 (the split two-table view; FR-007).

| Field | Type | Notes |
|---|---|---|
| `Receivables` | `Debt[]` | All debts where `Direction == Receivable`, each showing `CounterpartyName`, `CurrentBalance`, `TargetDate`, `Notes`, `Status` |
| `Payables` | `Debt[]` | All debts where `Direction == Payable`, each showing `CounterpartyName`, `RepaymentRate`, `CurrentBalance`, `Status` |
