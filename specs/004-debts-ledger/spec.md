# Feature Specification: Debts Ledger (Debts & Loans)

**Feature Branch**: `004-debts-ledger`

**Created**: 2026-07-15

**Status**: Draft

**Input**: User description: "Debts Ledger (Debts & Loans): a system that tracks mutual debts with
individuals, gemachim, or institutions in both directions on one screen split into two tables —
'owed to us' (Accounts Receivable: person/gemach name, amount, target repayment date, notes) and
'we owe' (Accounts Payable: lender/gemach name, amount, repayment rate if any, current balance).
Each debt has a status (open/closed) and a current remaining balance that starts at the original
amount. The user can create a new debt entry in either direction, edit its details, and record a
repayment against it — full or partial. Recording a repayment reduces the debt's current balance
by the paid amount; if the balance reaches zero the debt's status automatically becomes closed.
User flow (from the source spec): someone who owed the family 1,000 pays it back via bank
transfer — the user opens the Debts Ledger, selects that person, and marks 'repayment received';
the system updates that debt's status/balance and automatically generates a corresponding
transaction in the current month's transaction ledger (an Income-type transaction for a receivable
being collected, tagged as not tithe-applicable since it is the return of principal rather than
new income) so the monthly cash-flow picture stays accurate without manual double-entry.
Symmetrically, when the user records a payment made toward a debt the household owes (a payable),
the system generates a corresponding outflow transaction for that payment. Edge case: a debt that
is only partially closed must have its ledger balance updated to reflect only the amount actually
paid, with the generated transaction also reflecting only that actual paid amount — not the full
original debt. This feature also replaces the existing Monthly Overview screen's debt-repayments
placeholder (currently always zero, per feature 002's spec) with the real sum of this month's
payable-repayment transactions, now that this feature exists to supply that data."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Record & Manage Debts in Both Directions (Priority: P1)

The user creates a debt entry for either direction — someone/some gemach that owes the family
money (a receivable), or money the family owes someone/some gemach (a payable) — with a
counterparty name, an original amount, and direction-specific details (a target repayment date for
receivables, a repayment rate for payables), plus free-text notes. The user can edit these details
later or delete the entry entirely.

**Why this priority**: Every other capability in this feature (viewing the ledger, recording
repayments) depends on debt entries existing. Without this, there is no real data feeding the
system — it is the foundational slice.

**Independent Test**: Can be fully tested by creating a receivable and a payable debt entry, each
with their direction-specific fields, editing their details, and deleting one, verifying each
change persists correctly.

**Acceptance Scenarios**:

1. **Given** the debts ledger, **When** the user creates a new receivable debt with a counterparty
   name, amount, and target repayment date, **Then** the entry is saved with status "Open" and a
   current balance equal to the original amount.
2. **Given** the debts ledger, **When** the user creates a new payable debt with a counterparty
   name, amount, and repayment rate, **Then** the entry is saved the same way, in the payable
   direction.
3. **Given** an existing debt entry, **When** the user edits its counterparty name, amount, or
   direction-specific field, **Then** the changes are saved and reflected immediately.
4. **Given** an existing debt entry, **When** the user deletes it, **Then** it no longer appears
   anywhere in the ledger.

---

### User Story 2 - View the Split Debts Ledger Screen (Priority: P2)

The user opens the Debts Ledger screen and sees two separate tables side by side: "חייבים לנו"
(who owes the family, with name, amount, target date, and notes) and "אנחנו חייבים" (who the family
owes, with name, amount, repayment rate, and current balance), so they can see the full picture of
mutual debts at a glance.

**Why this priority**: This is the user-facing payoff of tracking debts at all, but it can only
show something meaningful once User Story 1 supplies at least one debt entry in each direction.

**Independent Test**: Can be fully tested by creating a receivable and a payable debt and opening
the screen, verifying each appears in its correct table with the right columns.

**Acceptance Scenarios**:

1. **Given** one or more receivable debts exist, **When** the user opens the Debts Ledger screen,
   **Then** the "חייבים לנו" table lists each with counterparty name, current balance, target
   date, and notes.
2. **Given** one or more payable debts exist, **When** the user opens the Debts Ledger screen,
   **Then** the "אנחנו חייבים" table lists each with counterparty name, repayment rate, and current
   balance.
3. **Given** a debt whose status is Closed, **When** the user views its table, **Then** it is
   visibly distinguishable from open debts (e.g., a status indicator) rather than looking identical
   to an outstanding debt.

---

### User Story 3 - Record Repayments with Automatic Transaction Generation (Priority: P3)

The user records a repayment (full or partial) against an open debt. The debt's current balance
decreases by the paid amount, its status automatically becomes Closed once the balance reaches
zero, and the system automatically creates the matching transaction in the current month's
transaction ledger (feature 002) — an Income transaction for a receivable being collected, or an
outflow transaction for a payable being paid down — with zero manual double-entry. The Monthly
Overview screen's debt-repayments figure is fed by these payable-repayment transactions.

**Why this priority**: This refines and automates a mechanism that User Stories 1-2 already make
visible; it is the feature's core value proposition (avoiding manual double-entry) but the ledger
is already useful for tracking who-owes-whom without it.

**Independent Test**: Can be fully tested by recording a partial repayment against each direction
and verifying: the debt's balance decreases by exactly the paid amount, its status stays Open (or
becomes Closed only when the balance reaches zero), a matching transaction appears in that month's
transaction ledger for exactly the paid amount, and (for payables) the Monthly Overview's
debt-repayments figure reflects it.

**Acceptance Scenarios**:

1. **Given** an open receivable debt, **When** the user records a full repayment equal to its
   current balance, **Then** the balance becomes zero, the status becomes Closed, and an Income
   transaction for that amount is created in the current month, marked not tithe-applicable.
