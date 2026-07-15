# Phase 1 Data Model: Transactions & Tithe (Chomesh) Offsets

## Transaction

Represents a single recorded income or expense event (spec.md Key Entities).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Date` | `DateOnly` | The calendar date of the transaction; its year/month determine which month it belongs to for all calculations |
| `Amount` | `decimal` | MUST be > 0 |
| `Type` | `TransactionType` enum | `Income`, `FixedExpense`, `RegularExpense`, `FixedDonation`, `SmallCharityExpense` (FR-001) |
| `PaymentMethod` | `PaymentMethod` enum | `CreditCard`, `BankTransfer`, `Cash`, `Check` (FR-001) |
| `IsTitheApplicable` | `bool?` | Required (non-null) when `Type == Income`; MUST be `null` for every other type (FR-001a) |
| `Description` | `string?` | Optional free-text note |

**Validation rules**:
- `Amount` must be strictly positive.
- `Type` and `PaymentMethod` are both required — a transaction cannot be saved without both
  (FR-002).
- `IsTitheApplicable` MUST be non-null when `Type == Income` (FR-001a) and MUST be null for all
  other types — enforced as a Core-layer invariant, not left to the API/client.
- Editing or deleting a `Transaction` MUST cause every figure derived from it (monthly income
  totals, tithe obligation, offset carry-forward) to be recomputed on next read — there is no
  cached/stale derived state to invalidate, since nothing derived is stored (FR-003; see
  research.md).

## TitheSetting

The user-configurable tithe rate applied to tithe-applicable income each month (spec.md Key
Entities; FR-006). Follows the same single-row upsert pattern as feature 001's `AnnualReserve`.

| Field | Type | Notes |
|---|---|---|
| `Id` | `int` (fixed value, e.g. `1`) | Single-row table — one household-wide rate |
| `Rate` | `decimal` | e.g. `0.1` for maaser or `0.2` for chomesh. MUST be > 0 and <= 1. Defaults to `0.2` when no row exists yet (matching this feature's chomesh framing), returned by `ITitheSettingRepository.GetRateAsync` |

## MonthlyTitheObligation (response shape, not a stored entity)

The computed result for a given calendar month (spec.md Key Entities), produced by
`TitheEngine.ComputeMonth(year, month)` per the forward-walk algorithm in research.md.

| Field | Type | Notes |
|---|---|---|
| `Year` | `int` | |
| `Month` | `int` | 1-12 |
| `TitheApplicableIncome` | `decimal` | Sum of this month's Income transactions flagged tithe-applicable (FR-005) |
| `NonTitheApplicableIncome` | `decimal` | Sum of this month's Income transactions flagged not tithe-applicable (FR-005) — does not feed the tithe target, but does feed the bottom-line savings summary |
| `TitheRate` | `decimal` | From `TitheSetting`, as of calculation time |
| `GrossTitheTarget` | `decimal` | `TitheApplicableIncome × TitheRate` (FR-007) |
| `FixedDonationsThisMonth` | `decimal` | Sum of this month's Fixed Donation transactions (FR-008a) |
| `CreditCarriedIn` | `decimal` | Generic excess-deduction credit carried in from the prior month (FR-009) |
| `SmallCharityAppliedThisMonth` | `decimal` | Portion of the prior month's not-yet-offset small-charity remainder applied against this month's target (FR-008b, FR-010) |
| `NetTitheDue` | `decimal` | `max(0, GrossTitheTarget - FixedDonationsThisMonth - CreditCarriedIn - SmallCharityAppliedThisMonth)` (FR-009) |
| `CreditCarriedOut` | `decimal` | Excess deductions beyond the gross target this month, carried to next month's `CreditCarriedIn` (FR-009) |

## SmallCharityOffsetLedger (response shape, not a stored entity)

Per calendar month (spec.md Key Entities; FR-010; User Story 3), tracks the small-charity
offset mechanism specifically, distinct from the generic credit above.

| Field | Type | Notes |
|---|---|---|
| `Year` | `int` | |
| `Month` | `int` | 1-12 |
| `SmallCharityExpenseTotal` | `decimal` | Sum of this month's *new* Small Charity Expense transactions — NOT eligible to offset this same month's tithe (edge case), only eligible starting next month |
| `AvailableFromPriorMonth` | `decimal` | The not-yet-offset remainder carried in from the prior month, eligible to offset this month's tithe |
| `AppliedThisMonth` | `decimal` | How much of `AvailableFromPriorMonth` was actually used as an offset against this month's `MonthlyTitheObligation` (equals `SmallCharityAppliedThisMonth` above) |
| `UnappliedRemainder` | `decimal` | `(AvailableFromPriorMonth - AppliedThisMonth) + SmallCharityExpenseTotal` — carried forward to become next month's `AvailableFromPriorMonth` |

## Monthly Overview (response shape, not a stored entity)

Backs the Monthly Overview screen (User Story 2; FR-014–FR-018).

| Field | Type | Notes |
|---|---|---|
| `Year` / `Month` | `int` | |
| `TitheApplicableIncomeLines` | `Transaction[]` | This month's Income transactions where `IsTitheApplicable == true`, plus their subtotal (FR-014) |
| `NonTitheApplicableIncomeLines` | `Transaction[]` | This month's Income transactions where `IsTitheApplicable == false`, plus their subtotal (FR-014) |
| `DonationLines` | `Transaction[]` | This month's Fixed Donation (and, for full visibility, Small Charity Expense) transactions, plus their subtotal | 
| `RemainingToGive` | `decimal` | `NetTitheDue` (from `MonthlyTitheObligation`) minus total donations already given this month, floored at zero (FR-015) |
| `FixedExpenseLines` | `Transaction[]` | This month's Fixed Expense transactions, plus subtotal (FR-016) |
| `RegularExpenseLines` | `Transaction[]` | This month's Regular Expense transactions, plus subtotal (FR-016) |
| `DebtRepaymentsSummary` | `decimal` | Single aggregate placeholder; hardcoded to `0` until the future Debts Ledger feature supplies real data (FR-017) |
| `TotalOutflow` | `decimal` | Donations given + fixed expenses + regular expenses + debt repayments (FR-018) |
| `TotalIncome` | `decimal` | `TitheApplicableIncome + NonTitheApplicableIncome` |
| `RemainingToSave` | `decimal` | `TotalIncome - TotalOutflow` (FR-018) |

## Dashboard response shape extension (FR-013)

Feature 001's `GET /api/dashboard` response gains:

| Field | Type | Notes |
|---|---|---|
| `TitheDue` | `decimal` | `MonthlyTitheObligation.NetTitheDue` for the requested year/month |
| `TitheGrossTarget` | `decimal` | For traceability (Constitution Principle V / FR-012) |
| `TitheFixedDonationsDeduction` | `decimal` | = `FixedDonationsThisMonth + CreditCarriedIn` combined, or kept as two separate fields — see contracts/api.md for the exact shape |
| `TitheSmallCharityDeduction` | `decimal` | = `SmallCharityAppliedThisMonth` |

This replaces the previous assumption (feature 001 spec.md Assumptions) that projected
income/fixed expenses are external inputs unrelated to a tithe figure — the dashboard's existing
`ProjectedIncome`/`FixedExpenses` fields are unchanged in meaning, but a new tithe line is added
alongside them.
