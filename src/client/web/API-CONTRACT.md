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

## Search documents

`GET /api/documents?search={partialText}`

Searches case-insensitively for a partial match in document reference number, company UEN, or company name. The response uses the same collection envelope and document shape described below.

## Retrieve documents below the configured confidence threshold

`GET /api/documents/below-threshold`

Takes no query or threshold parameter; threshold configuration remains backend-owned. The response uses the same collection envelope and document shape described below.

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

## Retrieve document details

`GET /api/documents/{id}`

Returns the `DocumentDto` fields in lower camel case, with `dueDate` and `createdAt` for list display. The original attachment may be returned as `originalFileUrl` (for example, its Google Drive link) or as `originalFileBase64` with `originalFileContentType`.

```json
{
  "id": 17,
  "refNumber": "INV-17",
  "typeCode": "INV",
  "company": { "id": 42, "uen": "201234567A", "name": "Example Pte Ltd" },
  "sentDate": "2026-10-01T00:00:00Z",
  "dueDate": "2026-10-31T00:00:00Z",
  "totalAmount": 123.45,
  "confidence": 0.72,
  "relatedDocumentNumbers": null,
  "subtotal": 114.3,
  "subtotalTaxRate": 0.08,
  "subtotalTaxAmount": 9.15,
  "documentItems": [],
  "createdAt": "2026-10-02T00:00:00Z",
  "originalFileUrl": "https://drive.google.com/..."
}
```
