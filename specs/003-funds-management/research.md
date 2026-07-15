# Phase 0 Research: Funds Management & Summary

All Technical Context fields were resolved with informed defaults during planning (no
`NEEDS CLARIFICATION` markers remained). This document records the reasoning behind the
non-obvious choices.

## Decision: `Fund` and `FundEarmark` as flat top-level entities, not an aggregate root

**Rationale**: This codebase has consistently modeled parent/child financial records as
independent, flatly-queryable/CRUD-able entities linked by a foreign key
(`AnnualBudgetItem`/`AnnualReserve`, `MonthlyExpenseBudgetItem`, `FixedDonationStandingOrder`) —
never as a DDD-style aggregate root with nested collection mutation. Following the same pattern
for `Fund`/`FundEarmark` (a `FundEarmark` referencing its `FundId`) keeps the codebase consistent
and lets each side have its own simple repository and endpoint set.

**Alternatives considered**:
- `Fund` as an aggregate root owning an in-memory `List<FundEarmark>` with add/remove methods —
  rejected: inconsistent with every other parent/child relationship in this codebase, and adds
  ceremony (loading the full collection just to mutate one line) with no benefit at this scale.

## Decision: Reconciliation (`Discrepancy`) is computed on demand, not stored

**Rationale**: Consistent with every other derived figure in this project (allocation lines,
tithe obligation, budget-item remaining balance) — `Discrepancy = TotalBalance - Sum(Earmarks)` is
cheap to compute from the two underlying facts already in the database. Storing it separately
would create a second source of truth that could drift from the fund/earmark rows themselves,
which is exactly the failure mode Constitution Principle IV exists to prevent.

**Alternatives considered**:
- A stored, separately-updated `Discrepancy` column on `Fund` — rejected: same second-source-of-
  truth risk already rejected for the annual budget allocation and tithe engine in features 001/002.

## Decision: Cascade delete from `Fund` to `FundEarmark`

**Rationale**: FR-003 requires deleting a fund to remove its earmark lines too. Configuring EF
Core's `OnDelete(DeleteBehavior.Cascade)` on the `FundEarmark.FundId` foreign key lets the database
enforce this in one operation rather than the repository needing to manually delete child rows
first (which would be an easy invariant to accidentally break in a future edit).

**Alternatives considered**:
- Manual cascade in `FundRepository.DeleteAsync` (delete earmarks, then the fund) — rejected as the
  primary mechanism: works, but duplicates logic the database can guarantee more robustly;
  reasonable as a fallback only if cascade configuration were unavailable, which it is not here.

## Decision: No formula-input support for Fund/FundEarmark amounts in this feature

**Rationale**: Feature 002 added arithmetic-formula support (FR-022) to the Monthly Overview and
Annual Budget amount fields per explicit user direction at that time. The user's request for this
feature did not ask for it, and adding it here would be scope creep beyond what was requested —
per this project's practice of not adding unrequested capabilities. It can be added later using
the exact same client-side pattern already built (`attachFormulaInput`) if requested.

**Alternatives considered**:
- Proactively adding formula support for consistency — rejected: not requested for this feature;
  the existing `attachFormulaInput` widget can be reused verbatim on these fields later with no
  backend redesign needed, so deferring it costs nothing.

## Decision: xUnit + real SQLite for tests (unchanged from features 001/002)

**Rationale**: No new information changes the existing testing decision — Constitution Principle
III still fixes SQLite as the only supported store, and the EF Core InMemory provider still risks
masking SQL-translation bugs. Reused without modification.
