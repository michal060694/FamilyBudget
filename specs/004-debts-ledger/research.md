# Phase 0 Research: Debts Ledger (Debts & Loans)

All Technical Context fields were resolved with informed defaults during planning (no
`NEEDS CLARIFICATION` markers remained). This document records the reasoning behind the
non-obvious choices.

## Decision: No separate `Repayment` table — the generated `Transaction` IS the repayment history

**Rationale**: FR-010/FR-011 already require every repayment to generate a `Transaction` (feature
002) for the exact amount paid. That transaction, dated in the month it was recorded, already
serves as the durable historical record of the repayment — a separate `Repayments` table would
duplicate this fact and risk drifting from it (the same second-source-of-truth risk rejected for
every derived figure in features 001-003). `Debt.CurrentBalance` only needs to store the
*current* remaining balance, not the history of how it got there.

**Alternatives considered**:
- A dedicated `Repayment` entity (`DebtId`, `Amount`, `Date`) in addition to the generated
  `Transaction` — rejected: redundant with the transaction already required by FR-010/FR-011, and
  a second place the "amount actually paid" fact could disagree with the transaction ledger.

## Decision: `TransactionType.DebtRepayment` is a new enum value, used only for payable outflows

**Rationale**: FR-011 requires a payable repayment's transaction to be "distinguishable from other
expense transactions so it can be summed separately" for FR-012 (the Monthly Overview's
debt-repayments figure). Feature 002's `TransactionType` enum already exists and is stored via an
EF Core string conversion (`HasConversion<string>()`), so adding one more member
(`DebtRepayment`) is a purely additive change — existing rows and existing filters
(`t.Type is TransactionType.FixedDonation or TransactionType.SmallCharityExpense`, etc.) are
unaffected since they don't reference the new value.

A receivable's collection, by contrast, is simply an `Income` transaction with
`IsTitheApplicable = false` — no new type is needed there, since "income, not tithe-applicable" is
already exactly the right shape (FR-010) and doesn't need to be summed separately from other
non-tithe-applicable income.

**Alternatives considered**:
- Tagging debt-repayment transactions via their `Description` text (e.g., a fixed prefix like
  "החזר חוב - ") and pattern-matching on it for FR-012 — rejected: fragile (a user editing the
  description would silently break the Monthly Overview figure), and the codebase already has a
  precedent (the `TransactionType` enum itself) for exactly this "which category does this
  transaction belong to" need.
- A boolean `IsDebtRepayment` flag on `Transaction` instead of a new enum value — rejected: would
  need to work alongside `Type` (e.g., "Income and IsDebtRepayment" vs. "Expense and
  IsDebtRepayment") to distinguish the two directions, more complex than one new enum value that
  already carries the necessary distinction on its own for the payable side (the receivable side
  doesn't need a flag at all, per above).

## Decision: `DebtRepaymentService` centralizes the "update balance + create transaction" consistency rule

**Rationale**: Recording a repayment is the one place in this feature with real cross-entity
business logic — the `Debt` and the generated `Transaction` must always change together. Putting
this in a dedicated `FamilyBudget.Core` service (rather than inline in the API endpoint, or split
across the endpoint and repository) keeps it independently unit-testable and gives future callers
(e.g., a future bulk-import) one place to call rather than a rule they must remember to replicate.

**Alternatives considered**:
- Inline logic directly in `DebtEndpoints.cs`'s repayment handler — rejected: the API layer is
  meant to stay a thin translation layer per Constitution Principle II; this rule is business
  logic and belongs in Core.

## Decision: Editing a debt's original amount preserves the amount already paid

**Rationale**: FR-003 allows editing a debt's amount (e.g., correcting a typo). If repayments have
already been recorded against it, naively overwriting `CurrentBalance` to the new amount would
silently erase the fact that some of it was already paid. Instead, `Debt.Update(...)` recomputes
`CurrentBalance` as `newOriginalAmount − (originalAmount − currentBalanceBeforeEdit)` — i.e., it
preserves "amount already paid" and reapplies it against the corrected original amount — and
re-evaluates `Status` accordingly (closing it if the recomputed balance is now zero or less,
reopening it if a previously-Closed debt's corrected amount leaves a positive balance).

**Alternatives considered**:
- Disallowing amount edits once any repayment has been recorded — rejected: more restrictive than
  FR-003 requires, and correcting a typo in the original amount is a legitimate, common edit.

## Decision: xUnit + real SQLite as the test double (unchanged from features 001-003)

**Rationale**: No new information changes the existing testing decision — Constitution Principle
III fixes PostgreSQL as the production store, but tests still run against an in-memory SQLite
connection as a fast stand-in. Reused without modification.
