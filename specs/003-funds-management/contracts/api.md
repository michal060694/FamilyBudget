# API Contracts: Funds Management & Summary

## `GET /api/funds`

Backs User Story 2 (View Fund Summary Screen; FR-004, FR-008-FR-012).

**Response 200**:

```json
{
  "funds": [
    {
      "fundId": "guid",
      "name": "מיטב",
      "totalBalance": 15000,
      "earmarkedTotal": 15000,
      "discrepancy": 0,
      "earmarks": [
        { "id": "guid", "purposeLabel": "פאה", "amount": 10000 },
        { "id": "guid", "purposeLabel": "שנתי 26", "amount": 5000 }
      ]
    }
  ]
}
```

`discrepancy` = `totalBalance - earmarkedTotal`: `0` fully reconciled, `> 0` unearmarked remainder,
`< 0` over-earmarked mismatch (see data-model.md).

## `POST /api/funds`

Creates a new fund (FR-001).

**Request body**: `{ "name": "IBI", "totalBalance": 8000 }`

**Response 201**: `{ "fundId": "guid", "name": "IBI", "totalBalance": 8000, "earmarkedTotal": 0, "discrepancy": 8000, "earmarks": [] }`

**Errors**: `400` if `name` is empty or `totalBalance` is negative.

## `PUT /api/funds/{id}`

Renames a fund and/or updates its recorded total balance (FR-002).

**Request body**: `{ "name": "IBI", "totalBalance": 8500 }`

**Response 200**: the updated fund summary (same shape as a `funds[]` entry above).

**Errors**: `400` if `name` is empty or `totalBalance` is negative; `404` if `id` does not match
any fund.

## `DELETE /api/funds/{id}`

Deletes a fund and all of its earmark lines (FR-003).

**Response**: `204 No Content`. **Errors**: `404` if `id` does not match any fund.

## `POST /api/funds/{fundId}/earmarks`

Adds a new earmark line within a fund (FR-005).

**Request body**: `{ "purposeLabel": "חיסכון כללי", "amount": 3000 }`

**Response 201**: `{ "id": "guid", "purposeLabel": "חיסכון כללי", "amount": 3000 }`.
`Location` header points to `/api/funds/earmarks/{id}`.

**Errors**: `400` if `purposeLabel` is empty or `amount` is not strictly positive; `404` if
`fundId` does not match any fund.

## `PUT /api/funds/earmarks/{id}`

Updates an earmark line's purpose label and/or amount (FR-006).

**Request body**: `{ "purposeLabel": "חיסכון כללי", "amount": 3500 }`

**Response 200**: `{ "id": "guid", "purposeLabel": "חיסכון כללי", "amount": 3500 }`.

**Errors**: `400` if `purposeLabel` is empty or `amount` is not strictly positive; `404` if `id`
does not match any earmark line.

## `DELETE /api/funds/earmarks/{id}`

Deletes an earmark line (FR-007).

**Response**: `204 No Content`. **Errors**: `404` if `id` does not match any earmark line.
