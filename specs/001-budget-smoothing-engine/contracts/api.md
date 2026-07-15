# API Contracts: Annual Budget & Smoothing Engine

**Updated 2026-07-14**: query parameters and fields switched from Hebrew-calendar to
Gregorian-calendar terms (`year`/`month` instead of `hebrewYear`/`hebrewMonth`).

**Updated 2026-07-14 (later same day)**: `GET /api/annual-budget` now also returns the
`AnnualBudgetSummary` fields (data-model.md), and two write endpoints were added for the client
screen redesign: `POST /api/annual-budget-items` (create a new item) and `PUT /api/reserve` (set
the manually-entered reserve for a year). This feature is no longer purely read-only.

**Updated 2026-07-14 (yet later same day)**: `isFullyFunded` (deposit-based, never true in
practice since there is no deposit write path) was replaced by `amountUsed` +
`isFullyUsed` (usage-based) in `AnnualBudgetItemView`. A `PATCH /api/annual-budget-items/{id}/usage`
endpoint was added — per explicit user direction it **overwrites** `AmountUsed` to the exact
value given (it does not add to the existing amount), matching the client's inline-editable
"נוצל" table cell.

## `GET /api/dashboard?year={year}&month={month}`

Backs User Story 1 (Monthly Proactive Dashboard, FR-003).

**Query parameters**: `year` (int, required), `month` (int 1-12, required). Both default to the
current date when omitted.

**Response 200**:

```json
{
  "year": 2026,
  "month": 10,
  "projectedIncome": 0,
  "fixedExpenses": 0,
  "totalRequiredAllocation": 0,
  "freeBalance": 0,
  "allocationLines": [
    {
      "annualBudgetItemId": "guid",
      "name": "December Holidays",
      "targetMonth": 12,
      "totalAmount": 0,
      "amountAlreadySetAside": 0,
      "amountUsed": 0,
      "allocatedMonthly": 0
    }
  ]
}
```

**Errors**: `400` if `month` is outside 1-12.

## `GET /api/annual-budget?year={year}`

Backs User Story 2 (Annual Budget & Year-Cycle Table, FR-004) and the client's top summary tiles.

**Query parameters**: `year` (int, required; defaults to current year).

**Response 200**:

```json
{
  "year": 2026,
  "reserveOnHand": 12000,
  "totalAnnualBudget": 48000,
  "notYetCovered": 36000,
  "monthlyAllocation": 3000,
  "items": [
    {
      "annualBudgetItemId": "guid",
      "name": "December Holidays",
      "targetMonth": 12,
      "totalAmount": 0,
      "amountAlreadySetAside": 0,
      "amountUsed": 0,
      "isFullyUsed": false
    }
  ]
}
```

Ordering: items with `targetMonth` set are returned first, sorted ascending starting from January
(month 1); items with `targetMonth: null` (general/month-independent, FR-001) are appended last,
per FR-004. Summary fields per data-model.md's `AnnualBudgetSummary`: `notYetCovered` and
`monthlyAllocation` use flat year-level math (always ÷12), independent of the per-item
`BudgetSmoothingEngine` used by the dashboard endpoint above. `isFullyUsed` is `amountUsed >=
totalAmount` (usage-based — see the update note at the top of this file).

**Errors**: none beyond standard validation (`year` must be a positive integer).

## `POST /api/annual-budget-items`

Creates a new annual budget item, starting fully unfunded/unused (`amountAlreadySetAside: 0`,
`amountUsed: 0`).

**Request body**:

```json
{ "year": 2026, "name": "Clothing", "totalAmount": 3600, "targetMonth": null }
```

`targetMonth` is optional/nullable (omit or pass `null` for a general item).

**Response 201**: the created item, in the same shape as an `items[]` entry above. `Location`
header points to `/api/annual-budget-items/{id}` (no corresponding `GET` for a single item exists
yet — fetch via `GET /api/annual-budget` instead).

**Errors**: `400` if `name` is empty, `totalAmount` is not strictly positive, or `targetMonth` is
outside 1-12 when provided.

## `PATCH /api/annual-budget-items/{id}/usage`

Sets `AmountUsed` on an existing item to an exact value — an **overwrite**, not an increment.

**Request body**:

```json
{ "amount": 300 }
```

**Response 200**: the updated item, same shape as an `items[]` entry above.

**Errors**: `400` if `amount` is negative (zero is allowed — it means "not used yet"); `404` if
`id` does not match any item.

## `DELETE /api/annual-budget-items/{id}`

Permanently deletes an annual budget item. Added per explicit user direction alongside the client
row delete control.

**Response**: `204 No Content` on success; `404` if `id` does not match any item.

## `PUT /api/reserve?year={year}`

Creates or updates the `AnnualReserve` amount for a year (upsert).

**Request body**:

```json
{ "amount": 12000 }
```

**Response 200**: `{ "year": 2026, "amount": 12000 }`

**Errors**: `400` if `amount` is negative.
