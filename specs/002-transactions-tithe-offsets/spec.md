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
non-tithe-applicable income; a "מעשרות" (tithes) section showing four figures — the gross tithe
target, this month's active fixed-donation standing orders (drillable into a table the user can
add/edit/delete), the immediately preceding month's ad-hoc/small-charity donations (also drillable
and editable), and the amount still to donate (the protected net tithe due, after both fixed
donations and the prior-month offset); a read-only table of annual
budget items whose target month is the current month (money that needs to be withdrawn from the
annual reserve to cover them); budgeted-vs-used expense category tables (fixed/standing-order and
regular); a debt-repayments summary line; and finally an overall summary of total outflow and the
amount remaining to save for the month.

**Why this priority**: This is the primary screen where the household actually reviews its month —
it is the user-facing payoff of recording transactions (User Story 1) and of the protected tithe
math this feature guarantees (Constitution Principle IV), presented as fully traceable, separately
labeled sections rather than one opaque total (Constitution Principle V).

**Independent Test**: Can be fully tested by recording a mix of tithe-applicable income,
non-tithe-applicable income, donations, fixed expenses, and regular expenses for a month, then
verifying the screen shows each in its own section with correct subtotals, a "remaining to give"
figure equal to the month's protected net tithe due (`NetTitheDue`), and a bottom summary that
reconciles total income against total outflow and savings.

**Acceptance Scenarios**:

1. **Given** a month with both tithe-applicable and non-tithe-applicable income transactions,
   **When** the user opens the monthly overview screen, **Then** the income section shows two
   separate tables — one for tithe-applicable income and one for non-tithe-applicable income —
   each with its own subtotal, and only the tithe-applicable subtotal feeds the tithe calculation.
2. **Given** a month's active fixed-donation standing orders and its computed gross tithe target,
   **When** the user views the "מעשרות" section, **Then** it shows the gross target, the sum of
   this month's active standing orders, the sum of the immediately preceding month's ad-hoc/
   small-charity donations, and the amount still to donate — the protected net tithe due (gross
   target minus both this month's standing orders and the prior-month offset).
3. **Given** the user clicks the fixed-donations or prior-month-donations figure, **When** the
   drill-down opens, **Then** it shows an editable table (name/description, amount, and — for
   standing orders — an optional end month) that the user can add to, edit, or delete from, and
   the figure shown outside the drill-down always equals the sum of that table.
4. **Given** an annual budget item (feature 001) whose target month is the currently viewed
   month, **When** the user views the monthly overview screen, **Then** it appears in a read-only
   table of amounts that need to be withdrawn from the annual reserve this month, together with
   its total amount.
5. **Given** a month's recorded budget categories, **When** the user views the screen below the
   annual-withdrawals table, **Then** fixed/standing-order expenses and regular monthly expenses
   are each shown in their own budgeted-vs-used table, and a debt-repayments amount is shown as a
   single summary line.
6. **Given** all of the above sections for a month, **When** the user views the bottom of the
   screen, **Then** a summary shows the total amount going out (this month's fixed donations +
   fixed expenses used + regular expenses used + debt repayments) and the resulting amount
   remaining to save for the month.

---

### User Story 3 - Recurring Fixed Donations & Prior-Month Offset Management (Priority: P3)

The user manages fixed donations as named, recurring standing orders (with an optional end month)
instead of re-entering the same donation every month, and separately manages the ad-hoc/
small-charity donations that count toward next month's tithe deduction — both directly from the
"מעשרות" section's drill-down tables.

**Why this priority**: This refines and makes directly editable a mechanism that User Story 2
already depends on internally (the fixed-donations and prior-month-offset figures); it is valuable
for day-to-day upkeep but the core calculation in User Story 2 already works correctly with
whatever standing orders and donations exist.

**Independent Test**: Can be fully tested by creating a standing order with no end month and
verifying it applies to every future month; creating one with an end month and verifying it stops
applying the month after; and recording an ad-hoc donation in one month and verifying it appears
as next month's prior-month offset, editable there.

**Acceptance Scenarios**:

1. **Given** a fixed-donation standing order with no end month set, **When** the user views any
   future month's "מעשרות" section, **Then** that standing order's amount is included in the
   month's fixed-donations total.
2. **Given** a fixed-donation standing order with an end month set, **When** the user views a
   month after that end month, **Then** the standing order's amount is no longer included.
3. **Given** an ad-hoc/small-charity donation recorded in month M, **When** the user views month
   M+1's prior-month-donations drill-down, **Then** that donation appears there (not in month M's
   own calculation, per the edge case below), editable and deletable.

