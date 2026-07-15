# API Contracts: Debts Ledger (Debts & Loans)

## `GET /api/debts`

Backs User Story 2 (the split two-table view; FR-007).

**Response 200**:

```json
{
  "receivables": [
    {
      "id": "guid",
      "direction": "Receivable",
      "counterpartyName": "יוסי כהן",
      "originalAmount": 1000,
      "currentBalance": 1000,
      "status": "Open",
      "targetDate": "2026-09-01",
      "repaymentRate": null,
      "notes": "הלוואה קצרה"
    }
  ],
  "payables": [
    {
      "id": "guid",
      "direction": "Payable",
      "counterpartyName": "גמ\"ח השכונה",
      "originalAmount": 5000,
      "currentBalance": 3000,
      "status": "Open",
      "targetDate": null,
      "repaymentRate": 500,
      "notes": null
    }
  ]
}
```

## `POST /api/debts`

Creates a new debt entry in either direction (FR-001, FR-002).

**Request body**:

```json
{
  "direction": "Receivable",
  "counterpartyName": "יוסי כהן",
  "originalAmount": 1000,
  "targetDate": "2026-09-01",
  "repaymentRate": null,
  "notes": "הלוואה קצרה"
}
```

**Response 201**: the created debt, in the same shape as a `receivables[]`/`payables[]` entry above
(`status: "Open"`, `currentBalance` equal to `originalAmount`).

**Errors**: `400` if `counterpartyName` is empty, `originalAmount` is not strictly positive, or
`repaymentRate` is present and not strictly positive.

## `PUT /api/debts/{id}`

Edits a debt's counterparty name, original amount, direction-specific field, and notes (FR-003).
Preserves the amount already paid when the original amount changes (research.md).

**Request body**: same shape as `POST /api/debts` (direction is immutable — not included).

**Response 200**: the updated debt. **Errors**: `400` (same as `POST`); `404` if `id` does not
match any debt.

## `DELETE /api/debts/{id}`

Deletes a debt entry (FR-004). Does not affect any transaction previously generated from a
repayment against it (FR-013).

**Response**: `204 No Content`. **Errors**: `404` if `id` does not match any debt.

## `POST /api/debts/{id}/repayments`

Records a repayment against an open debt (FR-008-FR-011).

**Request body**:

```json
{ "amount": 400, "paymentMethod": "BankTransfer" }
```

**Response 200**: the updated debt (reduced `currentBalance`, `status` possibly now `Closed`).

**Errors**: `400` if `amount` is not strictly positive or exceeds the debt's current balance;
`404` if `id` does not match any debt; `409 Conflict` if the debt's status is already `Closed`.

## `GET /api/monthly-overview?year={year}&month={month}` (extended, feature 002)

No response shape change. `debtRepaymentsSummary` (already present in
`MonthlyOverviewResponse` since feature 002) now reflects the real sum of that month's
`DebtRepayment`-type transactions instead of always `0` (FR-012).
