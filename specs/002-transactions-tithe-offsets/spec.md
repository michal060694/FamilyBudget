# Feature Specification: Transactions & Tithe (Chomesh) Offsets

**Feature Branch**: `002-transactions-tithe-offsets`

**Created**: 2026-07-14

**Status**: Draft

**Input**: User description: "Transactions & Tithe (Chomesh) Offsets: a system that lets the user
record day-to-day income and expense transactions, tagging each with a payment method (credit
card, bank transfer/checking, cash, or check), and distinguishes recurring fixed contributions
(donations set up as standing orders each month) from small ad-hoc charity expenses that are
pending offset. The system automatically derives a monthly tithe (chomesh) target of 20% of total
net income entered for the month. Before the tithe obligation for the month is finalized, the
system automatically deducts: (1) the recurring fixed donations defined for that month, and (2)
small charity/donation expenses recorded in the previous month that have not yet been offset
(TitheOffset) — carrying forward any not-yet-offset amount. This feeds Screen 5 (Transactions &
Tithe Offsets entry form) and the tithe line on the existing Monthly Dashboard (Screen 1),
replacing the placeholder assumption that projected income/fixed expenses are external inputs.
Business rule: the tithe obligation must only be computed after netting out the previous month's
pending small-charity offsets and the recurring fixed donations — this 'protected tithe deduction'
must never be bypassed. A partial reconciliation of a person-to-person debt (handled by a separate
future Debts Ledger feature) is out of scope here, as are fund/investment account balances
(separate future Funds feature) — this feature only needs to record transactions, classify them by
type (income/fixed expense/regular expense/small charity expense/fixed donation) and payment
method, and compute the resulting monthly tithe-due figure with the automatic offsets described
above."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record Day-to-Day Transactions (Priority: P1)

The user enters income and expense transactions as they occur throughout the month — a salary
deposit, a grocery purchase, a fixed monthly donation, a small cash gift to charity — tagging each
one with its type and the payment method used (credit card, bank transfer/checking, cash, or
check).

**Why this priority**: Every other capability in this feature (the tithe calculation, the offset
carry-forward, the dashboard's income/expense lines) depends on transactions actually being
recorded. Without this, there is no real data feeding the system — it is the foundational slice.

**Independent Test**: Can be fully tested by recording several transactions of different types and
payment methods for the current calendar month and verifying each is stored with its date, amount,
type, and payment method, and appears correctly in the month's transaction list.

**Acceptance Scenarios**:

1. **Given** the transaction entry form, **When** the user records an income transaction of a
   given amount for the current month, **Then** the transaction is saved and included in that
   month's total net income.
2. **Given** the transaction entry form, **When** the user records an expense and selects a
   payment method (credit card, bank transfer, cash, or check), **Then** the transaction is saved
   with that payment method attached and distinguishable from transactions using other payment
   methods.
3. **Given** a calendar month with several recorded transactions of mixed types, **When** the user
   views the month's transaction list, **Then** every transaction is shown with its date, type,
   amount, and payment method, and the list can be filtered by type and/or payment method.

---

### User Story 2 - Monthly Overview Screen: Income, Tithe & Expense Breakdown (Priority: P2)

The user opens a single monthly overview screen that lays out the full financial picture for the
current calendar month, top to bottom: an income section split into tithe-applicable and
non-tithe-applicable income; a donations section showing every donation given this month and how
much more is still owed toward the month's tithe target; then expense sections (fixed/standing-
order expenses and regular monthly expenses) plus a debt-repayments summary line; and finally an
overall summary of total outflow and the amount remaining to save for the month.

**Why this priority**: This is the primary screen where the household actually reviews its month —
it is the user-facing payoff of recording transactions (User Story 1) and of the protected tithe
math this feature guarantees (Constitution Principle IV), presented as fully traceable, separately
labeled sections rather than one opaque total (Constitution Principle V).

**Independent Test**: Can be fully tested by recording a mix of tithe-applicable income,
non-tithe-applicable income, donations, fixed expenses, and regular expenses for a month, then
verifying the screen shows each in its own section with correct subtotals, a "remaining to give"
figure equal to the month's net tithe due minus donations already given, and a bottom summary that
reconciles total income against total outflow and savings.

**Acceptance Scenarios**:

1. **Given** a month with both tithe-applicable and non-tithe-applicable income transactions,
   **When** the user opens the monthly overview screen, **Then** the income section shows two
   separate tables — one for tithe-applicable income and one for non-tithe-applicable income —
   each with its own subtotal, and only the tithe-applicable subtotal feeds the tithe calculation.