---

### Edge Cases

- What happens when a month's total net income is zero or the tithe rate produces a target smaller
  than the sum of that month's deductions? → The tithe due MUST be floored at zero for that month;
  it MUST NOT go negative or automatically create a refund.
- What happens when fixed donations and the prior-month donations together exceed the gross tithe
  target for the month? → `NetTitheDue` is simply floored at zero for that month; per explicit user
  direction, no further multi-month credit is carried forward — the lookback is exactly one
  calendar month, not recursive.
- What happens when a transaction, standing order, or budget category is edited or deleted after a
  month's tithe-due figure has already been viewed? → The tithe-due figure MUST be recomputed from
  current data on demand, the same way the annual budget allocation recomputes when its inputs
  change.
- What happens when a small-charity/ad-hoc donation is recorded in the same month whose tithe is
  being calculated? → It MUST NOT reduce that same month's tithe target; only the immediately
  preceding calendar month's ad-hoc donations are eligible, so this month's own ad-hoc donations
  become eligible only for next month's calculation.
- What happens to a fixed-donation standing order with no end month set? → It MUST be treated as
  applying to every calendar month, including months before it was created, for the purpose of
  computing whether it applies to a given target month (no separate "start month" is tracked).
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
- **FR-008**: The system MUST deduct from the gross tithe target, before presenting the protected
  tithe due (`NetTitheDue`) for the month: (a) the sum of that month's active fixed-donation
  standing orders (FR-023), and (b) the sum of ad-hoc/small-charity donations dated in exactly the
  immediately preceding calendar month.
- **FR-009**: The system MUST floor `NetTitheDue` at zero for any month where deductions meet or
  exceed the gross tithe target. Per explicit user direction, no further carry-forward of any
  excess is tracked beyond this one-month lookback (this is a deliberate simplification of the
  originally-specified multi-month recursive carry).
- **FR-010**: *(superseded by FR-023's standing-order model and the flat one-month lookback in
  FR-008/FR-009 — no separate small-charity ledger/carry-forward tracking is maintained beyond the
  immediately preceding month.)*
- **FR-011**: The system MUST NOT allow the protected tithe-due figure (`NetTitheDue`) to be
  presented or exported without both deductions applied — a gross (pre-deduction) figure MUST
  never be treated as the final tithe due wherever the protected obligation is required (e.g., the
  Monthly Dashboard, and the Monthly Overview screen's "amount still to donate" figure — see
  FR-015).
- **FR-012**: The system MUST display the tithe breakdown as separate, traceable components (gross
  target, this month's fixed-donations total, the prior month's ad-hoc-donations total, and the
  protected net due shown as the "amount still to donate") rather than a single opaque number.
  `StillToDonateAfterFixed` (gross minus this month's fixed donations only, not the prior-month
  offset) is retained internally as an intermediate figure but MUST NOT be displayed or exported as
  the "amount still to donate."
- **FR-013**: The system MUST expose the computed monthly net tithe-due figure for consumption by
  the Monthly Dashboard (feature 001), replacing the previous placeholder assumption that this
  figure is an external input.
- **FR-014**: The system MUST display a monthly overview screen with an income section containing
  two separate tables — tithe-applicable income and non-tithe-applicable income — each with its
  own subtotal.
- **FR-015**: The system MUST display, on the monthly overview screen, an "amount still to donate"
  figure equal to the protected `NetTitheDue` — the gross tithe target minus both this month's
  active fixed-donation standing orders and the prior-month offset (FR-008/FR-009/FR-011), floored
  at zero. (Superseded 2026-07-19, per explicit user direction: this figure previously omitted the
  prior-month offset by design — see FR-011/FR-012.)
- **FR-016**: The system MUST display, on the monthly overview screen, a Fixed/Standing-Order
  Expenses table and a separate Regular Monthly Expenses table, each listing that month's named
  expense budget categories with the amount budgeted for the month, the amount used so far, and
  the resulting remaining balance (FR-021).
