---

description: "Task list for Debts Ledger (Debts & Loans)"
---

# Tasks: Debts Ledger (Debts & Loans)

**Input**: Design documents from `specs/004-debts-ledger/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included. Constitution Principle IV (Financial Data Integrity, NON-NEGOTIABLE) requires
automated tests proving a debt's balance never goes negative and its generated transaction always
reflects exactly the amount paid, so Core unit tests are mandatory, not optional, for this feature.

**Organization**: Tasks are grouped by user story (spec.md priorities P1/P2/P3) to enable
independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root

## Path Conventions

Per plan.md: reuses the existing `src/FamilyBudget.Core/`, `src/FamilyBudget.Infrastructure/`,
`src/FamilyBudget.Api/`, `tests/FamilyBudget.Core.Tests/`, `tests/FamilyBudget.Api.Tests/`
projects from features 001-003 — no new projects are created.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the existing solution is ready to receive this feature's new entity/endpoints.

- [X] T001 Confirm `dotnet build` succeeds on the current solution before adding any new code
      (baseline check); no new NuGet packages are required

**Checkpoint**: Solution builds; ready to add feature 004 code

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, persistence, and DI wiring that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 Create `Debt` entity with `DebtDirection`/`DebtStatus` enums in
      `src/FamilyBudget.Core/Entities/Debt.cs` per data-model.md (`Id`, `Direction`,
      `CounterpartyName`, `OriginalAmount`, `CurrentBalance`, `Status`, `TargetDate`,
      `RepaymentRate`, `Notes`), with `RecordRepayment(amount)` (rejects if closed, amount <= 0, or
      amount > balance) and `Update(...)` (preserves amount already paid, per research.md)
- [X] T003 [P] Add `TransactionType.DebtRepayment` to the enum in
      `src/FamilyBudget.Core/Entities/Transaction.cs` (research.md) — additive, no existing
      behavior changes
- [X] T004 [P] Define `IDebtRepository` abstraction in
      `src/FamilyBudget.Core/Abstractions/IDebtRepository.cs` (`GetAllAsync`, `GetByIdAsync`,
      `AddAsync`, `DeleteAsync`, `SaveChangesAsync`)
- [X] T005 Extend `FamilyBudgetDbContext` in
      `src/FamilyBudget.Infrastructure/Persistence/FamilyBudgetDbContext.cs` with a `Debts`
      `DbSet` and EF Core configuration (enum-to-string conversions for `Direction`/`Status`)
      (depends on T002)
- [X] T006 Implement `DebtRepository` in
      `src/FamilyBudget.Infrastructure/Repositories/DebtRepository.cs` implementing
      `IDebtRepository` (depends on T004, T005)
- [X] T007 Create an additive EF Core migration (`AddDebts`) using `dotnet ef migrations add`
      against `src/FamilyBudget.Infrastructure` (depends on T005)
- [X] T008 Wire up dependency injection in `src/FamilyBudget.Api/Program.cs`: register
      `IDebtRepository` → `DebtRepository` (depends on T006)

**Checkpoint**: Foundation ready — `dotnet ef database update` succeeds and DI resolves the new
repository; user story implementation can now begin

---

## Phase 3: User Story 1 - Record & Manage Debts in Both Directions (Priority: P1) 🎯 MVP

**Goal**: Let the user create, edit, and delete debt entries in either direction (FR-001-FR-004).

**Independent Test**: Create a receivable and a payable debt via `POST /api/debts`, edit one via
`PUT`, delete the other via `DELETE`, verifying each change persists.

### Tests for User Story 1

- [X] T009 [P] [US1] Unit tests for `Debt` validation (name required, amount > 0, repayment rate >
      0 when present) in `tests/FamilyBudget.Core.Tests/DebtTests.cs`
- [X] T010 [P] [US1] Integration tests for `POST`/`PUT`/`DELETE /api/debts` (including `400`/`404`
      cases from contracts/api.md) in `tests/FamilyBudget.Api.Tests/DebtEndpointsTests.cs`

### Implementation for User Story 1

- [X] T011 [P] [US1] Define `DebtResponse`/`CreateDebtRequest`/`UpdateDebtRequest` DTOs in
      `src/FamilyBudget.Api/Contracts/DebtContracts.cs` per contracts/api.md
- [X] T012 [US1] Implement `POST /api/debts` in `src/FamilyBudget.Api/Endpoints/DebtEndpoints.cs`
      with `400` validation (depends on T006, T011)
- [X] T013 [US1] Implement `PUT /api/debts/{id}` in
      `src/FamilyBudget.Api/Endpoints/DebtEndpoints.cs` (depends on T012)
- [X] T014 [US1] Implement `DELETE /api/debts/{id}` in
      `src/FamilyBudget.Api/Endpoints/DebtEndpoints.cs` (depends on T012)
- [X] T015 [US1] Register the debt endpoints in `src/FamilyBudget.Api/Program.cs` (depends on
      T012, T013, T014)

**Checkpoint**: User Story 1 is fully functional and independently testable — this is the MVP

---

## Phase 4: User Story 2 - View the Split Debts Ledger Screen (Priority: P2)

**Goal**: Show every debt split into Receivables/Payables tables on a dedicated "ספר חובות" tab
(FR-007).

**Independent Test**: Create a receivable and a payable debt, call `GET /api/debts`, and verify
each appears in its correct list with the right fields; open the new client tab and verify the
same data renders.

### Tests for User Story 2

- [X] T016 [P] [US2] Integration test for `GET /api/debts` response shape (both directions
      present) in `tests/FamilyBudget.Api.Tests/DebtEndpointsTests.cs`

### Implementation for User Story 2

- [X] T017 [US2] Implement `GET /api/debts` in `src/FamilyBudget.Api/Endpoints/DebtEndpoints.cs`,
      grouping into `receivables`/`payables` (depends on T006, T011)
- [X] T018 [US2] Add a "ספר חובות" (Debts Ledger) tab to `src/FamilyBudget.Api/wwwroot/index.html`
      alongside the existing tabs, rendering the two tables with add/edit/delete per debt and a
      visible status indicator (depends on T017)

**Checkpoint**: User Stories 1 AND 2 both work independently

---

## Phase 5: User Story 3 - Record Repayments with Automatic Transaction Generation (Priority: P3)

**Goal**: Recording a repayment updates the debt's balance/status and automatically creates the
matching transaction, feeding the Monthly Overview's debt-repayments figure (FR-008-FR-013).

**Independent Test**: Record a partial repayment against each direction and verify the debt
balance, status, generated transaction, and (for payables) the Monthly Overview's debt-repayments
figure all update correctly; attempt an over-payment and verify it is rejected.

### Tests for User Story 3

- [X] T019 [P] [US3] Unit tests for `Debt.RecordRepayment` — partial/full repayment, auto-close at
      zero, rejection when amount exceeds balance or debt is already closed — in
      `tests/FamilyBudget.Core.Tests/DebtTests.cs`
- [X] T020 [P] [US3] Unit tests for `DebtRepaymentService` — a receivable repayment produces an
      `Income` transaction with `IsTitheApplicable = false`; a payable repayment produces a
      `DebtRepayment`-type transaction; both for exactly the paid amount — in
      `tests/FamilyBudget.Core.Tests/DebtRepaymentServiceTests.cs`
- [X] T021 [P] [US3] Integration tests for `POST /api/debts/{id}/repayments` (success, over-payment
      `400`, closed-debt `409`) in `tests/FamilyBudget.Api.Tests/DebtEndpointsTests.cs`
- [X] T022 [P] [US3] Extend `tests/FamilyBudget.Api.Tests/MonthlyOverviewEndpointsTests.cs` to
      assert `debtRepaymentsSummary` reflects a recorded payable repayment for that month

### Implementation for User Story 3

- [X] T023 [US3] Implement `DebtRepaymentService.RecordRepaymentAsync(debtId, amount,
      paymentMethod)` in `src/FamilyBudget.Core/Services/DebtRepaymentService.cs`: loads the debt,
      calls `Debt.RecordRepayment`, creates the matching `Transaction` (Income/not-tithe-applicable
      for Receivable, `DebtRepayment` for Payable), and persists both together (depends on T002,
      T003, T006)
- [X] T024 [US3] Implement `POST /api/debts/{id}/repayments` in
      `src/FamilyBudget.Api/Endpoints/DebtEndpoints.cs`, returning `409` for an already-closed debt
      and `400` for an over-payment (depends on T011, T023)
- [X] T025 [US3] Update `MonthlyOverviewQueryService.DebtRepaymentsSummary` in
      `src/FamilyBudget.Core/Services/MonthlyOverviewQueryService.cs` to sum the viewed month's
      `DebtRepayment`-type transactions instead of the hardcoded placeholder (FR-012) (depends on
      T003)
- [X] T026 [US3] Add a "record repayment" action (amount + payment method, inline — not a popup)
      per debt row in the Debts Ledger tab in `src/FamilyBudget.Api/wwwroot/index.html` (depends on
      T018, T024)

**Checkpoint**: All three user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Hardening and end-to-end validation across all three stories

- [X] T027 [P] Run quickstart.md end-to-end validation manually against a running
      `src/FamilyBudget.Api` instance
- [X] T028 Run the full test suite (`dotnet test`) and confirm all Constitution Principle IV
      invariant tests (T009, T019, T020) pass

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
- **User Story 2 (P2)**: Depends on the `Debt` entity/repository from Foundational; reads the
  same data US1 writes but does not require US1's endpoints to exist to be developed
- **User Story 3 (P3)**: Depends on `TransactionType.DebtRepayment` (Foundational) and the `Debt`
  entity; extends the ledger screen built in US2 with the repayment action — implement after US2
  to avoid rework

### Within Each User Story

- Tests are written before implementation and MUST fail first
- Core services before API endpoints
- Story complete and checkpoint-validated before moving to the next priority

### Parallel Opportunities

- T003, T004 in Foundational can run in parallel (different files)
- T009/T010 (US1 tests) can run in parallel
- T019/T020/T021/T022 (US3 tests) can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run T009/T010, confirm debt CRUD works per contracts/api.md

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → validate independently → MVP demo (debt CRUD)
3. User Story 2 → validate independently → demo the split ledger screen
4. User Story 3 → validate independently → demo repayment + automatic transaction generation
5. Polish phase → full regression pass

---

## Notes

- [P] tasks touch different files with no unmet dependencies
- Every task listed above maps to a specific file path from plan.md's project structure
- Commit after each task or logical group
- Constitution Principle IV makes T009, T019, T020 non-negotiable — do not skip them even if
  time-constrained