2. **Given** a month's donation transactions and its computed net tithe due, **When** the user
   views the donations section, **Then** it lists every donation given that month and shows a
   "remaining to give" figure equal to the net tithe due minus the total already given, floored at
   zero.
3. **Given** a month's donations meet or exceed the net tithe due, **When** the user views the
   donations section, **Then** the "remaining to give" figure shows zero (the obligation is fully
   met) rather than a negative number.
4. **Given** a month's recorded transactions, **When** the user views the screen below the
   donations section, **Then** fixed/standing-order expenses and regular monthly expenses are each
   shown in their own table, and a debt-repayments amount is shown as a single summary line.
5. **Given** all of the above sections for a month, **When** the user views the bottom of the
   screen, **Then** a summary shows the total amount going out (donations given + fixed expenses +
   regular expenses + debt repayments) and the resulting amount remaining to save for the month.
6. **Given** a displayed tithe-related figure anywhere on the screen, **When** the user inspects
   it, **Then** the gross target, the fixed-donations deduction, and the prior-month-offset
   deduction remain individually traceable rather than only the final net number (per the
   underlying protected tithe calculation).

---

### User Story 3 - Small-Charity Offset Carry-Forward Visibility (Priority: P3)

The user reviews, for any given month, how much was spent on small ad-hoc charity/donation
expenses, how much of that was already applied as an offset against a tithe obligation, and how
much remains unapplied and will carry forward to reduce next month's tithe target.

**Why this priority**: This refines and makes auditable a mechanism that User Story 2 already
depends on internally; it is valuable for trust and troubleshooting but the core calculation in
User Story 2 works correctly without a dedicated visibility view.

**Independent Test**: Can be fully tested by recording small-charity expenses across two
consecutive months and verifying the unoffset remainder from the first month appears correctly as
the carried-forward amount reducing the second month's tithe target, with the running total shown
per month.

**Acceptance Scenarios**:

1. **Given** small-charity expenses recorded in a month that exceed the amount applied as offset
   that same month, **When** the user views that month's offset summary, **Then** the unapplied
   remainder is shown and is the exact amount that reduces the following month's tithe target.
2. **Given** a small-charity remainder was fully applied as an offset in the following month,
   **When** the user views the month after that, **Then** no further carry-forward from the
   original month remains outstanding.

---

### Edge Cases

- What happens when a month's total net income is zero or the tithe rate produces a target smaller
  than the sum of that month's deductions? → The tithe due MUST be floored at zero for that month;
  it MUST NOT go negative or automatically create a refund.
- What happens when fixed donations and the prior-month offset together exceed the gross tithe
  target for the month? → The excess amount MUST carry forward as a credit that reduces the
  following month's gross tithe target (before applying that month's own deductions), consistent
  with the same "never bypassed, never lost" protected-deduction principle.
- What happens when a transaction is edited or deleted after a month's tithe-due figure has already
  been viewed? → The tithe-due figure MUST be recomputed from current transaction data on demand,
  the same way the annual budget allocation recomputes when its inputs change.
- What happens when a small-charity expense is recorded in the same month whose tithe is being
  calculated? → It MUST NOT reduce that same month's tithe target; only the prior month's
  not-yet-offset small-charity amount is eligible, so this month's small-charity expenses become
  eligible to offset next month's target.
- What happens when no payment method or type is selected for a transaction? → The system MUST
  require both a type and a payment method before a transaction can be saved.
- What happens when an Income transaction is not marked as tithe-applicable or not? → The system
  MUST require the tithe-applicable flag to be set explicitly for every Income transaction; it
  MUST NOT default silently to either value, and non-tithe-applicable income MUST be fully excluded
  from the gross tithe target calculation while still counting toward total income in the
  bottom-line savings summary.
- What happens on the monthly overview screen before the Debts Ledger feature exists? → The
  debt-repayments line MUST be shown as a single summary amount (defaulting to zero when no such
  data source is available yet) rather than a detailed table, so the screen remains fully usable
  ahead of that future feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow the user to record a transaction with a date, an amount, a
  type (Income, Fixed Expense, Regular Expense, Fixed Donation, or Small Charity Expense), and a
  payment method (Credit Card, Bank Transfer/Checking, Cash, or Check).
- **FR-001a**: For every Income transaction, the system MUST require the user to explicitly mark
  it as either tithe-applicable or not tithe-applicable.
