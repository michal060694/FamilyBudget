# Feature Specification: Annual Budget & Smoothing Engine

**Feature Branch**: `001-budget-smoothing-engine`

**Created**: 2026-07-13

**Updated**: 2026-07-14 — switched from Hebrew-calendar-year cycles to Gregorian-calendar-year
cycles per explicit user direction (superseding the original Hebrew-calendar framing).

**Status**: Draft

**Input**: User description: "Annual Budget & Smoothing Engine: a system that manages
calendar-year annual budget items and proactively smooths cash flow by allocating money into
savings funds every month ahead of predictable heavy-expense months. ... AllocatedMonthly =
(TotalAmount - AmountAlreadySetAside) / MonthsRemaining ..."

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Monthly Proactive Dashboard (Priority: P1)

At the start of every calendar month, the user opens the dashboard and immediately sees whether
the month is under control: projected income vs. fixed expenses, how much must be set aside this
month toward future annual expenses (the dynamic smoothing allocation), and what is left over as
free/discretionary balance.

**Why this priority**: This is the single screen that delivers the system's core promise —
proactive visibility that prevents surprise heavy-expense months. Without it, none of the
underlying calculation logic has any user-facing value. It is the minimum viable slice.

**Independent Test**: Can be fully tested by loading the dashboard for a given calendar month with
a set of annual budget items already defined, and verifying the displayed allocation figure
matches the smoothing formula and the free balance reconciles income, fixed expenses, and the
allocation line.

**Acceptance Scenarios**:

1. **Given** an annual budget item "December Holidays" with a total defined amount and an amount
   already set aside, **When** the user opens the dashboard in the month of October, **Then**
   the dashboard shows a monthly allocation line equal to the remaining amount divided by the
   number of calendar months remaining until the item's target month (or year-end, whichever
   applies), and this line is shown separately from fixed expenses and projected income.
2. **Given** projected income and fixed expenses for the current calendar month, **When** the
   dashboard is displayed, **Then** the free/discretionary balance shown equals projected income
   minus fixed expenses minus the total required monthly allocation across all annual budget
   items.

---

### User Story 2 - Annual Budget & Year-Cycle Table (Priority: P2)

The user opens a dedicated screen showing every annual budget item for the current calendar year,
organized by calendar month (January, February, ...) followed by general/month-independent items,
each showing its defined target amount against the amount already set aside/used so far.

**Why this priority**: This is where the user defines and reviews the annual budget items that
feed the dashboard's smoothing calculation (Story 1). It is needed to configure the system but is
secondary to seeing the proactive result.

**Independent Test**: Can be fully tested by creating several annual budget items (some mapped to
calendar months, some general) and verifying the table lists them in the correct month order with
general items last.

**Acceptance Scenarios**:

1. **Given** a mix of month-mapped items (e.g., April, December) and general items (e.g., car
   test, insurance), **When** the user opens the annual budget screen, **Then** items are listed
   ordered by calendar month starting from January, with general items appearing after all
   month-mapped items.
2. **Given** an annual budget item whose amount already set aside equals its total defined
   amount, **When** the user views the table, **Then** the item is visually distinguishable as
   fully funded and no longer contributes to the monthly allocation requirement.

---

### User Story 3 - Budget Overrun Becomes Internal Debt (Priority: P3)

When an annual budget item's actual usage exceeds its defined target amount, the user sees the
excess automatically treated as an internal debt that is spread across the remaining months so
the shortfall is resolved before the calendar year ends.

**Why this priority**: This is an important correctness/edge-case behavior of the smoothing
engine, but it only matters once Stories 1 and 2 are in place and a real overrun occurs — it
refines the core behavior rather than delivering a new independent capability.

**Independent Test**: Can be fully tested by setting an annual budget item's used amount above
its defined total, then verifying the monthly allocation requirement for subsequent months
increases by the excess divided across the remaining months.

**Acceptance Scenarios**:

1. **Given** an annual budget item that has been used beyond its defined total amount, **When**
   the system recalculates the monthly allocation at the start of the next calendar month, **Then**
   the excess amount is added to that item's remaining balance and spread across the remaining
   months so the deficit reaches zero by calendar year-end.

---

### Edge Cases

