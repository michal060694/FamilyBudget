---

description: "Task list for Annual Budget & Smoothing Engine"
---

# Tasks: Annual Budget & Smoothing Engine

**Note (2026-07-14)**: All 30 tasks below were completed against the original Hebrew-calendar
design (`HebrewYearCycle`, `hebrewYear`/`hebrewMonth`, Tishrei/Elul terminology). The feature was
then switched to a Gregorian-calendar year cycle per explicit user direction — see spec.md's
Updated note, and the code now uses `CalendarYearCycle`/`Year`/`Month`/`TargetMonth` instead. This
file is left as the historical record of what was executed; it is not being rewritten
task-by-task.

**Input**: Design documents from `specs/001-budget-smoothing-engine/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included. Constitution Principle IV (Financial Data Integrity, NON-NEGOTIABLE)
requires automated tests proving the allocation/overrun invariants hold, so Core unit tests are
mandatory, not optional, for this feature.

**Organization**: Tasks are grouped by user story (spec.md priorities P1/P2/P3) to enable
independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root

## Path Conventions

Per plan.md: `src/FamilyBudget.Core/`, `src/FamilyBudget.Infrastructure/`, `src/FamilyBudget.Api/`,
`tests/FamilyBudget.Core.Tests/`, `tests/FamilyBudget.Api.Tests/`.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Solution and project scaffolding per plan.md's Clean Architecture structure

- [X] T001 Create `FamilyBudget.sln` and the five projects (`src/FamilyBudget.Core`,
      `src/FamilyBudget.Infrastructure`, `src/FamilyBudget.Api`,
      `tests/FamilyBudget.Core.Tests`, `tests/FamilyBudget.Api.Tests`) via `dotnet new` targeting
      .NET 8, then add them all to the solution
- [X] T002 Add project references: `FamilyBudget.Infrastructure` → `FamilyBudget.Core`;
      `FamilyBudget.Api` → `FamilyBudget.Core` and `FamilyBudget.Infrastructure`;
      `FamilyBudget.Core.Tests` → `FamilyBudget.Core`; `FamilyBudget.Api.Tests` → `FamilyBudget.Api`
- [X] T003 Add NuGet packages per research.md: `Microsoft.EntityFrameworkCore.Sqlite` and
      `Microsoft.EntityFrameworkCore.Design` to `src/FamilyBudget.Infrastructure`;
      `Microsoft.AspNetCore.Mvc.Testing` to `tests/FamilyBudget.Api.Tests`; xUnit packages
      (`xunit`, `xunit.runner.visualstudio`) to both test projects
- [X] T004 [P] Enable nullable reference types and treat warnings sensibly across all five
      `.csproj` files (`<Nullable>enable</Nullable>`)

**Checkpoint**: Solution builds (`dotnet build`) with all empty projects wired together

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, persistence, and DI wiring that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T005 Create `AnnualBudgetItem` entity in
      `src/FamilyBudget.Core/Entities/AnnualBudgetItem.cs` with fields and validation rules from
      data-model.md (`Id`, `HebrewYear`, `Name`, `TotalAmount`, `TargetHebrewMonth`,
      `AmountAlreadySetAside`)
- [X] T006 [P] Implement `HebrewYearCycle` service in
      `src/FamilyBudget.Core/Services/HebrewYearCycle.cs` wrapping
      `System.Globalization.HebrewCalendar` to expose: current Hebrew year/month, and months
      remaining between a given month and either a target month or Hebrew year-end (Elul), per
      research.md's decision
- [X] T007 [P] Define `IAnnualBudgetItemRepository` abstraction in
      `src/FamilyBudget.Core/Abstractions/IAnnualBudgetItemRepository.cs` (e.g.
      `GetByHebrewYearAsync(int year)`)
- [X] T008 Create `FamilyBudgetDbContext` in
      `src/FamilyBudget.Infrastructure/Persistence/FamilyBudgetDbContext.cs` with EF Core entity
      configuration for `AnnualBudgetItem` (depends on T005)
- [X] T009 Implement `AnnualBudgetItemRepository` in
      `src/FamilyBudget.Infrastructure/Repositories/AnnualBudgetItemRepository.cs` implementing
      `IAnnualBudgetItemRepository` against `FamilyBudgetDbContext` (depends on T007, T008)
- [X] T010 Create the initial EF Core migration (`InitialCreate`) for the `AnnualBudgetItem`
      table using `dotnet ef migrations add` against `src/FamilyBudget.Infrastructure` (depends
      on T008)
- [X] T011 Wire up dependency injection in `src/FamilyBudget.Api/Program.cs`: register
      `FamilyBudgetDbContext` with the SQLite connection string and register
      `IAnnualBudgetItemRepository` → `AnnualBudgetItemRepository` (depends on T008, T009)

**Checkpoint**: Foundation ready — `dotnet ef database update` succeeds and DI resolves the
repository; user story implementation can now begin

---

## Phase 3: User Story 1 - Monthly Proactive Dashboard (Priority: P1) 🎯 MVP

**Goal**: At the start of any Hebrew month, show projected income vs. fixed expenses, the dynamic
monthly smoothing-allocation total, and the resulting free/discretionary balance (FR-002, FR-003).

**Independent Test**: Seed one `AnnualBudgetItem` (e.g., Tishrei holidays) with a known
`TotalAmount`/`AmountAlreadySetAside`, call `GET /api/dashboard` for Menachem-Av, and verify
`allocatedMonthly` matches `(TotalAmount - AmountAlreadySetAside) / MonthsRemaining` and
`freeBalance` reconciles per contracts/api.md.

### Tests for User Story 1

- [X] T012 [P] [US1] Unit test for the core smoothing formula (Menachem-Av → Tishrei scenario
      from spec.md) in `tests/FamilyBudget.Core.Tests/BudgetSmoothingEngineTests.cs`
- [X] T013 [P] [US1] Integration test for `GET /api/dashboard` response shape and values in
      `tests/FamilyBudget.Api.Tests/DashboardEndpointTests.cs`

### Implementation for User Story 1

- [X] T014 [US1] Implement `BudgetSmoothingEngine.CalculateAllocation(item, currentHebrewMonth)`
      in `src/FamilyBudget.Core/Services/BudgetSmoothingEngine.cs`, using `HebrewYearCycle` for
      `MonthsRemaining` (depends on T005, T006)
- [X] T015 [P] [US1] Define `DashboardResponse` and `AllocationLine` DTOs in
      `src/FamilyBudget.Api/Contracts/DashboardContracts.cs` per contracts/api.md
- [X] T016 [US1] Implement `GET /api/dashboard` minimal API endpoint in
      `src/FamilyBudget.Api/Endpoints/DashboardEndpoints.cs`, computing `totalRequiredAllocation`
      and `freeBalance` (depends on T009, T014, T015)
- [X] T017 [US1] Add `400` validation for `hebrewMonth` outside 1-13 in
      `src/FamilyBudget.Api/Endpoints/DashboardEndpoints.cs` (depends on T016)

**Checkpoint**: User Story 1 is fully functional and independently testable — this is the MVP

---

## Phase 4: User Story 2 - Annual Budget & Year-Cycle Table (Priority: P2)

**Goal**: Show every annual budget item for the current Hebrew year, ordered by Hebrew month
(Tishrei-first), with general items last, each showing target vs. set-aside amounts (FR-001,
FR-004).

**Independent Test**: Seed a mix of month-mapped and general items, call
`GET /api/annual-budget`, and verify ordering and fully-funded flagging per contracts/api.md.

### Tests for User Story 2

- [X] T018 [P] [US2] Unit test for ordering logic (Tishrei-first, general items last) in
      `tests/FamilyBudget.Core.Tests/AnnualBudgetOrderingTests.cs`
- [X] T019 [P] [US2] Integration test for `GET /api/annual-budget` ordering and `isFullyFunded`
      flag in `tests/FamilyBudget.Api.Tests/AnnualBudgetEndpointTests.cs`

### Implementation for User Story 2

- [X] T020 [US2] Implement `GetOrderedForYear(hebrewYear)` query logic (Tishrei-first ordering,
      general items last, `isFullyFunded` derivation) in
      `src/FamilyBudget.Core/Services/AnnualBudgetQueryService.cs` (depends on T005, T007)
- [X] T021 [P] [US2] Define `AnnualBudgetResponse` and item DTOs in
      `src/FamilyBudget.Api/Contracts/AnnualBudgetContracts.cs` per contracts/api.md
- [X] T022 [US2] Implement `GET /api/annual-budget` minimal API endpoint in
      `src/FamilyBudget.Api/Endpoints/AnnualBudgetEndpoints.cs` (depends on T009, T020, T021)

**Checkpoint**: User Stories 1 AND 2 both work independently

---

## Phase 5: User Story 3 - Budget Overrun Becomes Internal Debt (Priority: P3)

**Goal**: When an item's actual usage exceeds its defined target, spread the excess across
remaining months so the deficit is zeroed out by Hebrew year-end (FR-006); treat an underfunded
item whose target month has passed the same way (FR-007).

**Independent Test**: Set an item's `AmountAlreadySetAside` implying usage beyond `TotalAmount`
(negative `RemainingBalance`), recompute for the next Hebrew month, and verify the excess is
folded into the remaining months' allocation per the Core unit tests.

### Tests for User Story 3

- [X] T023 [P] [US3] Unit test: overrun item's excess is spread across remaining months to reach
      zero by Hebrew year-end (FR-006) in
      `tests/FamilyBudget.Core.Tests/BudgetOverrunTests.cs`
- [X] T024 [P] [US3] Unit test: item still underfunded after its target month has passed is
      treated as an overrun and spread across remaining months (FR-007) in
      `tests/FamilyBudget.Core.Tests/BudgetOverrunTests.cs`
- [X] T025 [P] [US3] Unit test: edge cases — fully-funded item yields `AllocatedMonthly = 0`; a
      single remaining month absorbs the full remaining balance — in
      `tests/FamilyBudget.Core.Tests/BudgetSmoothingEdgeCaseTests.cs`

### Implementation for User Story 3

- [X] T026 [US3] Extend `BudgetSmoothingEngine.CalculateAllocation` to handle negative
      `RemainingBalance` (overrun) and passed-target-month shortfall by treating both as spread
      across the remaining months in `src/FamilyBudget.Core/Services/BudgetSmoothingEngine.cs`
      (depends on T014) — **no code change needed**: T014's `RemainingToDeposit + Overrun` split
      (data-model.md) combined with `HebrewYearCycle.GetMonthsRemaining`'s year-end fallback
      already generalizes to both cases; T023/T024 confirm this against the existing
      implementation.
- [X] T027 [US3] Handle the fully-funded-early and single-month-remaining edge cases in the same
      method in `src/FamilyBudget.Core/Services/BudgetSmoothingEngine.cs` (depends on T026) —
      **no code change needed**, same reasoning; confirmed by T025.

**Checkpoint**: All three user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Hardening and validation across all three stories

- [X] T028 [P] Unit tests for `HebrewYearCycle` leap-year month-count edge cases (13-month years)
      in `tests/FamilyBudget.Core.Tests/HebrewYearCycleTests.cs`
- [X] T029 Run `quickstart.md` end-to-end validation manually against a running
      `src/FamilyBudget.Api` instance — verified: dashboard/annual-budget endpoints return
      correct allocations, ordering, and 400 validation against a real seeded SQLite database
- [X] T030 Run the full test suite (`dotnet test`) and confirm all Constitution Principle IV
      invariant tests (T012, T023, T024, T025) pass — 18/18 tests passing

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — start immediately
- **Foundational (Phase 2)**: Depends on Setup — BLOCKS all user stories
- **User Stories (Phase 3-5)**: All depend on Foundational completion; independently
  implementable/testable in priority order (P1 → P2 → P3) or in parallel if staffed
- **Polish (Phase 6)**: Depends on all desired user stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependency on US2/US3 — this is the MVP
- **User Story 2 (P2)**: Independent of US1 at the code level (separate query/endpoint); shares
  the `AnnualBudgetItem` entity and repository from Foundational
- **User Story 3 (P3)**: Extends `BudgetSmoothingEngine` from US1 (T014) in place — implement
  after US1 to avoid rework, though its tests are independently runnable

### Within Each User Story

- Tests are written before implementation and MUST fail first
- Core services before API endpoints
- Story complete and checkpoint-validated before moving to the next priority

### Parallel Opportunities

- T003, T004 in Setup can run in parallel
- T006, T007 in Foundational can run in parallel (different files)
- T012/T013 (US1 tests) can run in parallel; T015 (DTOs) can run in parallel with T014 (engine)
- T018/T019 (US2 tests) can run in parallel with all of US1 once Foundational is done
- T023/T024/T025 (US3 tests) can run in parallel with each other

---

## Parallel Example: User Story 1

```bash
# Launch both US1 tests together:
Task: "Unit test for smoothing formula in tests/FamilyBudget.Core.Tests/BudgetSmoothingEngineTests.cs"
Task: "Integration test for GET /api/dashboard in tests/FamilyBudget.Api.Tests/DashboardEndpointTests.cs"

# DTOs can be authored while the engine is being implemented:
Task: "Define DashboardResponse/AllocationLine DTOs in src/FamilyBudget.Api/Contracts/DashboardContracts.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run T012/T013, confirm dashboard reconciles per contracts/api.md
5. Demo the dashboard against seeded data

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → validate independently → MVP demo
3. User Story 2 → validate independently → demo annual table
4. User Story 3 → validate independently → demo overrun/shortfall handling
5. Polish phase → full regression pass

---

## Notes

- [P] tasks touch different files with no unmet dependencies
- Every task listed above maps to a specific file path from plan.md's project structure
- Commit after each task or logical group
- Constitution Principle IV makes T012, T023, T024, T025 non-negotiable — do not skip them even
  if time-constrained