- **FR-002**: The system MUST require both a type and a payment method to be specified before a
  transaction can be saved.
- **FR-003**: The system MUST allow the user to edit or delete a previously recorded transaction,
  and any figure derived from it (monthly income total, tithe due, offset carry-forward) MUST be
  recomputed accordingly.
- **FR-004**: The system MUST allow the user to view the list of transactions for a given calendar
  month, filterable by type and/or payment method.
- **FR-005**: The system MUST compute, for a calendar month, both the total tithe-applicable
  income and the total non-tithe-applicable income as separate sums of that month's Income-type
  transactions, split by the tithe-applicable flag (FR-001a).
- **FR-006**: The system MUST maintain a stored, user-configurable tithe rate (e.g., 0.1 for
  maaser or 0.2 for chomesh) and MUST NOT hardcode this rate anywhere in the calculation logic.
- **FR-007**: The system MUST compute each month's gross tithe target as the month's total
  tithe-applicable income (FR-005) multiplied by the configured tithe rate; non-tithe-applicable
  income MUST NOT contribute to the gross tithe target.
- **FR-008**: The system MUST deduct from the gross tithe target, before presenting the tithe due
  for the month: (a) the sum of that month's Fixed Donation transactions, and (b) the not-yet-
  offset Small Charity Expense amount carried forward from the prior month.
- **FR-009**: The system MUST floor the net tithe due at zero for any month where deductions meet
  or exceed the gross tithe target, and MUST carry the excess forward as a credit reducing the
  following month's gross tithe target.
- **FR-010**: The system MUST track, per calendar month, the total Small Charity Expense amount
  recorded, the portion applied as an offset to a tithe obligation, and the unapplied remainder
  carried forward to the following month.
- **FR-011**: The system MUST NOT allow the tithe-due figure to be presented or exported without
  the protected deductions applied — a gross (pre-deduction) figure MUST never be treated as the
  final tithe due.
- **FR-012**: The system MUST display the tithe-due calculation as separate, traceable components
  (gross target, fixed-donations deduction, prior-month-offset deduction, net due) rather than a
  single opaque number.
- **FR-013**: The system MUST expose the computed monthly net tithe-due figure for consumption by
  the Monthly Dashboard (feature 001), replacing the previous placeholder assumption that this
  figure is an external input.
- **FR-014**: The system MUST display a monthly overview screen with an income section containing
  two separate tables — tithe-applicable income and non-tithe-applicable income — each with its
  own subtotal.
- **FR-015**: The system MUST display, on the monthly overview screen, a donations section listing
  the month's donation transactions together with a "remaining to give" figure equal to the net
  tithe due (FR-008/FR-009) minus the total donations given so far that month, floored at zero.
- **FR-016**: The system MUST display, on the monthly overview screen, a Fixed/Standing-Order
  Expenses table and a separate Regular Monthly Expenses table, each listing that month's named
  expense budget categories with the amount budgeted for the month, the amount used so far, and
  the resulting remaining balance (FR-021).
- **FR-017**: The system MUST display, on the monthly overview screen, a debt-repayments summary
  line as a single aggregate amount; until a dedicated Debts Ledger feature exists, this line MUST
  default to zero rather than block the rest of the screen.
- **FR-018**: The system MUST display, at the bottom of the monthly overview screen, a summary of
  total outflow (donations given + fixed expenses used + regular expenses used + debt repayments)
  and the resulting amount remaining to save for the month.
- **FR-019**: The system MUST let the user add, edit (description and amount), and delete
  individual income transactions directly from the monthly overview screen's income tables,
  without leaving the screen.
- **FR-020**: The system MUST let the user add, edit (description/name and amount), and delete
  individual donation transactions (fixed or small-charity) directly from the monthly overview
  screen's donations table.
- **FR-021**: The system MUST let the user add, rename, delete, and set the budgeted amount and
  used amount for named Fixed/Regular expense budget categories, scoped to a specific calendar
  month (no automatic carry-over from month to month); the remaining balance for the category
  MUST be computed as budgeted minus used.
- **FR-022**: Every amount input on the monthly overview screen and the annual budget screen MUST
  accept a simple arithmetic formula (e.g. `600-200`) as well as a plain number. The computed
  result MUST be what all calculations use; the raw formula text MUST be persisted and redisplayed
  whenever the field is focused again, while the computed result is shown when the field is not
  focused.

### Key Entities

