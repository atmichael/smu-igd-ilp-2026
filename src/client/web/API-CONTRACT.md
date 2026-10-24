# Frontend API contract proposal

All endpoints are relative to the configured `/api` base path and use the existing bearer-token authentication. Collection responses use `{ "items": [], "totalCount": 0 }`.

## Search companies

`GET /api/companies?search={partialText}`

Searches case-insensitively for a partial match in UEN or company name. An empty or omitted `search` returns the company list.

```json
{
  "items": [
    { "id": 42, "uen": "201234567A", "name": "Example Pte Ltd", "totalOutstandingSgd": 1234.5 }
  ],
  "totalCount": 1
}
```

## Retrieve company details

`GET /api/companies/{id}`

Returns the company fields and its most recent documents, sorted by `createdAt` descending.

```json
{
  "id": 42,
  "uen": "201234567A",
  "name": "Example Pte Ltd",
  "address": "1 Example Road",
  "taxRegistrationNumber": "M12345678X",
  "totalOutstandingSgd": 1234.5,
  "documents": [
    { "id": 17, "refNumber": "INV-17", "sentDate": "2026-10-01T00:00:00Z", "totalAmount": 123.45, "createdAt": "2026-10-02T00:00:00Z" }
  ]
}
```