- What happens when an annual budget item's target month has already passed and it is not yet
  fully funded? (e.g., the smoothing engine is being set up mid-year, after the target month) →
  The system MUST treat the shortfall the same way as a budget overrun (Story 3): the outstanding
  amount becomes an internal debt spread across the remaining months to zero it out before
  year-end.
- What happens when only one calendar month remains before year-end (December) and an item still
  has an unfunded balance? → The full remaining balance MUST be allocated to that single month.
- What happens when an annual budget item is fully funded early (amount already set aside equals
  or exceeds the total defined amount)? → Its required monthly allocation MUST be zero for the
  rest of the year.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow defining annual budget items for a calendar year, each with a
  total target amount and either a specific calendar month (1-12) or a "general"
  (month-independent) classification.
- **FR-002**: The system MUST compute, at the start of every calendar month and on demand, a
  required monthly allocation for each active annual budget item using:
  `AllocatedMonthly = (TotalAmount - AmountAlreadySetAside) / MonthsRemaining`, where
  `AmountAlreadySetAside` is the amount already deposited/reserved toward that item (not the
  amount spent), and `MonthsRemaining` is the number of calendar months from the current month to
  the end of the calendar year (December), or to the item's target month if that comes first.
- **FR-003**: The system MUST display a monthly dashboard showing, as separate line items:
  projected income, fixed expenses, the total required monthly smoothing allocation, and the
  resulting free/discretionary balance.
- **FR-004**: The system MUST display an annual budget table listing all budget items for the
  current calendar year, ordered by calendar month starting from January, with general
  (month-independent) items listed after all month-mapped items, each row showing the target
  amount and the amount already set aside/used.
- **FR-005**: The system MUST recompute the monthly allocation requirement whenever the amount
  already set aside for an item changes or a new calendar month begins.
- **FR-006**: When an annual budget item's actual usage exceeds its defined total amount, the
  system MUST record the excess as an internal debt against that item and add it to the
  allocation requirement of the remaining months so the deficit is eliminated by calendar
  year-end.
- **FR-007**: The system MUST treat an item that is still underfunded after its target month has
  passed the same way as an overrun (FR-006): the shortfall becomes an internal debt spread across
  the remaining months of the year.
- **FR-008**: The system MUST produce allocation figures that are consistent with fund-balance
  totals so that, once fund accounts exist, the sum of amounts earmarked within a fund always
  equals that fund's recorded balance (Constitution Principle IV).

### Key Entities

- **AnnualBudgetItem**: Represents a single planned annual expense for a calendar year. Key
  attributes: name, total target amount, target calendar month 1-12 (or "general"/month-independent
  flag), amount already set aside toward it.
- **CalendarYearCycle**: Represents the current calendar year's month sequence (January through
  December) used to compute months remaining for any given item and current month.
- **MonthlyAllocationSnapshot**: The computed result, per calendar month, of required allocations
  across all annual budget items — feeds the dashboard's smoothing-allocation line.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can determine, within 10 seconds of opening the dashboard, exactly how much
  must be set aside this month to stay on track for every upcoming annual expense.
- **SC-002**: For any defined annual budget item, the system's computed monthly allocation always
  results in the item being fully funded by its target month (or calendar year-end for general
  items), with zero manual recalculation by the user.
- **SC-003**: When an annual budget item overruns its target amount, the resulting shortfall is
  fully absorbed into the remaining months' allocations with no month requiring the user to
  manually adjust figures.
- **SC-004**: The annual budget table correctly reflects month ordering (January-first, general
  items last) for 100% of configured items, verified across a full calendar year cycle.

## Assumptions

- A standard Gregorian calendar month sequence (January through December, always 12 months) is
  used as the reference calendar for this feature; there is no leap-year month-count variability
  to account for (Gregorian leap years only affect the day count in February, not the number or
  ordering of months).
- Actual transaction recording/tagging, fund account balances, and tithe/donation calculations
  are out of scope for this feature and are covered by separate features; this feature assumes
  "amount already set aside" and "projected income/fixed expenses" are available as inputs.
- Only one household/user context exists (no multi-user permissions model is required for this
  feature).
- "General/month-independent" items are treated as due by calendar year-end (December) for the
  purpose of computing months remaining, unless a specific due month is later introduced.