- **Transaction**: A single recorded income or expense event. Key attributes: date, amount (plus an
  optional raw formula string per FR-022), type (Income, Fixed Expense, Regular Expense, Fixed
  Donation, Small Charity Expense), payment method (Credit Card, Bank Transfer/Checking, Cash,
  Check), optional description/note, and — for Income transactions only — a tithe-applicable flag.
  Fixed Expense/Regular Expense remain valid transaction types for general ledger use, but the
  monthly overview screen's Fixed/Regular Expense sections are driven by
  `MonthlyExpenseBudgetItem` (below), not by raw transactions of these types.
- **MonthlyExpenseBudgetItem**: A named Fixed or Regular expense category planned for one specific
  calendar month (e.g. "Rent" for July 2026) — not automatically recurring month to month. Key
  attributes: name, type (Fixed or Regular Expense), budgeted amount for the month (plus optional
  formula), amount used so far this month (manually entered/overwritten, plus optional formula,
  mirroring `AnnualBudgetItem`'s usage-tracking pattern), and a computed remaining balance
  (budgeted minus used).
- **TitheSetting**: The user-configurable tithe rate applied to net income each month (e.g., 0.1 or
  0.2).
- **MonthlyTitheObligation**: The computed result for a given calendar month — total net income,
  gross tithe target, fixed-donations deduction, prior-month-offset deduction applied, net tithe
  due, and any excess credit carried forward.
- **SmallCharityOffsetLedger**: Per calendar month, the total Small Charity Expense amount
  recorded, the amount applied as an offset, and the unapplied remainder carried to the next month.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can record a new transaction (type, amount, payment method) in under 15
  seconds.
- **SC-002**: For any calendar month, the displayed net tithe due always reflects the protected
  deductions (fixed donations and prior-month offset) with zero manual recalculation by the user.
- **SC-003**: Across any sequence of consecutive months, 100% of small-charity expense amounts are
  accounted for as either "applied as offset" or "carried forward" — none are ever lost or double
  counted.
- **SC-004**: A user can view the full breakdown of any month's tithe-due calculation (gross
  target, each deduction, net due) within 10 seconds of opening the relevant screen.
- **SC-005**: From the monthly overview screen alone, a user can determine, without opening any
  other screen, how much of this month's income counted toward the tithe, how much is still owed
  to charity this month, and how much is left to save — within 10 seconds.

## Assumptions

- The tithe rate is a single household-wide setting applied uniformly to all net income each
  month; per-transaction or per-income-source tithe rates are out of scope for this feature.
- "Total net income" for tithe purposes is the sum of Income transactions tagged as
  tithe-applicable for the month (FR-001a/FR-005/FR-007); non-tithe-applicable income still counts
  toward the household's total income in the bottom-line savings summary. Netting against expenses
  (beyond the described fixed-donation and small-charity offsets) is out of scope.
- Recurring Fixed Donation transactions must be entered for the specific month they apply to
  (no automatic month-to-month recurrence/scheduling engine is included in this feature).
- Only one household/user context exists (no multi-user permissions model is required for this
  feature), consistent with feature 001.
- Fund/investment account balances and the mechanics of "earmarking" money within a fund are out of
  scope for this feature and are covered by a separate future Funds feature; this feature only
  needs the transaction ledger and tithe calculation to be correct and auditable on their own.
- Person-to-person or gemach debt reconciliation (partial repayments, statuses) is out of scope for
  this feature and is covered by a separate future Debts Ledger feature; the monthly overview
  screen's debt-repayments line is a placeholder summary (defaulting to zero) until that feature
  exists, not a full ledger.
- The monthly overview screen described in User Story 2 supersedes the simpler income/expense line
  items originally assumed for the Monthly Dashboard in feature 001 — this feature owns that
  screen's data and layout going forward.
- `MonthlyExpenseBudgetItem`'s "used amount" (FR-021) is a manually entered/overwritten figure —
  like `AnnualBudgetItem.AmountUsed` in feature 001 — rather than being automatically reconciled
  against the sum of any raw Fixed/Regular Expense transactions; the two are intentionally
  independent so the budgeted-vs-used planning view never silently drifts from what the user
  actually typed in.
- Formula support (FR-022) only needs to compute a numeric result and persist the raw formula
  string for redisplay — it does not need to re-validate the formula against the stored amount on
  the server; the server accepts and stores whatever (amount, formula) pair the client sends,
  matching this project's existing low-ceremony, single-household trust model.
