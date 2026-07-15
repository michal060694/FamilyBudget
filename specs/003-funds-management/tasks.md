---

description: "Task list for Funds Management & Summary"
---

# Tasks: Funds Management & Summary

**Input**: Design documents from `specs/003-funds-management/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api.md, quickstart.md

**Tests**: Included. Constitution Principle IV (Financial Data Integrity, NON-NEGOTIABLE) requires
automated tests proving the fund-balance reconciliation invariant holds, so Core unit tests are
mandatory, not optional, for this feature.

**Organization**: Tasks are grouped by user story (spec.md priorities P1/P2/P3) to enable
independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (US1, US2, US3)
- File paths are relative to the repository root

## Path Conventions

Per plan.md: reuses the existing `src/FamilyBudget.Core/`, `src/FamilyBudget.Infrastructure/`,
`src/FamilyBudget.Api/`, `tests/FamilyBudget.Core.Tests/`, `tests/FamilyBudget.Api.Tests/`
projects from features 001/002 — no new projects are created.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Confirm the existing solution is ready to receive this feature's new entities/endpoints.

- [X] T001 Confirm `dotnet build` succeeds on the current solution before adding any new code
      (baseline check); no new NuGet packages are required (reuses EF Core Sqlite, xUnit,
      `Microsoft.AspNetCore.Mvc.Testing` already referenced from features 001/002)

**Checkpoint**: Solution builds; ready to add feature 003 code

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Entities, persistence, and DI wiring that every user story depends on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

- [X] T002 [P] Create `Fund` entity in `src/FamilyBudget.Core/Entities/Fund.cs` per data-model.md
      (`Id`, `Name`, `TotalBalance`), with `Rename(name)` and `SetTotalBalance(amount)` methods and
      validation (`Name` non-empty, `TotalBalance >= 0`)
- [X] T003 [P] Create `FundEarmark` entity in `src/FamilyBudget.Core/Entities/FundEarmark.cs` per
      data-model.md (`Id`, `FundId`, `PurposeLabel`, `Amount`), with an `Update(purposeLabel,
      amount)` method and validation (`PurposeLabel` non-empty, `Amount > 0`)
- [X] T004 [P] Define `IFundRepository` abstraction in
      `src/FamilyBudget.Core/Abstractions/IFundRepository.cs` (`GetAllAsync`, `GetByIdAsync`,
      `AddAsync`, `DeleteAsync`, `SaveChangesAsync`)
- [X] T005 [P] Define `IFundEarmarkRepository` abstraction in
      `src/FamilyBudget.Core/Abstractions/IFundEarmarkRepository.cs` (`GetByFundIdAsync`,
      `GetByIdAsync`, `AddAsync`, `DeleteAsync`, `SaveChangesAsync`)
- [X] T006 Extend `FamilyBudgetDbContext` in
      `src/FamilyBudget.Infrastructure/Persistence/FamilyBudgetDbContext.cs` with `Funds` and
      `FundEarmarks` `DbSet`s, plus EF Core configuration including cascade delete from `Fund` to
      `FundEarmark` (research.md) (depends on T002, T003)
- [X] T007 [P] Implement `FundRepository` in
      `src/FamilyBudget.Infrastructure/Repositories/FundRepository.cs` implementing
      `IFundRepository` (depends on T004, T006)
- [X] T008 [P] Implement `FundEarmarkRepository` in
      `src/FamilyBudget.Infrastructure/Repositories/FundEarmarkRepository.cs` implementing
      `IFundEarmarkRepository` (depends on T005, T006)
- [X] T009 Create an additive EF Core migration (`AddFundsAndFundEarmarks`) using
      `dotnet ef migrations add` against `src/FamilyBudget.Infrastructure` (depends on T006)
- [X] T010 Wire up dependency injection in `src/FamilyBudget.Api/Program.cs`: register
      `IFundRepository` → `FundRepository` and `IFundEarmarkRepository` → `FundEarmarkRepository`
      (depends on T007, T008)

**Checkpoint**: Foundation ready — `dotnet ef database update` succeeds and DI resolves both new
repositories; user story implementation can now begin

---

## Phase 3: User Story 1 - Define Funds & Their Balances (Priority: P1) 🎯 MVP

**Goal**: Let the user create, rename, update the balance of, and delete funds (FR-001–FR-003).

**Independent Test**: Create a fund via `POST /api/funds`, update its name/balance via `PUT`, and
delete it via `DELETE`, verifying each change persists and the fund disappears after deletion.

### Tests for User Story 1

- [X] T011 [P] [US1] Unit tests for `Fund` validation (name required, balance >= 0) in
      `tests/FamilyBudget.Core.Tests/FundTests.cs`
- [X] T012 [P] [US1] Integration tests for `POST`/`PUT`/`DELETE /api/funds` (including `400`/`404`
      cases from contracts/api.md) in `tests/FamilyBudget.Api.Tests/FundEndpointsTests.cs`

### Implementation for User Story 1

- [X] T013 [P] [US1] Define `FundResponse`/`CreateFundRequest`/`UpdateFundRequest` DTOs in
      `src/FamilyBudget.Api/Contracts/FundContracts.cs` per contracts/api.md
- [X] T014 [US1] Implement `POST /api/funds` in `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs`
      with `400` validation (depends on T007, T013)
- [X] T015 [US1] Implement `PUT /api/funds/{id}` in
      `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs` (depends on T014)
- [X] T016 [US1] Implement `DELETE /api/funds/{id}` in
      `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs`, cascading to earmark lines (depends on T014)
- [X] T017 [US1] Register the fund endpoints in `src/FamilyBudget.Api/Program.cs` (depends on
      T014, T015, T016)

**Checkpoint**: User Story 1 is fully functional and independently testable — this is the MVP

---

## Phase 4: User Story 2 - View Fund Summary Screen (Priority: P2)

**Goal**: Show every fund with its balance and earmark breakdown on a dedicated "קרנות" (Funds)
tab (FR-004, FR-008, FR-013).

**Independent Test**: Create a couple of funds (with or without earmarks) and call
`GET /api/funds`, verifying every fund appears with its name, balance, and earmark list (empty
list allowed); open the new client tab and verify the same data renders.

### Tests for User Story 2

- [X] T018 [P] [US2] Unit tests for `FundSummaryQueryService`'s per-fund assembly (funds with and
      without earmarks) in `tests/FamilyBudget.Core.Tests/FundSummaryQueryServiceTests.cs`
- [X] T019 [P] [US2] Integration test for `GET /api/funds` response shape (multiple funds, one with
      no earmarks) in `tests/FamilyBudget.Api.Tests/FundEndpointsTests.cs`

### Implementation for User Story 2

- [X] T020 [US2] Implement `FundSummaryQueryService.GetAllAsync()` in
      `src/FamilyBudget.Core/Services/FundSummaryQueryService.cs`, assembling `FundSummary` records
      per data-model.md (depends on T004, T005)
- [X] T021 [US2] Implement `GET /api/funds` in `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs`
      (depends on T013, T020)
- [X] T022 [US2] Add a "קרנות" (Funds) tab to `src/FamilyBudget.Api/wwwroot/index.html` alongside
      the existing "תקציב שנתי" and "סקירה חודשית" tabs, rendering each fund as a card/table with
      its name, balance, and earmark list (depends on T021)

**Checkpoint**: User Stories 1 AND 2 both work independently

---

## Phase 5: User Story 3 - Manage Earmarks & Balance Reconciliation (Priority: P3)

**Goal**: Let the user add/edit/delete earmark lines within a fund, with the earmarked-sum-vs-
balance discrepancy always visibly reconciled or flagged (FR-005–FR-012).

**Independent Test**: Add earmark lines to a fund summing to less than, exactly, and more than its
balance, verifying the discrepancy figure is correct and clearly distinguishable in each case.

### Tests for User Story 3

- [X] T023 [P] [US3] Unit tests for `FundEarmark` validation (purpose label required, amount > 0)
      in `tests/FamilyBudget.Core.Tests/FundEarmarkTests.cs`
- [X] T024 [P] [US3] Unit tests for `FundSummaryQueryService`'s discrepancy calculation — exactly
      reconciled (`0`), under-earmarked (`> 0`), and over-earmarked (`< 0`) — in
      `tests/FamilyBudget.Core.Tests/FundSummaryQueryServiceTests.cs`
- [X] T025 [P] [US3] Integration tests for `POST`/`PUT`/`DELETE /api/funds/{fundId}/earmarks` and
      `/api/funds/earmarks/{id}` (including `400`/`404` cases) in
      `tests/FamilyBudget.Api.Tests/FundEndpointsTests.cs`

### Implementation for User Story 3

- [X] T026 [P] [US3] Define `FundEarmarkResponse`/`CreateFundEarmarkRequest`/
      `UpdateFundEarmarkRequest` DTOs in `src/FamilyBudget.Api/Contracts/FundContracts.cs` per
      contracts/api.md
- [X] T027 [US3] Implement `POST /api/funds/{fundId}/earmarks` in
      `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs` (depends on T008, T026)
- [X] T028 [US3] Implement `PUT /api/funds/earmarks/{id}` in
      `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs` (depends on T027)
- [X] T029 [US3] Implement `DELETE /api/funds/earmarks/{id}` in
      `src/FamilyBudget.Api/Endpoints/FundEndpoints.cs` (depends on T027)
- [X] T030 [US3] Register the earmark endpoints in `src/FamilyBudget.Api/Program.cs` (depends on
      T027, T028, T029)
- [X] T031 [US3] Add earmark-line management (add/edit/delete) and a clearly distinguished
      discrepancy indicator (reconciled / under-earmarked / over-earmarked) to the Funds tab in
      `src/FamilyBudget.Api/wwwroot/index.html` (depends on T022, T030)

**Checkpoint**: All three user stories are independently functional

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Hardening and end-to-end validation across all three stories

- [X] T032 [P] Run quickstart.md end-to-end validation manually against a running
      `src/FamilyBudget.Api` instance
- [X] T033 Run the full test suite (`dotnet test`) and confirm all Constitution Principle IV
      invariant tests (T011, T018, T023, T024) pass

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
- **User Story 2 (P2)**: Depends on the `Fund`/`FundEarmark` entities and repositories from
  Foundational; reads the same `Fund` data US1 writes, but does not require US1's endpoints to
  exist to be developed
- **User Story 3 (P3)**: Depends on `FundEarmarkRepository` (Foundational) and extends the
  `FundSummaryQueryService`/response shape built in US2 — implement after US2 to avoid rework

### Within Each User Story

- Tests are written before implementation and MUST fail first
- Core services before API endpoints
- Story complete and checkpoint-validated before moving to the next priority

### Parallel Opportunities

- T002-T005 in Foundational can run in parallel (different files)
- T011/T012 (US1 tests) can run in parallel
- T018/T019 (US2 tests) can run in parallel
- T023/T024/T025 (US3 tests) can run in parallel

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (blocks everything)
3. Complete Phase 3: User Story 1
4. **STOP and VALIDATE**: run T011/T012, confirm fund CRUD works per contracts/api.md

### Incremental Delivery

1. Setup + Foundational → foundation ready
2. User Story 1 → validate independently → MVP demo (fund CRUD)
3. User Story 2 → validate independently → demo the Funds summary tab
4. User Story 3 → validate independently → demo earmark management and reconciliation
5. Polish phase → full regression pass

---

## Notes

- [P] tasks touch different files with no unmet dependencies
- Every task listed above maps to a specific file path from plan.md's project structure
- Commit after each task or logical group
- Constitution Principle IV makes T011, T018, T023, T024 non-negotiable — do not skip them even if
  time-constrained