- **FR-017**: The system MUST display, on the monthly overview screen, a debt-repayments summary
  line as a single aggregate amount; until a dedicated Debts Ledger feature exists, this line MUST
  default to zero rather than block the rest of the screen.
- **FR-018**: The system MUST display, at the bottom of the monthly overview screen, a summary of
  total outflow (this month's active fixed-donation standing orders + fixed expenses used +
  regular expenses used + debt repayments) and the resulting amount remaining to save for the
  month.
- **FR-019**: The system MUST let the user add, edit (description and amount), and delete
  individual income transactions directly from the monthly overview screen's income tables,
  without leaving the screen.
- **FR-020**: *(superseded by FR-023 for fixed donations, now managed as recurring standing
  orders, and FR-024 for ad-hoc/small-charity donations, managed via the prior-month drill-down.)*
- **FR-021**: The system MUST let the user add, rename, delete, and set the budgeted amount and
  used amount for named Fixed/Regular expense budget categories, scoped to a specific calendar
  month (no automatic carry-over from month to month); the remaining balance for the category
  MUST be computed as budgeted minus used.
- **FR-022**: Every amount input on the monthly overview screen and the annual budget screen MUST
  accept a simple arithmetic formula (e.g. `600-200`) as well as a plain number. The computed
  result MUST be what all calculations use; the raw formula text MUST be persisted and redisplayed
  whenever the field is focused again, while the computed result is shown when the field is not
  focused.
- **FR-023**: The system MUST let the user define a named fixed-donation standing order (recurring
  monthly donation) with an amount and an optional end month; a standing order with no end month
  MUST apply to every calendar month (past and future), and one with an end month MUST apply only
  up to and including that month. The system MUST let the user add, edit (name, amount, end month),
  and delete standing orders.
- **FR-024**: The system MUST let the user add, edit, and delete individual ad-hoc/small-charity
  donations for a given calendar month directly from the "מעשרות" section's prior-month drill-down.
- **FR-025**: The system MUST display, on the monthly overview screen, a read-only table of every
  annual budget item (feature 001) whose target month is the currently viewed calendar month,
  together with each item's total amount, so the user knows what needs to be withdrawn from the
  annual reserve this month; this table MUST NOT be added into the monthly outflow/savings summary
  (FR-018), since it reflects reallocating already-saved money rather than new spending from this
  month's income.

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
- **FixedDonationStandingOrder**: A named recurring monthly donation (e.g. "Yeshiva"). Key
  attributes: name, amount (plus optional formula), and an optional end year/month — no end date
  means it applies indefinitely; an end date means it stops applying after that calendar month.
  Replaces the need to re-enter the same Fixed Donation transaction every month.
- **MonthlyTitheObligation**: The computed result for a given calendar month — tithe-applicable and
  non-tithe-applicable income, gross tithe target, this month's fixed-donations total (from active
  standing orders), the immediately preceding month's ad-hoc-donations total, the protected net
  tithe due, and the separate unprotected "still to donate after fixed donations" figure.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can record a new transaction (type, amount, payment method) in under 15
  seconds.
- **SC-002**: For any calendar month, the displayed net tithe due always reflects the protected
  deductions (this month's fixed-donation standing orders and the immediately preceding month's
  ad-hoc donations) with zero manual recalculation by the user.
- **SC-003**: A user can add a recurring fixed-donation standing order once and have it correctly
  apply to every subsequent month without re-entry, until its end month (if any) has passed.
- **SC-004**: A user can view the full breakdown of any month's tithe calculation (gross target,
  each deduction, the protected net due, and the unprotected still-to-donate figure) within 10
  seconds of opening the relevant screen.
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
- Fixed donations are managed as named recurring standing orders (FR-023), not re-entered as
  transactions each month; a standing order with no end month is assumed to apply retroactively to
  any month (no separate "start month" is tracked), which is an acceptable simplification for a
  single-household personal tool where the user controls their own data entry.
- Per explicit user direction, the prior-month small-charity/ad-hoc-donation offset looks back
  exactly one calendar month with no further multi-month carry-forward — a deliberate
  simplification from the feature's original multi-month recursive design (see constitution.md
  v1.2.0 amendment).
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
