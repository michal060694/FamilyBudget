# API Contracts: Transactions & Tithe (Chomesh) Offsets

## `POST /api/transactions`

Records a new transaction (FR-001, FR-002, FR-001a).

**Request body**:

```json
{
  "date": "2026-07-15",
  "amount": 500,
  "type": "Income",
  "paymentMethod": "BankTransfer",
  "isTitheApplicable": true,
  "description": "Salary"
}
```

`isTitheApplicable` is required when `type` is `"Income"`; MUST be omitted or `null` for every
other type.

**Response 201**: the created transaction, in the same shape as the request body plus `id`.
`Location` header points to `/api/transactions/{id}`.

**Errors**: `400` if `amount` is not strictly positive; `type` or `paymentMethod` is missing or
not a recognized value; `isTitheApplicable` is missing when `type` is `Income`, or provided when
`type` is not `Income`.

## `GET /api/transactions?year={year}&month={month}&type={type}&paymentMethod={paymentMethod}`

Lists transactions for a calendar month, optionally filtered (FR-004).

**Query parameters**: `year` (int, required), `month` (int 1-12, required), `type` (optional),
`paymentMethod` (optional).

**Response 200**:

```json
{
  "year": 2026,
  "month": 7,
  "transactions": [
    {
      "id": "guid",
      "date": "2026-07-15",
      "amount": 500,
      "type": "Income",
      "paymentMethod": "BankTransfer",
      "isTitheApplicable": true,
      "description": "Salary"
    }
  ]
}
```

**Errors**: `400` if `month` is outside 1-12, or `type`/`paymentMethod` is not a recognized value.

## `PUT /api/transactions/{id}`

Edits an existing transaction (FR-003). Same request/validation shape as `POST`. Any figure
derived from the transaction (monthly income totals, tithe obligation, offset carry-forward) is
recomputed from current data on the next relevant `GET`.

**Response 200**: the updated transaction. **Errors**: `400` (same as `POST`); `404` if `id` does
not match any transaction.

## `DELETE /api/transactions/{id}`

Deletes a transaction (FR-003).

**Response**: `204 No Content`. **Errors**: `404` if `id` does not match any transaction.

## `GET /api/tithe-setting`

Returns the current household tithe rate (FR-006).

**Response 200**: `{ "rate": 0.2 }` (defaults to `0.2` if never set — see data-model.md).

## `PUT /api/tithe-setting`

Sets the household tithe rate (upsert, FR-006).

**Request body**: `{ "rate": 0.2 }`

**Response 200**: `{ "rate": 0.2 }`. **Errors**: `400` if `rate` is not in `(0, 1]`.

## `GET /api/monthly-overview?year={year}&month={month}`

Backs User Story 2, the Monthly Overview screen (FR-014–FR-018).

**Query parameters**: `year` (int, required), `month` (int 1-12, required).

**Response 200**:

```json
{
  "year": 2026,
  "month": 7,
  "titheApplicableIncome": {
    "lines": [ { "id": "guid", "date": "2026-07-15", "amount": 500, "description": "Salary" } ],
    "subtotal": 500
  },
  "nonTitheApplicableIncome": {
    "lines": [],
    "subtotal": 0
  },
  "titheObligation": {
    "grossTitheTarget": 100,
    "fixedDonationsThisMonth": 40,
    "creditCarriedIn": 0,
    "smallCharityAppliedThisMonth": 10,
    "netTitheDue": 50
  },
  "smallCharityOffsetLedger": {
    "smallCharityExpenseTotal": 15,
    "availableFromPriorMonth": 10,
    "appliedThisMonth": 10,
    "unappliedRemainder": 15
  },
  "donations": {
    "lines": [ { "id": "guid", "date": "2026-07-03", "amount": 40, "description": "Standing order" } ],
    "givenThisMonth": 40,
    "remainingToGive": 10
  },
  "fixedExpenses": { "lines": [], "subtotal": 0 },
  "regularExpenses": { "lines": [], "subtotal": 0 },
  "debtRepaymentsSummary": 0,
  "totalOutflow": 90,
  "totalIncome": 500,
  "remainingToSave": 410
}
```

`remainingToGive` = `titheObligation.netTitheDue - donations.givenThisMonth`, floored at zero
(FR-015). `debtRepaymentsSummary` is always `0` until the future Debts Ledger feature exists
(FR-017). `totalOutflow` = donations given + fixed expenses + regular expenses +
`debtRepaymentsSummary` (FR-018); `remainingToSave` = `totalIncome - totalOutflow`.

**Errors**: `400` if `month` is outside 1-12.

## `GET /api/dashboard?year={year}&month={month}` (extended, feature 001)

Existing endpoint from feature 001, extended per FR-013 to include the tithe line:

```json
{
  "year": 2026,
  "month": 7,
  "projectedIncome": 0,
  "fixedExpenses": 0,
  "totalRequiredAllocation": 0,
  "freeBalance": 0,
  "allocationLines": [],
  "titheDue": {
    "grossTitheTarget": 100,
    "fixedDonationsDeduction": 40,
    "smallCharityDeduction": 10,
    "netTitheDue": 50
  }
}
```

All other fields are unchanged from feature 001's contract (`specs/001-budget-smoothing-engine/contracts/api.md`).
