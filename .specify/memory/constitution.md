<!--
Sync Impact Report
- Version change: 1.0.0 → 1.1.0
- Modified principles: I. Proactive Budget Smoothing (removed Hebrew-calendar-specific framing
  and worked examples; now calendar-agnostic per explicit user direction to use the Gregorian
  calendar for the annual budget cycle)
- Added sections: none
- Removed sections: none
- Modified sections: Domain Formulas & Additional Constraints (MonthsRemaining is now computed
  relative to the end of the Gregorian calendar year / December, not the Hebrew year / Elul)
- Templates requiring updates:
  - .specify/templates/plan-template.md ✅ no calendar-specific references to sync
  - .specify/templates/spec-template.md ✅ no calendar-specific references to sync
  - .specify/templates/tasks-template.md ✅ no calendar-specific references to sync
  - .claude/skills/speckit-*/SKILL.md ✅ no agent-specific renames required
- Follow-up TODOs: none

Prior report (v1.0.0, superseded):
- Version change: [TEMPLATE] → 1.0.0 (initial ratification)
- Modified principles: N/A (first concrete version, replacing template placeholders)
- Added sections: I. Proactive Budget Smoothing; II. Clean Layered Architecture; III. Fixed
  Technology Stack; IV. Financial Data Integrity; V. Auditability & Transparency; Domain
  Formulas (Additional Constraints); Development Workflow; Governance
-->

# FamilyBudget Constitution

## Core Principles

### I. Proactive Budget Smoothing (Core Mission)

The system's central purpose is preventing uneven cash flow across the months of the calendar
year. Every annual expense item MUST be met by a dynamic monthly allocation into a savings fund,
computed and re-checked at the start of every calendar month, so that predictable heavy-expense
months (e.g., December holidays, back-to-school clothing, annual insurance renewals) never
require an unplanned cash burden on that month alone. Any feature, shortcut, or optimization
that could cause a future month to become unexpectedly overloaded again is a constitution
violation and MUST be rejected or redesigned, even if it simplifies the implementation.

### II. Clean Layered Architecture (NON-NEGOTIABLE)

The solution MUST be split into strictly separated projects: **Core** (domain entities, budget
and tithe calculation logic, invariants), **Infrastructure** (EF Core, SQLite persistence,
external integrations such as Excel import), and **API** (HTTP endpoints, request/response
models). Dependencies MUST point inward only — Core has zero references to Infrastructure or
API. Business/domain logic (budget math, tithe engine, fund invariants) MUST be independently
unit-testable without a database, an HTTP host, or any UI in scope.

### III. Fixed Technology Stack

The project MUST be implemented in C# on .NET 8. The database MUST be SQLite, accessed
exclusively through Entity Framework Core (no raw ADO.NET or alternate ORMs). Source control
MUST live in a dedicated, organized GitHub repository. These choices are fixed for the
lifetime of this project and MUST NOT be changed without a MAJOR constitution amendment.

### IV. Financial Data Integrity (NON-NEGOTIABLE)

The following domain invariants MUST always hold and MUST be covered by automated tests before
any related feature is considered done:

- **Fund balance integrity**: the sum of all amounts earmarked to purposes within a given fund
  (e.g., Meitav, IBI) MUST exactly equal that fund's current recorded balance at all times.
- **Protected tithe calculation**: the tithe (chomesh/maaser) amount due MUST be calculated only
  after netting out (a) predefined fixed recurring donations for the month and (b) prior-month
  small charity expenses not yet offset (`TitheOffset`).
- **Configurable tithe rate**: the tithe rate is a stored, user-configurable setting (e.g., 0.1
  for maaser or 0.2 for chomesh) — it MUST NOT be hardcoded as a constant anywhere in the
  domain logic.

Any change touching fund allocation or tithe logic MUST include a test that proves these
invariants still hold after the change.

### V. Auditability & Transparency

Every number shown to the user MUST be traceable to its components rather than presented as a
single opaque aggregate. At minimum, the monthly dashboard MUST separately show: projected
income vs. fixed expenses, the dynamic monthly fund allocation, the tithe due after offsets, and
the resulting free/discretionary balance. If a calculation cannot be broken down and explained
to the user in plain terms, it MUST NOT ship as a single black-box figure.

## Domain Formulas & Additional Constraints

- **Monthly dynamic allocation formula**:
  `AllocatedMonthly = (TotalAmount - AmountAlreadySetAside) / MonthsRemaining`
  - `MonthsRemaining` is computed relative to the end of the Gregorian calendar year (December).
  - `AmountAlreadySetAside` is the amount already deposited/reserved toward that specific budget
    item — **not** the amount already spent. This distinction is load-bearing for Principle I
    and MUST be preserved in all implementations and refactors of the allocation engine.
- **Budget overrun handling**: when an annual budget item is exceeded, the excess MUST become an
  internal debt of the fund and MUST be added dynamically to the allocation requirement of the
  remaining months, so the deficit is zeroed out before the end of the budget year.

## Development Workflow

- New capabilities are delivered as separate Spec-Kit features (spec → clarify → plan → tasks →
  implement), one cohesive capability at a time, rather than one monolithic specification.
- Any feature whose plan touches Principle IV invariants (fund balance, tithe calculation) MUST
  list the specific automated test(s) that verify the invariant as part of its task breakdown
  before implementation is considered complete.
- Excel-based historical import (or any future integration) is Infrastructure-layer code and
  MUST NOT leak spreadsheet-specific concerns into Core.

## Governance

This constitution supersedes all other project practices, templates, and ad-hoc conventions.
All specs, plans, and task lists MUST be checked against these principles at the "Constitution
Check" gate defined in the planning template; any violation MUST be either resolved or
explicitly justified in that plan's Complexity Tracking table.

**Amendment procedure**: amendments are proposed by editing this file, must state the reason for
the change, and require the project owner's explicit approval before being ratified. Versioning
follows semantic versioning:
- **MAJOR**: backward-incompatible principle removals or redefinitions (e.g., changing the fixed
  technology stack, removing a NON-NEGOTIABLE invariant).
- **MINOR**: new principle or materially expanded guidance added.
- **PATCH**: wording clarifications and non-semantic refinements.

Compliance is reviewed at every `/speckit-plan` invocation via the Constitution Check gate, and
may be re-verified at `/speckit-analyze` time for cross-artifact consistency.

**Version**: 1.1.0 | **Ratified**: 2026-07-13 | **Last Amended**: 2026-07-14
