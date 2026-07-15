# Phase 0 Research: Transactions & Tithe (Chomesh) Offsets

All Technical Context fields were resolved with informed defaults during planning (no
`NEEDS CLARIFICATION` markers remained). This document records the reasoning behind the
non-obvious choices — chiefly, the exact carry-forward algorithm implied by FR-008/FR-009/FR-010,
which the spec describes in terms of "the prior month" but which recursively depends on every
month before it.

## Decision: Tithe obligation is computed by a forward walk over calendar months, not stored as mutable state

**Rationale**: Consistent with feature 001's decision to compute `AllocatedMonthly` on demand
rather than persist a mutable snapshot (avoiding a second source of truth — Constitution
Principle V), the monthly tithe obligation, the small-charity offset ledger, and the generic
credit carry-forward are all *derived* from the `Transaction` table, not stored as
independently-editable rows. Because FR-008(b) ("not-yet-offset small charity amount carried
forward from the prior month") and FR-009 ("excess carried forward as a credit... reducing the
following month's gross tithe target") are each recursively defined in terms of the previous
month's outcome, computing month M's obligation requires walking forward from the earliest month
that has any relevant transaction up to M, carrying two running values month-to-month:

- `unappliedSmallCharity` — the small-charity remainder not yet used as an offset (FR-010).
- `excessCredit` — the generic credit produced when a month's deductions exceeded its gross target
  (FR-009).

**Algorithm** (`TitheEngine.ComputeMonth(year, month)`), for each month M walked from the
household's first relevant month up to the target month:

1. `titheApplicableIncome(M)` = sum of Income transactions in M flagged tithe-applicable (FR-001a).
2. `grossTarget(M) = titheApplicableIncome(M) × TitheRate` (FR-007).
3. `fixedDonations(M)` = sum of Fixed Donation transactions in M.
4. `availableSmallCharity(M) = unappliedSmallCharity` (the running value carried in from M-1 —
   per the edge case, M's *own* new Small Charity Expense transactions are NOT available to M,
   only to M+1).
5. `remainingCapacity(M) = max(0, grossTarget(M) - fixedDonations(M) - excessCredit_in)`, where
   `excessCredit_in` is the running credit carried in from M-1.
6. `appliedSmallCharity(M) = min(availableSmallCharity(M), remainingCapacity(M))`.
7. `netBeforeFloor(M) = grossTarget(M) - fixedDonations(M) - excessCredit_in - appliedSmallCharity(M)`.
8. `netTitheDue(M) = max(0, netBeforeFloor(M))` (FR-009 floor).
9. `excessCredit_out = max(0, -netBeforeFloor(M))` — carried into M+1's `excessCredit_in`.
10. `unappliedSmallCharity_out = (availableSmallCharity(M) - appliedSmallCharity(M)) + thisMonthsNewSmallCharityExpenseTotal(M)`
    — carried into M+1's `unappliedSmallCharity`.

This directly implements FR-008, FR-009, and FR-010 without inventing behavior beyond what the
spec's edge cases describe, and keeps the two carry-forward concepts (small-charity offset vs.
generic excess credit) distinct per FR-010's explicit tracking requirement.

**Alternatives considered**:
- Persisting a mutable `MonthlyTitheObligation` row updated by a background job whenever
  transactions change — rejected: introduces a second source of truth that could drift from the
  underlying `Transaction` data, the same risk feature 001 already rejected for allocation
  snapshots.
- Only looking one month back (ignoring any deeper recursive history) — rejected: would silently
  drop small-charity remainders or credits that are more than one month old, violating FR-010's
  "100% accounted, never lost" requirement (SC-003) the moment a remainder survives two consecutive
  months without being fully absorbed.

**Assumption this introduces**: the walk starts from the earliest calendar month that has *any*
Income, Fixed Donation, or Small Charity Expense transaction recorded (i.e., whenever the
household started using this feature) — there is no separate "tithe tracking start date" setting.
For a single household with realistic transaction volumes, this walk is cheap enough to run on
every request, matching feature 001's precedent of computing on demand.

## Decision: `Transaction.IsTitheApplicable` is nullable, required exactly when `Type == Income`

**Rationale**: FR-001a requires every Income transaction to explicitly declare tithe-applicability
that MUST NOT default silently; other transaction types (expenses, donations) have no meaning for
this flag. Modeling it as `bool?` with a validation rule — "MUST be non-null when Type is Income,
MUST be null otherwise" — keeps the invariant enforceable at the Core layer (constructor/factory
guard) rather than relying on a magic default value that could be mistaken for a real answer.

**Alternatives considered**:
- A non-nullable `bool` defaulting to `true` or `false` — rejected: FR-001a explicitly forbids a
  silent default; a missing value must be a validation error, not an assumed answer.
- A separate `IncomeTitheClassification` child entity — rejected: over-engineered for a single
  boolean fact about an income row; adds a join for no behavioral benefit.

## Decision: `TitheSetting` follows the `AnnualReserve` single-row upsert pattern

**Rationale**: Feature 001 already established the pattern for a single household-wide config
value (`AnnualReserve`: one row, `PUT` upserts it, repository returns a default when absent). The
tithe rate (FR-006) is the same shape of fact — one value, no history — so reusing the pattern
keeps the codebase consistent rather than introducing a second config-storage convention.

**Alternatives considered**:
- A generic key-value `Settings` table — rejected: no other setting exists yet to justify the
  generality; premature abstraction for a single typed value.

## Decision: `TransactionType` and `PaymentMethod` are C# enums, stored as strings via EF Core value conversion

**Rationale**: Storing as strings (rather than raw integers) keeps the SQLite data
human-readable when inspected directly (consistent with the project's local-first,
single-household, low-ceremony philosophy) and avoids silent breakage if enum members are
reordered later. EF Core's built-in enum-to-string value conversion covers this with no custom
converter code.

**Alternatives considered**:
- Default EF Core integer storage for enums — rejected: fragile against future reordering of enum
  members and harder to eyeball when debugging the SQLite file directly.
- Separate lookup tables for type/payment method — rejected: over-engineered for a small, fixed,
  code-defined set of values with no user-facing management need.

## Decision: dashboard integration extends the existing `GET /api/dashboard` response rather than adding a parallel endpoint

**Rationale**: FR-013 requires the Monthly Dashboard (feature 001) to consume the computed net
tithe-due figure in place of its former external-input placeholder. Adding `titheDue` (and its
breakdown, per FR-012) as new fields on the existing `DashboardResponse` keeps the dashboard a
single call for the client, and matches Constitution Principle V (every number traceable, shown
alongside the existing income/expense/allocation/free-balance fields already on that response).

**Alternatives considered**:
- A separate `GET /api/tithe/{year}/{month}` endpoint consumed independently by the dashboard
  client code — rejected as the *sole* source: still needed for the Monthly Overview screen's own
  use (see contracts/api.md), but the dashboard endpoint itself is extended directly rather than
  requiring the client to stitch two calls together for one screen.

## Decision: xUnit + real SQLite for tests (unchanged from feature 001)

**Rationale**: No new information changes feature 001's existing testing decision — Constitution
Principle III still fixes SQLite as the only supported store, and the EF Core InMemory provider
still risks masking SQL-translation bugs. Reused without modification.
