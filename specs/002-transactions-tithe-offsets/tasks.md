---

description: "Task list for Transactions & Tithe (Chomesh) Offsets"
---

# Tasks: Transactions & Tithe (Chomesh) Offsets

**Input**: Design documents from `specs/002-transactions-tithe-offsets/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included. Constitution Principle IV (Financial Data Integrity, NON-NEGOTIABLE) requires
automated tests proving the protected tithe calculation and its carry-forward invariants hold, so
Core unit tests are mandatory, not optional, for this feature.

**Organization**: Tasks are grouped by user story (spec.md priorities P1/P2/P3) to enable
independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root

## Path Conventions

Per plan.md: reuses the existing `src/FamilyBudget.Core/`, `src/FamilyBudget.Infrastructure/`,
`src/FamilyBudget.Api/`, `tests/FamilyBudget.Core.Tests/`, `tests/FamilyBudget.Api.Tests/`
projects from feature 001 — no new projects are created.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the existing feature-001 solution is ready to receive this feature's new
entities/endpoints; no new projects or packages are needed.

- [X] T001 Confirm `dotnet build` succeeds on the current solution before adding any new code
      (baseline check)
- [X] T002 [P] Confirm no new NuGet packages are required: `Microsoft.EntityFrameworkCore.Sqlite`,
      `Microsoft.EntityFrameworkCore.Design`, xUnit, and `Microsoft.AspNetCore.Mvc.Testing` are
      already referenced from feature 001 and are reused as-is (research.md)

**Checkpoint**: Solution builds; ready to add feature 002 code

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, persistence, and DI wiring that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T003 Create `Transaction` entity with nested `TransactionType`/`PaymentMethod` enums in
      `src/FamilyBudget.Core/Entities/Transaction.cs` per data-model.md (`Id`, `Date`, `Amount`,
      `Type`, `PaymentMethod`, `IsTitheApplicable` (nullable, required iff `Type == Income`),
      `Description`), enforcing validation rules (FR-001, FR-001a, FR-002) in a constructor/factory
      guard
- [X] T004 [P] Create `TitheSetting` entity in `src/FamilyBudget.Core/Entities/TitheSetting.cs`
      (single-row, `Rate` in `(0, 1]`, defaults to `0.2`) per data-model.md and the
      `AnnualReserve` single-row pattern (FR-006)
- [X] T005 [P] Define `ITransactionRepository` abstraction in
      `src/FamilyBudget.Core/Abstractions/ITransactionRepository.cs` (e.g.
      `AddAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`,
      `GetByMonthAsync(year, month, type?, paymentMethod?)`, and a method to fetch all
      transactions up to and including a given month, needed by the `TitheEngine`'s forward walk)
- [X] T006 [P] Define `ITitheSettingRepository` abstraction in
      `src/FamilyBudget.Core/Abstractions/ITitheSettingRepository.cs` (`GetRateAsync`,
      `SetRateAsync`) mirroring `IAnnualReserveRepository`'s upsert pattern
- [X] T007 Extend `FamilyBudgetDbContext` in
      `src/FamilyBudget.Infrastructure/Persistence/FamilyBudgetDbContext.cs` with `Transactions`
      and `TitheSettings` `DbSet`s, plus EF Core configuration mapping `TransactionType`/
      `PaymentMethod` to strings via value conversion (research.md) (depends on T003, T004)
- [X] T008 Implement `TransactionRepository` in
      `src/FamilyBudget.Infrastructure/Repositories/TransactionRepository.cs` implementing
      `ITransactionRepository` against `FamilyBudgetDbContext` (depends on T005, T007)
- [X] T009 [P] Implement `TitheSettingRepository` in
      `src/FamilyBudget.Infrastructure/Repositories/TitheSettingRepository.cs` implementing
      `ITitheSettingRepository` (depends on T006, T007)
- [X] T010 Create an additive EF Core migration (`AddTransactionsAndTitheSettings`) for the two new
      tables using `dotnet ef migrations add` against `src/FamilyBudget.Infrastructure` (depends on
      T007)
- [X] T011 Wire up dependency injection in `src/FamilyBudget.Api/Program.cs`: register
      `ITransactionRepository` → `TransactionRepository` and `ITitheSettingRepository` →
      `TitheSettingRepository` (depends on T008, T009)

**Checkpoint**: Foundation ready — `dotnet ef database update` succeeds and DI resolves both new
repositories; user story implementation can now begin

---

## Phase 3: User Story 1 - Record Day-to-Day Transactions (Priority: P1) 🎯 MVP

**Goal**: Let the user record, edit, delete, and list income/expense transactions tagged by type
and payment method, with income additionally tagged tithe-applicable or not (FR-001, FR-001a,
FR-002, FR-003, FR-004).

**Independent Test**: Record several transactions of different types/payment methods for the
current month via `POST /api/transactions`, then verify `GET /api/transactions?year=&month=`
returns them with correct fields and supports filtering by type/payment method, per
contracts/api.md.

### Tests for User Story 1

- [X] T012 [P] [US1] Unit tests for `Transaction` validation rules — amount must be positive,
      type/payment method required, `IsTitheApplicable` required exactly when `Type == Income` —
      in `tests/FamilyBudget.Core.Tests/TransactionValidationTests.cs`
- [X] T013 [P] [US1] Integration tests for `POST`/`GET`/`PUT`/`DELETE /api/transactions` (including
      the `400` cases from contracts/api.md) in
      `tests/FamilyBudget.Api.Tests/TransactionEndpointsTests.cs`

### Implementation for User Story 1

- [X] T014 [P] [US1] Define `CreateTransactionRequest`/`TransactionResponse`/
      `TransactionListResponse` DTOs in `src/FamilyBudget.Api/Contracts/TransactionContracts.cs`
      per contracts/api.md
- [X] T015 [US1] Implement `POST /api/transactions` in
      `src/FamilyBudget.Api/Endpoints/TransactionEndpoints.cs` with `400` validation for missing/
      invalid `type`, `paymentMethod`, `amount`, or a misused `isTitheApplicable` (depends on T008,
      T014)
- [X] T016 [US1] Implement `GET /api/transactions?year=&month=&type=&paymentMethod=` in
      `src/FamilyBudget.Api/Endpoints/TransactionEndpoints.cs` (depends on T008, T014)
- [X] T017 [US1] Implement `PUT /api/transactions/{id}` in
      `src/FamilyBudget.Api/Endpoints/TransactionEndpoints.cs` (depends on T015)
- [X] T018 [US1] Implement `DELETE /api/transactions/{id}` in
      `src/FamilyBudget.Api/Endpoints/TransactionEndpoints.cs` (depends on T015)
- [X] T019 [US1] Register the transaction endpoints in `src/FamilyBudget.Api/Program.cs` (depends
      on T015, T016, T017, T018)

**Checkpoint**: User Story 1 is fully functional and independently testable — this is the MVP

---

## Phase 4: User Story 2 - Monthly Overview Screen: Income, Tithe & Expense Breakdown (Priority: P2)

**Goal**: Compute the protected monthly tithe obligation (gross target minus fixed donations minus
prior-month small-charity offset, floored at zero with excess carried forward as credit) and
present it, together with the income split, donations, and expense tables, on a single Monthly
Overview screen; feed the net tithe-due figure into the existing Dashboard (FR-005–FR-018).

**Independent Test**: Seed a month's tithe-applicable income, a fixed donation, and a prior
month's unresolved small-charity remainder, call `GET /api/monthly-overview`, and verify
`titheObligation.netTitheDue` matches the formula in research.md and `donations.remainingToGive`
reconciles against it; then call `GET /api/dashboard` for the same month and verify its `titheDue`
matches.

### Tests for User Story 2

- [X] T020 [P] [US2] Unit tests for `TitheEngine`'s gross tithe target computation — only
      tithe-applicable income counts, non-tithe-applicable income is excluded (FR-005, FR-007) —
      in `tests/FamilyBudget.Core.Tests/TitheEngineTests.cs`
- [X] T021 [P] [US2] Unit tests for `TitheEngine`'s protected deductions and zero floor — fixed
      donations and prior-month small-charity offset subtract from the gross target, net due never
      goes negative (FR-008, FR-009) — in `tests/FamilyBudget.Core.Tests/TitheEngineTests.cs`
- [X] T022 [P] [US2] Unit tests for the generic excess-deduction credit carry-forward — when
      deductions exceed the gross target, the excess reduces the following month's gross target
      before its own deductions apply (FR-009 edge case) — in
      `tests/FamilyBudget.Core.Tests/TitheCreditCarryForwardTests.cs`
- [X] T023 [P] [US2] Integration test for `GET /api/monthly-overview` response shape, income-split
      subtotals, and `remainingToGive` math in
      `tests/FamilyBudget.Api.Tests/MonthlyOverviewEndpointsTests.cs`
- [X] T024 [P] [US2] Extend `tests/FamilyBudget.Api.Tests/DashboardEndpointTests.cs` to assert
      `GET /api/dashboard`'s new `titheDue` object matches the same month's
      `/api/monthly-overview` result (FR-013)

### Implementation for User Story 2

- [X] T025 [US2] Implement `TitheEngine.ComputeMonth(year, month)` — the forward-walk algorithm
      from research.md (gross target, fixed-donation deduction, credit carry-in/out, small-charity
      applied, net due floor) — in `src/FamilyBudget.Core/Services/TitheEngine.cs` (depends on
      T003, T004, T008, T009)
- [X] T026 [US2] Implement `MonthlyOverviewQueryService` assembling the income split, donations +
      remaining-to-give, fixed/regular expense tables, debt-repayments placeholder (`0`), and
      outflow/savings summary in
      `src/FamilyBudget.Core/Services/MonthlyOverviewQueryService.cs` (depends on T025)
- [X] T027 [P] [US2] Define the `MonthlyOverviewResponse` DTO tree in
      `src/FamilyBudget.Api/Contracts/MonthlyOverviewContracts.cs` per contracts/api.md
- [X] T028 [US2] Implement `GET /api/monthly-overview?year=&month=` in
      `src/FamilyBudget.Api/Endpoints/MonthlyOverviewEndpoints.cs` (depends on T026, T027)
- [X] T029 [US2] Implement `GET /api/tithe-setting` and `PUT /api/tithe-setting` in
      `src/FamilyBudget.Api/Endpoints/MonthlyOverviewEndpoints.cs` (depends on T009)
- [X] T030 [US2] Extend `DashboardContracts` in
      `src/FamilyBudget.Api/Contracts/DashboardContracts.cs` with the `titheDue` breakdown fields
      per contracts/api.md (depends on T025)
- [X] T031 [US2] Extend `GET /api/dashboard` in
      `src/FamilyBudget.Api/Endpoints/DashboardEndpoints.cs` to compute and include `titheDue` via
      `TitheEngine` (FR-013) (depends on T025, T030)
- [X] T032 [US2] Register the monthly-overview and tithe-setting endpoints in
      `src/FamilyBudget.Api/Program.cs` (depends on T028, T029, T031)
- [X] T033 [US2] Add a "Monthly Overview" view to `src/FamilyBudget.Api/wwwroot/index.html`: two
      income tables (tithe-applicable / not), a donations table with "remaining to give", fixed and
      regular expense tables, a debt-repayments summary line, and a bottom outflow/savings summary
      — reusing the existing RTL/CSS conventions (depends on T028–T032)

**Checkpoint**: User Stories 1 AND 2 both work independently

---

## Phase 5: User Story 3 - Small-Charity Offset Carry-Forward Visibility (Priority: P3)

**Goal**: Show, for any month, the small-charity expense total, how much was applied as an offset,
and how much unapplied remainder carries forward, with nothing ever lost or double-counted across
consecutive months (FR-010, SC-003).

**Independent Test**: Record small-charity expenses across two consecutive months and verify the
first month's unapplied remainder appears as the second month's `availableFromPriorMonth`, with
the running totals reconciling per data-model.md's `SmallCharityOffsetLedger`.

### Tests for User Story 3

- [X] T034 [P] [US3] Unit tests for the multi-month small-charity carry-forward walk across 3+
      consecutive months (FR-010) in
      `tests/FamilyBudget.Core.Tests/SmallCharityOffsetCarryForwardTests.cs`
- [X] T035 [P] [US3] Unit test proving 100% accounting — across any sequence of months, every
      small-charity amount is either "applied" or "carried forward", never lost or double-counted
      (SC-003) — in `tests/FamilyBudget.Core.Tests/SmallCharityOffsetCarryForwardTests.cs`

### Implementation for User Story 3

- [X] T036 [US3] Extend `TitheEngine` to compute and expose the full `SmallCharityOffsetLedger`
      shape (`smallCharityExpenseTotal`, `availableFromPriorMonth`, `appliedThisMonth`,
      `unappliedRemainder`) in `src/FamilyBudget.Core/Services/TitheEngine.cs` (depends on T025)
- [X] T037 [US3] Add `smallCharityOffsetLedger` to the `GET /api/monthly-overview` response in
      `src/FamilyBudget.Api/Contracts/MonthlyOverviewContracts.cs` and
      `src/FamilyBudget.Api/Endpoints/MonthlyOverviewEndpoints.cs` per contracts/api.md (depends on
      T036, T027, T028)
- [X] T038 [US3] Add a small-charity offset visibility section to the Monthly Overview view in
      `src/FamilyBudget.Api/wwwroot/index.html` (depends on T033, T037)

**Checkpoint**: All three user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Hardening and end-to-end validation across all three stories

- [X] T039 [P] Run quickstart.md end-to-end validation manually against a running
      `src/FamilyBudget.Api` instance — verify transactions, monthly overview, dashboard
      integration, and tithe-setting endpoints all behave per contracts/api.md
- [X] T040 Run the full test suite (`dotnet test`) and confirm all Constitution Principle IV
      invariant tests (T012, T020, T021, T022, T034, T035) pass

---

## Phase 7: Direct Manipulation & Formula Fields (post-implementation user request)

**Purpose**: Per explicit user direction after the initial implementation, extend User Stories 1-2
so the monthly overview screen supports full inline CRUD on income/donations, adds a new
budgeted-vs-used planning model for Fixed/Regular expenses, and lets every amount field on both
the monthly and annual screens accept a persisted arithmetic formula (FR-019–FR-022).

- [X] T041 [P] Add `AmountFormula` to `Transaction` and `TotalAmountFormula`/`AmountUsedFormula` to
      `AnnualBudgetItem`, with matching EF Core migrations, contracts, and endpoint pass-through
      (FR-022)
- [X] T042 Create `MonthlyExpenseBudgetItem` entity (name, type restricted to Fixed/Regular
      Expense, budgeted/used amounts + formulas, computed `Remaining`) in
      `src/FamilyBudget.Core/Entities/MonthlyExpenseBudgetItem.cs`, with repository, migration,
      contracts, and full CRUD endpoints (`src/FamilyBudget.Api/Endpoints/MonthlyExpenseBudgetEndpoints.cs`)
      (FR-021)
- [X] T043 Re-point `MonthlyOverviewQueryService`'s Fixed/Regular expense sections at
      `MonthlyExpenseBudgetItem` instead of raw transactions, and source `TotalOutflow` from each
      category's used-amount sum (FR-016, FR-018)
- [X] T044 [P] Core unit tests (`MonthlyExpenseBudgetItemTests.cs`) and API integration tests
      (`MonthlyExpenseBudgetEndpointsTests.cs`) for the new entity/endpoints; updated
      `MonthlyOverviewEndpointsTests.cs` for the new expense-section shape
- [X] T045 Build a dependency-free JS arithmetic formula parser/evaluator and a reusable
      formula-aware input widget in `wwwroot/index.html` (FR-022)
- [X] T046 [US1] Add inline edit (description, formula-aware amount) and delete, plus per-table
      "+ add" actions, to the Monthly Overview income and donations tables (FR-019, FR-020)
- [X] T047 [US2] Add editable Fixed/Regular expense budget-line tables (name, budgeted, used,
      computed remaining, add/delete) to the Monthly Overview screen (FR-016, FR-021)
- [X] T048 Retrofit the Annual Budget screen's "used" inline-edit field and "add item" form to use
      the same formula-aware widget (FR-022)
- [X] T049 Re-run the full test suite (70/70 passing) and validate end-to-end against a running
      instance via direct API calls (create/update income, donation, and budget-line items with
      formulas; confirm monthly-overview and dashboard reconcile)

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
- **User Story 2 (P2)**: Depends on the `Transaction`/`TitheSetting` entities and repositories from
  Foundational, and reads the same `Transaction` data US1 writes, but does not require US1's
  endpoints to exist — it can be developed in parallel against seeded data, though in practice US1
  is implemented first since it is how transactions get created
- **User Story 3 (P3)**: Extends the `TitheEngine`/`MonthlyOverviewQueryService` built in US2 (T025,
  T026) in place — implement after US2 to avoid rework, though its tests are independently runnable
  once US2's engine exists

### Within Each User Story

- Tests are written before implementation and MUST fail first
- Core services before API endpoints
- Story complete and checkpoint-validated before moving to the next priority

### Parallel Opportunities

- T001, T002 in Setup can run in parallel
- T004, T005, T006 in Foundational can run in parallel (different files); T009 can run in parallel
  with T008
- T012/T013 (US1 tests) can run in parallel; T014 (DTOs) can run in parallel with T012/T013
- T020/T021/T022/T023/T024 (US2 tests) can run in parallel with each other
- T034/T035 (US3 tests) can run in parallel with each other

---

## Parallel Example: User Story 2

```bash
# Launch all US2 tests together:
Task: "Unit tests for gross tithe target in tests/FamilyBudget.Core.Tests/TitheEngineTests.cs"
Task: "Unit tests for protected deductions and zero floor in tests/FamilyBudget.Core.Tests/TitheEngineTests.cs"
Task: "Unit tests for credit carry-forward in tests/FamilyBudget.Core.Tests/TitheCreditCarryForwardTests.cs"
Task: "Integration test for GET /api/monthly-overview in tests/FamilyBudget.Api.Tests/MonthlyOverviewEndpointsTests.cs"
Task: "Extend DashboardEndpointTests.cs for titheDue"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run T012/T013, confirm transaction CRUD + filtering works per
   contracts/api.md
5. Demo transaction recording against seeded data

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → validate independently → MVP demo (transaction recording)
3. User Story 2 → validate independently → demo the Monthly Overview screen and dashboard tithe line
4. User Story 3 → validate independently → demo the small-charity carry-forward visibility
5. Polish phase → full regression pass

---

## Notes

- [P] tasks touch different files with no unmet dependencies
- Every task listed above maps to a specific file path from plan.md's project structure
- Commit after each task or logical group
- Constitution Principle IV makes T012, T020, T021, T022, T034, T035 non-negotiable — do not skip
  them even if time-constrained