2. **Given** an open receivable or payable debt, **When** the user records a partial repayment
   smaller than its current balance, **Then** the balance decreases by exactly that amount, the
   status remains Open, and a transaction is created for exactly the paid amount (not the original
   or remaining amount).
3. **Given** an open payable debt, **When** the user records a repayment, **Then** an outflow
   transaction is created for that amount, and the Monthly Overview screen's debt-repayments
   summary for that month includes it.
4. **Given** a user attempts to record a repayment larger than a debt's current remaining balance,
   **When** they submit it, **Then** the system rejects it rather than allowing the balance to go
   negative.

---

### Edge Cases

- What happens when a repayment amount exactly equals the current remaining balance? → The debt's
  balance becomes exactly zero and its status automatically becomes Closed (FR-005, FR-009).
- What happens when a repayment amount is less than the current remaining balance (partial
  closure)? → Only the balance is reduced by the amount actually paid, and the generated
  transaction reflects only that paid amount — not the original or remaining debt amount.
- What happens when a repayment amount exceeds the current remaining balance? → The system MUST
  reject the repayment; a debt's balance MUST NOT be allowed to go negative.
- What happens to previously generated repayment transactions if the originating debt entry is
  later edited or deleted? → They are NOT retroactively modified or removed; the transaction
  ledger remains an accurate historical record independent of later edits to the debt entry.
- What happens when a user tries to record a repayment against an already-Closed debt? → The
  system MUST reject it, since a closed debt has no remaining balance to repay.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow creating a debt entry in either direction — Receivable (owed
  to the household) or Payable (owed by the household) — with a counterparty name, an original
  amount, and notes.
- **FR-002**: The system MUST allow a receivable debt to optionally record a target repayment
  date, and a payable debt to optionally record a repayment rate.
- **FR-003**: The system MUST allow editing a debt entry's counterparty name, amount fields,
  direction-specific field, and notes.
- **FR-004**: The system MUST allow deleting a debt entry.
- **FR-005**: The system MUST track, for each debt, a current remaining balance that starts equal
  to the original amount and decreases only when a repayment is recorded against it.
- **FR-006**: The system MUST track each debt's status (Open or Closed), automatically
  transitioning it to Closed the moment its current balance reaches exactly zero, and MUST NOT
  allow a repayment against an already-Closed debt.
- **FR-007**: The system MUST display the debts ledger as two separate tables: Receivables
  ("חייבים לנו" — counterparty name, current balance, target date, notes) and Payables ("אנחנו
  חייבים" — counterparty name, repayment rate, current balance), with each debt's status visibly
  distinguishable.
- **FR-008**: The system MUST allow recording a repayment against an open debt for any amount up
  to and including its current remaining balance, and MUST reject an amount that exceeds it.
- **FR-009**: The system MUST reduce a debt's current balance by exactly the amount recorded in a
  repayment — never by the original or remaining amount if a smaller partial payment was made.
- **FR-010**: When a repayment is recorded against a Receivable, the system MUST automatically
  create a corresponding Income-type transaction (feature 002) dated in the current month, for
  exactly the paid amount, marked as not tithe-applicable.
- **FR-011**: When a repayment is recorded against a Payable, the system MUST automatically create
  a corresponding outflow transaction (feature 002) dated in the current month, for exactly the
  paid amount, distinguishable from other expense transactions so it can be summed separately.
- **FR-012**: The system MUST expose the sum of the current month's payable-repayment transactions
  to the Monthly Overview screen (feature 002), replacing its previous always-zero placeholder for
  the debt-repayments summary line.
- **FR-013**: The system MUST NOT retroactively modify or remove a previously generated repayment
  transaction when the originating debt entry is later edited or deleted.

### Key Entities

- **Debt**: A single mutual-debt record. Key attributes: direction (Receivable or Payable),
  counterparty name, original amount, current remaining balance (derived from repayments), status
  (Open/Closed), an optional target repayment date (receivables) or repayment rate (payables), and
  notes.
- **Repayment**: An event recording money paid toward a debt — amount and the date/month it was
  recorded — which reduces the parent debt's current balance and generates a matching transaction
  in feature 002's transaction ledger.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can create a new debt entry in under 15 seconds.
- **SC-002**: Recording a repayment updates the debt's balance/status and the transaction ledger
  in a single action, with zero manual double-entry required from the user.
- **SC-003**: 100% of recorded repayments leave every debt's current balance at zero or greater —
  none ever go negative.
- **SC-004**: The Monthly Overview screen's debt-repayments figure always reflects the actual sum
  of that month's recorded payable repayments, with zero manual calculation by the user.

## Assumptions

- The direction-specific fields (target repayment date for receivables, repayment rate for
  payables) are informational/display-only for this feature — they do not drive any automatic
  reminder, scheduling, or calculation beyond being shown in their respective table (consistent
  with the source spec's screen description).
- Only one household/user context exists (no multi-user permissions model is required for this
  feature), consistent with features 001-003.
- Deleting a debt entry does not retroactively affect any transaction already generated from a
  prior repayment against it (FR-013) — the transaction ledger remains an append-only historical
  record independent of later edits to the debt itself.
- A repayment transaction generated for a receivable is always marked not tithe-applicable, since
  it represents the return of principal already owed to the household rather than new income.
- This feature extends feature 002's Transaction concept with the ability to distinguish
  payable-repayment transactions from other expense transactions (needed for FR-012); the specific
  mechanism is a planning-time detail, not specified here.
- The payment method for a generated transaction is provided by the user at the time they record
  the repayment, reusing feature 002's existing required payment-method field — no new assumption
  is needed beyond that.
