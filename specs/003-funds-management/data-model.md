# Phase 1 Data Model: Funds Management & Summary

## Fund

Represents a single investment/savings account (spec.md Key Entities; FR-001–FR-004).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Name` | `string` | Display name, e.g., "מיטב", "IBI". MUST NOT be empty |
| `TotalBalance` | `decimal` | Current recorded total balance. MUST be >= 0 |

**Validation rules**:
- `Name` must not be empty/whitespace.
- `TotalBalance` must be >= 0 (edge case: negative balance rejected).

**Methods**:
- `Rename(name)` — updates `Name` (FR-002).
- `SetTotalBalance(amount)` — overwrites `TotalBalance` to the given value (FR-002); this is an
  overwrite, matching the established pattern from `AnnualBudgetItem.SetAmountUsed` and
  `MonthlyExpenseBudgetItem.SetUsedAmount`.

## FundEarmark

A named purpose-allocation line within a fund (spec.md Key Entities; FR-005–FR-008).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `FundId` | `Guid` | Foreign key to `Fund`. Cascade-deletes when the parent `Fund` is deleted (FR-003) |
| `PurposeLabel` | `string` | e.g., "פאה", "שנתי 26", "חיסכון כללי". MUST NOT be empty |
| `Amount` | `decimal` | MUST be strictly positive (edge case: zero/negative rejected) |

**Methods**:
- `Update(purposeLabel, amount)` — updates both fields together (FR-006).

## FundSummary (response shape, not a stored entity)

The per-fund view assembled by `FundSummaryQueryService` (FR-009–FR-012), computed fresh on every
request from the two tables above — no persisted snapshot (see research.md).

| Field | Formula / Source | Notes |
|---|---|---|
| `FundId` / `Name` / `TotalBalance` | From `Fund` | |
| `Earmarks` | All `FundEarmark` rows where `FundId` matches | Full list, per FR-008 |
| `EarmarkedTotal` | `sum(Earmarks.Amount)` | |
| `Discrepancy` | `TotalBalance - EarmarkedTotal` | `0` = fully reconciled (US3 scenario 1); `> 0` =
  unearmarked remainder still to allocate (US3 scenario 2, edge case "last earmark deleted"); `< 0`
  = over-earmarked mismatch that must be flagged (US3 scenario 3) |

The client is responsible for rendering `Discrepancy` distinctly depending on sign (e.g., neutral
"still to earmark" wording for `> 0`, a flagged/warning treatment for `< 0`, per FR-010/FR-012) —
the API always returns the raw signed figure so the client never needs to re-derive it.
