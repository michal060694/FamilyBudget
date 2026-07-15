# Feature Specification: Funds Management & Summary

**Feature Branch**: `003-funds-management`

**Created**: 2026-07-15

**Status**: Draft

**Input**: User description: "Funds Management & Summary: a system that lets the user track their
investment/savings accounts ('funds', e.g. Meitav, IBI) and see, for each fund, its total current
balance alongside a breakdown of how that balance is earmarked across purposes (e.g., '10,000 for
a wig/peah', '5,000 for annual 2026 expenses', 'general savings'). The user can add/edit/delete
funds (name, current total balance) and, within each fund, add/edit/delete named earmark lines
(purpose label, amount). Business rule (Constitution Principle IV, Fund balance integrity): the
sum of all earmarked amounts within a given fund must always exactly equal that fund's recorded
total balance — the system must make this reconcile automatically or clearly flag any mismatch,
never let the two silently drift apart. This is surfaced as a new tab/screen in the existing
FamilyBudget client (alongside the existing 'Annual Budget' and 'Monthly Overview' tabs), showing
a table or card per fund with its balance and the list of earmarked purposes summing to it. Out of
scope: this feature does not integrate with the Annual Budget Item's 'amount already set aside'
figure or the Monthly Overview screen automatically — it is a standalone view of where money
currently sits and what it's earmarked for, not an automatic reconciliation against annual budget
items or transactions (that connection can be a future enhancement)."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Define Funds & Their Balances (Priority: P1)

The user creates a named fund (e.g., "מיטב", "IBI") with its current total balance, and can edit
the name or update the balance later, or delete a fund entirely.

**Why this priority**: Every other capability in this feature (viewing the summary, earmarking
purposes) depends on at least one fund existing. Without this, there is no real data feeding the
system — it is the foundational slice.

**Independent Test**: Can be fully tested by creating a fund with a name and balance, editing its
name and balance, and deleting it, verifying each change is persisted and reflected immediately.

**Acceptance Scenarios**:

1. **Given** the funds screen, **When** the user creates a new fund with a name and a total
   balance, **Then** the fund is saved and appears in the list of funds with that name and balance.
2. **Given** an existing fund, **When** the user updates its recorded balance (e.g., after a
   statement update), **Then** the new balance is saved and reflected immediately.
3. **Given** an existing fund, **When** the user deletes it, **Then** the fund and all of its
   earmark lines are removed and no longer appear anywhere in the feature.

---

### User Story 2 - View Fund Summary Screen (Priority: P2)

The user opens a dedicated "Funds" tab and sees every fund listed with its total balance and,
underneath, the breakdown of purposes its balance is earmarked for — matching the mental model of
"מיטב: 15,000 ₪ — מתוכם 10,000 ₪ לפאה, ו-5,000 ₪ לשנתי 26".

**Why this priority**: This is the user-facing payoff of tracking funds at all — knowing at a
glance where money currently sits and what it's set aside for — but it can only show something
meaningful once User Story 1 supplies at least one fund.

**Independent Test**: Can be fully tested by creating a couple of funds (with or without earmark
lines yet) and opening the new tab, verifying every fund appears with its name, balance, and
whatever earmark lines exist (or an empty/fully-unearmarked state if none do).

**Acceptance Scenarios**:

1. **Given** one or more funds exist, **When** the user opens the Funds tab, **Then** every fund is
   shown as its own table or card with its name and total balance clearly visible.
2. **Given** a fund with earmark lines, **When** the user views that fund's card, **Then** every
   earmark line's purpose label and amount is listed underneath the fund's balance.
3. **Given** a fund with no earmark lines yet, **When** the user views that fund's card, **Then**
   the full balance is shown as not-yet-earmarked rather than the screen erroring or omitting the
   fund.

---

### User Story 3 - Manage Earmarked Purposes & Balance Reconciliation (Priority: P3)

Within a fund, the user adds, edits, and deletes named earmark lines (e.g., "פאה", "שנתי 26",
"חיסכון כללי"), and the system continuously shows whether the sum of those earmarks matches the
fund's recorded balance, clearly flagging any mismatch rather than letting the two figures quietly
disagree.

**Why this priority**: This refines and makes trustworthy a mechanism that User Story 2 already
displays; it is essential for the feature's core promise (Constitution Principle IV, Fund balance
integrity) but the summary screen already shows useful information without it.

**Independent Test**: Can be fully tested by adding earmark lines to a fund whose total does not
yet match the balance, verifying the mismatch is clearly indicated, then adjusting the lines (or
the balance) until they reconcile and verifying the mismatch indicator clears.

**Acceptance Scenarios**:

1. **Given** a fund with a recorded balance, **When** the user adds earmark lines whose amounts sum
   to exactly that balance, **Then** the fund is shown as fully and correctly earmarked with no
   mismatch indicated.
