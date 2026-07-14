# Phase 1 Data Model: Annual Budget & Smoothing Engine

**Updated 2026-07-14**: switched from Hebrew-calendar to Gregorian-calendar year cycles (see
spec.md's Updated note and research.md's superseded decision).

**Updated 2026-07-14 (later same day)**: added `AnnualReserve` and a flat year-level summary
(`AnnualBudgetSummary`) per explicit user direction for the client screen redesign — see the new
sections below. This also added the feature's first write endpoints (`POST
/api/annual-budget-items`, `PUT /api/reserve`); the client's table month labels use Hebrew month
names (Tishrei..Elul) as a display convention only — the underlying `TargetMonth` remains a plain
Gregorian-style 1-12 integer with no real Hebrew-calendar computation (see research.md).

## AnnualBudgetItem

Represents a single planned annual expense for a calendar year (spec.md Key Entities).

| Field | Type | Notes |
|---|---|---|
| `Id` | `Guid` | Primary key |
| `Year` | `int` | The Gregorian year this item belongs to (e.g., 2026) |
| `Name` | `string` | Display name, e.g., "December Holidays" |
| `TotalAmount` | `decimal` | Annual target amount. MUST be > 0 |
| `TargetMonth` | `int?` | 1-12 (Gregorian month, January=1); `null` means "general/month-independent" (FR-001) |
| `AmountAlreadySetAside` | `decimal` | Amount already deposited/reserved toward this item — **not** amount spent (FR-002). MUST be >= 0 |
| `AmountUsed` | `decimal` | Actual amount spent against this item so far (e.g. once its target month has occurred). Defaults to 0. Distinct from `AmountAlreadySetAside`: deposits build up *before* the expense happens, `AmountUsed` reflects what was actually drawn once it does. Detailed transaction-level recording is a separate feature — this is a simple running total. MUST be >= 0 |

**Validation rules**:
- `TotalAmount` must be strictly positive.
- `AmountAlreadySetAside` must be >= 0. It is capped at "no more deposits needed" once it reaches
  `TotalAmount` — depositing more than the target does not itself constitute overrun.
- `AmountUsed` must be >= 0. Overrun (Story 3) is specifically `AmountUsed > TotalAmount` —
  actual spending beyond the defined target — which is independent of how much has been
  deposited.
- `TargetMonth`, when present, must be between 1 and 12 (a Gregorian year always has exactly 12
  months — no leap-year branching is needed here, unlike the superseded Hebrew-calendar version).

**Derived state (not stored, computed by `BudgetSmoothingEngine`)**:
- `RemainingToDeposit = max(0, TotalAmount - AmountAlreadySetAside)` — the normal, not-yet-overrun
  smoothing requirement from FR-002.
- `Overrun = max(0, AmountUsed - TotalAmount)` — the excess actually spent beyond the target
  (Story 3 / FR-006). Kept as its own non-negative quantity, deliberately not merged into
  `AmountAlreadySetAside`, so "saved ahead of schedule" (no overrun, `AllocatedMonthly` trends to
  0) and "spent beyond the target" (overrun, `AllocatedMonthly` must increase) can never be
  confused — they are opposite outcomes and need independent signals.
- `MonthsRemaining` = number of calendar months from the current month to `TargetMonth` (or to
  calendar year-end/December if `TargetMonth` is `null`, or if the target month has already
  passed — FR-007 treats a passed, underfunded target month the same as year-end for the
  remaining-months count, spreading the shortfall over what's left of the year).
- `AllocatedMonthly = (RemainingToDeposit + Overrun) / MonthsRemaining` when `MonthsRemaining > 0`.
  When there is no remaining deposit requirement and no overrun, `AllocatedMonthly = 0` (edge
  case: "fully funded early").
- When only one calendar month remains (December) and a balance remains, `AllocatedMonthly`
  equals the full remaining amount for that month (edge case) — a natural consequence of dividing
  by `MonthsRemaining = 1`.

## AnnualReserve

A single manually-entered "money already set aside, not tied to any specific item" total per
year. Deliberately not linked to any `AnnualBudgetItem` — the user enters one aggregate figure
("how much do we have in the till"), independent of which items it will eventually cover.

| Field | Type | Notes |
|---|---|---|
| `Year` | `int` | Primary key — one row per year (upsert semantics via `PUT /api/reserve`) |
| `Amount` | `decimal` | MUST be >= 0. Defaults to 0 (via `IAnnualReserveRepository.GetAmountAsync` returning 0 when no row exists yet) |

## AnnualBudgetSummary (response shape, not a stored entity)

The year-level overview shown at the top of the client screen. Deliberately uses **flat,
always-divide-by-12** math (not the per-item `MonthsRemaining`-based `BudgetSmoothingEngine`) —
this is a simplified household-level heuristic requested by the user: "if I have X put aside, it
reduces what I need to save this month by X/12", independent of which individual items are due
when.

| Field | Formula | Notes |
|---|---|---|
| `ReserveOnHand` | `AnnualReserve.Amount` for the year (0 if unset) | The manually-entered "in the till" figure |
| `TotalAnnualBudget` | `sum(AnnualBudgetItem.TotalAmount)` for the year | All items, month-mapped and general alike |
| `NotYetCovered` | `max(0, TotalAnnualBudget - ReserveOnHand)` | Never negative — a reserve larger than the total budget just means everything is already covered |
| `MonthlyAllocation` | `NotYetCovered / 12` | The flat monthly figure shown as the headline stat |

Worked example (from the user's own numbers): `TotalAnnualBudget = 48,000`, `ReserveOnHand =
12,000` → `NotYetCovered = 36,000` → `MonthlyAllocation = 3,000`.

## CalendarYearCycle

A stateless helper concept (not a persisted entity), plain integer arithmetic over a fixed
12-month Gregorian year, answering: "what is the current month/year?", "how many months remain
between month X and month Y (or year-end) in a given year?". Implemented as a
`FamilyBudget.Core` service, not a database table. Unlike the superseded `HebrewYearCycle`, no
calendar library dependency is needed — a Gregorian year is always exactly 12 months.

## MonthlyAllocationSnapshot (response shape, not a stored entity)

The dashboard's smoothing-allocation line and the annual budget table are both projections over
`AnnualBudgetItem` + `CalendarYearCycle`, computed on request (see research.md — no persisted
snapshot). Fields returned to the API layer:

| Field | Type | Notes |
|---|---|---|
| `Year` | `int` | |
| `CurrentMonth` | `int` | 1-12 |
| `Items` | `AllocationLine[]` | one per active `AnnualBudgetItem` |
| `TotalRequiredAllocation` | `decimal` | sum of all `AllocationLine.AllocatedMonthly` |

`AllocationLine`: `{ AnnualBudgetItemId, Name, TargetMonth, TotalAmount,
AmountAlreadySetAside, AmountUsed, AllocatedMonthly }`

## Dashboard response shape (FR-003)

| Field | Type | Notes |
|---|---|---|
| `ProjectedIncome` | `decimal` | Input to this feature — see Assumptions in spec.md |
| `FixedExpenses` | `decimal` | Input to this feature — see Assumptions in spec.md |
| `TotalRequiredAllocation` | `decimal` | From `MonthlyAllocationSnapshot` above |
| `FreeBalance` | `decimal` | `ProjectedIncome - FixedExpenses - TotalRequiredAllocation` |

No state transitions apply to `AnnualBudgetItem` beyond value updates (`AmountAlreadySetAside`
changing over time as deposits are recorded by a future feature). This feature's original scope
was read-only; it now also supports creating new `AnnualBudgetItem` rows (`POST
/api/annual-budget-items`, always starting at `AmountAlreadySetAside = 0` and `AmountUsed = 0`)
and setting the `AnnualReserve` for a year (`PUT /api/reserve`) — both added per explicit user
direction alongside the client screen redesign. Editing/deleting existing items and recording
deposits/usage against an item remain out of scope for this feature.
