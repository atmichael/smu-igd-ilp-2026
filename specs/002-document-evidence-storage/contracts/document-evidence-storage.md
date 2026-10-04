# Document Evidence Storage Contract

**Status**: Proposed design contract for the durable evidence package and retrieval API.

## Overview

The evidence-storage API is responsible only for persistence and retrieval of the confirmed evidence package, its original source documents, related structured records, provenance, review status, and audit trail. It does not handle document ingestion, extraction, classification, matching rules, or payment approval.

## Resource model

The service persists these entities:

- `EvidencePackage`
- `SourceDocument`
- `StructuredRecord`
- `ProvenanceRecord`
- `AuditEvent`

All identifiers are stable server-generated UUIDs, with a case-level package ID used to tie the whole package together.

## Operation: Create or update evidence package

`POST /api/evidence-packages`

Creates a new package or a new version when the replacement workflow explicitly chooses a versioned resubmission.

### Request

```json
{
  "caseId": "AP-CASE-2048",
  "sourceType": "mixed",
  "reviewStatus": "draft",
  "documents": [
    {
      "documentType": "invoice",
      "sourceReference": "INV-10492",
      "reviewStatus": "confirmed",
      "storageLocation": "protected://evidence/invoices/INV-10492.pdf",
      "checksum": "sha256:...",
      "records": [
        {
          "recordCategory": "invoice-line",
          "recordType": "amount",
          "rawValue": "1450.00",
          "currentValue": "1495.00",
          "reviewStatus": "confirmed",
          "provenance": [
            {
              "eventType": "extracted",
              "actorType": "model",
              "previousValue": null,
              "newValue": "1450.00",
              "sourceReference": "ocr/invoice-10492"
            },
            {
              "eventType": "corrected",
              "actorType": "user",
              "previousValue": "1450.00",
              "newValue": "1495.00",
              "reason": "supplier confirmed revised amount"
            }
          ]
        }
      ]
    }
  ],
  "auditEvents": [
    {
      "eventType": "source-attached",
      "actorType": "system",
      "message": "Invoice document retained with package linkage",
      "metadata": {
        "sourceReference": "INV-10492",
        "schemaVersion": "ap-evidence-v1"
      }
    }
  ]
}
```

### Rules

- The request must include a valid `caseId` and must not overwrite a final package for the same source and case without explicit replacement semantics.
- A draft package may be submitted before finalization; a final package requires an explicit finalization step.
- Raw invoice content, credentials, or sensitive fields must remain in protected storage and audit channels; they are not inserted into ordinary logs.
- Failed save operations must return a failed or pending state and must not return a successful completion payload.

### Success response

`201 Created`

```json
{
  "evidencePackageId": "1f8d2b9e-6cf5-4a9a-9a81-7fe2bc145c61",
  "caseId": "AP-CASE-2048",
  "reviewStatus": "draft",
  "createdAt": "2026-10-04T14:22:31Z"
}
```

## Operation: Retrieve evidence package

`GET /api/evidence-packages/{evidencePackageId}`

Returns the full package with source documents, record sets, provenance, review status, and audit trail. The response must include enough lineage for the full match explanation without re-running extraction.

## Operation: Query packages

`GET /api/evidence-packages?caseId={caseId}&documentId={documentId}&sourceReference={sourceReference}&reviewStatus={reviewStatus}`

Supports retrieval for audit, review, and matching workflows.

## Operation: Finalize package

`POST /api/evidence-packages/{evidencePackageId}/finalize`

Promotes a draft or pending package to approved evidence, subject to validation that the package includes the required source documents and record status metadata.

### Success response

```json
{
  "evidencePackageId": "1f8d2b9e-6cf5-4a9a-9a81-7fe2bc145c61",
  "reviewStatus": "confirmed",
  "finalizedAt": "2026-10-04T14:45:00Z"
}
```

## Error model

Use `application/problem+json` for non-success responses.

| Status | Meaning |
|---|---|
| `400` | Invalid payload or missing required identifiers. |
| `401` / `403` | Access denied or authorization failure. |
| `409` | Duplicate final evidence for same source + case without explicit replacement. |
| `412` | Package cannot be finalized from current draft status. |
| `413` | Request exceeds configured storage or payload limits. |
| `422` | Integrity or validation rules failed for records or provenance. |
| `500` / `5xx` | Persistence or retrieval failure; do not report success. |

## Security and audit requirements

- No raw invoice content or credentials appear in ordinary logs.
- All sensitive content remains in protected storage and limited audit channels.
- The audit trail must log the source reference, model or schema version, verification result, user correction, and match outcome.
- A failed commit must not transition to a successful state.