2. **Given** a fund whose earmark lines sum to less than its recorded balance, **When** the user
   views that fund, **Then** the system clearly shows the unearmarked remainder (balance minus
   earmarked sum).
3. **Given** a fund whose earmark lines sum to more than its recorded balance, **When** the user
   views that fund, **Then** the system clearly flags the overage as a mismatch rather than
   silently accepting it.
4. **Given** a flagged mismatch, **When** the user edits an earmark line or the fund's balance so
   the sum reconciles exactly, **Then** the mismatch indicator disappears immediately.

---

### Edge Cases

- What happens when a fund's balance is updated (e.g., market value changed) after its earmark
  lines were already reconciled? → The system MUST immediately recompute and, if the sum no longer
  matches, flag the new mismatch — it MUST NOT silently adjust any earmark line to compensate.
- What happens when the last earmark line in a fund is deleted? → The fund's full balance becomes
  the unearmarked remainder; this MUST be shown as an informational figure, not necessarily an
  error state, since a fund can legitimately have no earmarks defined yet.
- What happens when a user tries to save an earmark line with a zero or negative amount? → The
  system MUST reject it; earmark amounts MUST be strictly positive.
- What happens when a user tries to save a fund with a negative balance? → The system MUST reject
  it; a fund's recorded balance MUST be zero or greater.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow the user to create a fund with a name and a current total
  balance.
- **FR-002**: The system MUST allow the user to rename a fund and/or update its recorded total
  balance at any time.
- **FR-003**: The system MUST allow the user to delete a fund, which also removes all of that
  fund's earmark lines.
- **FR-004**: The system MUST display every fund's name and current total balance.
- **FR-005**: The system MUST allow the user to add a named earmark line within a fund, consisting
  of a purpose label and an amount.
- **FR-006**: The system MUST allow the user to edit an earmark line's purpose label and/or amount.
- **FR-007**: The system MUST allow the user to delete an earmark line.
- **FR-008**: The system MUST display, for each fund, the full list of its earmark lines with
  their purpose labels and amounts.
- **FR-009**: The system MUST compute, for each fund, the sum of all its earmark line amounts and
  compare it against the fund's recorded total balance.
- **FR-010**: The system MUST clearly flag when a fund's earmarked sum does not exactly equal its
  recorded balance (Constitution Principle IV) — the two figures MUST NOT be allowed to silently
  drift apart unnoticed.
- **FR-011**: The system MUST NOT block saving a fund or earmark line purely because it currently
  creates a mismatch; reconciliation is enforced through clear, immediate visibility (FR-010,
  FR-012), not by refusing the edit.
- **FR-012**: The system MUST display, per fund, the unearmarked/mismatch amount (balance minus
  earmarked sum) so the user always sees exactly how much needs to be earmarked, or how much
  overage needs to be resolved, to reconcile.
- **FR-013**: The system MUST expose the funds summary as its own tab/screen in the existing
  client, alongside the existing Annual Budget and Monthly Overview tabs.

### Key Entities

- **Fund**: A named investment/savings account (e.g., "מיטב", "IBI"). Key attributes: name,
  current total balance (manually entered/updated by the user, not derived from an external feed).
- **FundEarmark**: A named purpose-allocation line within a fund (e.g., "פאה", "שנתי 26", "חיסכון
  כללי"). Key attributes: purpose label, amount, and a reference to the fund it belongs to.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can view every fund's balance and earmark breakdown within 10 seconds of
  opening the Funds tab, without navigating anywhere else.
- **SC-002**: For every fund, the system always shows whether the earmarked sum matches the
  recorded balance, with zero manual calculation by the user.
- **SC-003**: A user can create a new fund or a new earmark line in under 15 seconds.
- **SC-004**: 100% of displayed fund mismatch/unearmarked figures are recalculated and accurate
  immediately after any balance or earmark-line edit.

## Assumptions

- A fund's total balance is a manually entered/updated figure (e.g., copied from a brokerage
  statement) — this feature does not integrate with any external bank/investment data feed.
- Only one household/user context exists (no multi-user permissions model is required for this
  feature), consistent with features 001 and 002.
- No historical tracking of balance changes over time is required for this feature — only the
  current balance and current earmark breakdown are shown (a historical/audit view could be a
  future enhancement).
- This feature deliberately does not automatically reconcile against `AnnualBudgetItem`'s "amount
  already set aside" figure (feature 001) or against Monthly Overview transactions (feature 002);
  it is a standalone view of current fund state. Connecting these is explicitly out of scope and
  left for a future enhancement.
- A single household-wide currency is assumed throughout (as in the rest of the application); no
  multi-currency support is required.
