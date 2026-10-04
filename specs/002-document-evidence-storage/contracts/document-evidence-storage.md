# Document Evidence Storage Contract

**Status**: Proposed design contract for the durable evidence package and retrieval API.

## Overview

The evidence-storage API is responsible only for persistence and retrieval of the confirmed evidence package, its original source documents, related structured records, provenance, review status, and audit trail. It does not handle document ingestion, extraction, classification, matching rules, or payment approval.

## Resource model

The service persists these entities:

- `EvidencePackage`
- `EvidenceDocument`
- `StructuredRecord`
- `ProvenanceEntry`
- `AuditEvent`

All identifiers are stable server-generated UUIDs; the evidence package ID ties the package's documents, records, and audit events together. See the [data model overview](../data-model.md#overview) for a diagram of how these connect and a glossary of terms.

All routes require an authenticated user (`401` otherwise). Enum values use kebab-case on the wire (for example `pending-review`, `purchase-order-commitment`).

## Operation: Create or update evidence package

`POST /api/evidence-packages`

Creates a new package or a new version when the replacement workflow explicitly chooses a versioned resubmission.

### Request

```json
{
  "caseId": "AP-CASE-2048",
  "reviewStatus": "draft",
  "documents": [
    {
      "documentType": "invoice",
      "sourceReference": "INV-10492",
      "reviewStatus": "draft",
      "storageLocation": "protected://evidence/invoices/INV-10492.pdf",
      "checksum": "sha256:...",
      "records": [
        {
          "recordCategory": "invoice-line",
          "recordType": "amount",
          "rawValue": "1450.00",
          "currentValue": "1495.00",
          "reviewStatus": "draft",
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

`201 Created` with a `Location` header and the full package (same shape as retrieval). Key fields:

```json
{
  "evidencePackageId": "1f8d2b9e-6cf5-4a9a-9a81-7fe2bc145c61",
  "caseId": "AP-CASE-2048",
  "packageType": "invoice",
  "reviewStatus": "draft",
  "version": 1,
  "createdAt": "2026-10-04T14:22:31Z"
}
```

To replace confirmed evidence, include `replacesEvidencePackageId` and `replacementReason`; the new package gets `version + 1` and the prior package becomes `superseded` when the replacement is finalized. Evidence documents may include `sourceDocumentId` to reference the intake source document.

## Operation: Retrieve evidence package

`GET /api/evidence-packages/{evidencePackageId}`

Returns the full package with evidence documents, record sets, provenance, review status, match outcomes, and audit trail. The response must include enough lineage for the full match explanation without re-running extraction. Returns `404` for unknown IDs. Archived packages return `contentRestricted: true` with storage locations and values withheld.

## Operation: Query packages

`GET /api/evidence-packages?caseId={caseId}&documentId={documentId}&sourceReference={sourceReference}&reviewStatus={reviewStatus}`

Supports retrieval for audit, review, and matching workflows. Filters combine with AND. `documentId` matches either the evidence `documentId` or the `sourceDocumentId`. `reviewStatus` matches the package, any document, or any record; an unknown status returns `422`. With no filters, every package is returned; any signed-in user can query during the pilot, and role-based restrictions belong to Feature 17.

## Operation: Finalize package

`POST /api/evidence-packages/{evidencePackageId}/finalize`

Promotes a `draft`, `pending-review`, or `reviewed` package to `confirmed`. Returns `412` from any other state or when a non-rejected document lacks a source reference or storage location, and `409` when confirmed evidence already exists for the same source and case (or when the package being replaced is no longer the current confirmed version). Rejected documents and records are left out of finalization.

### Success response

`200 OK` with the full package. Key fields:

```json
{
  "evidencePackageId": "1f8d2b9e-6cf5-4a9a-9a81-7fe2bc145c61",
  "reviewStatus": "confirmed",
  "finalizedAt": "2026-10-04T14:45:00Z",
  "retentionUntil": "2029-10-04T14:45:00Z"
}
```

## Post-create operations

All return `200 OK` with the full package and append provenance entries and/or audit events; none overwrite prior history. Unknown package or record IDs return `404`; disallowed states return `412`.

| Operation | Body | Allowed when |
|---|---|---|
| `POST /api/evidence-packages/{id}/status` | `reviewStatus` (`draft`, `pending-review`, `reviewed`, `rejected`), optional `recordId`, `reason` | Package is `draft`, `pending-review`, or `reviewed` |
| `POST /api/evidence-packages/{id}/records/{recordId}/corrections` | `newValue`, `reason`, optional `actorType` (`user`/`reviewer`) | Package is not yet final; final evidence requires replacement |
| `POST /api/evidence-packages/{id}/records/{recordId}/verifications` | `verificationStatus`, optional `reason`, `modelVersion`, `schemaVersion` | Package is not `superseded`, `archived`, or `failed` |
| `POST /api/evidence-packages/{id}/match-outcomes` | `matchReviewId`, `outcome` (`matched`, `discrepancy`, `unmatched`), `supportingRecordIds`, optional `reason` | Package is `pending-review`, `reviewed`, or `confirmed`; unknown or absent supporting records set `evidenceComplete: false` |
| `POST /api/evidence-packages/{id}/archive` | none | Package is `confirmed` or `superseded` and `retentionUntil` has passed; archived content and values are withheld on retrieval (`contentRestricted: true`) |

## Error model

Use `application/problem+json` for non-success responses.

| Status | Meaning |
|---|---|
| `400` | Invalid payload or missing required identifiers (`caseId`, documents). |
| `401` / `403` | Access denied or authorization failure. |
| `404` | Package or record not found. |
| `409` | Duplicate final evidence for same source + case without explicit replacement, or the replaced package is no longer current. |
| `412` | The package's current state does not allow the operation (for example finalize from `confirmed`, correct final evidence, archive before `retentionUntil`). |
| `413` | Request exceeds the server request-size limit. |
| `422` | Validation failed: unknown enum values, missing required fields, record category not allowed for the document type, server-only event types, or `confirmed`/`reviewed` status on create. |
| `500` / `5xx` | Persistence or retrieval failure; do not report success. The body includes `evidencePackageId` and `reviewStatus` (`failed` for a create that was not committed, otherwise the unchanged prior status). |

## Security and audit requirements

- No raw invoice content or credentials appear in ordinary logs; failure logs contain only the package ID and exception type.
- All sensitive content remains in protected storage and limited audit channels. Credentials and record values in client-supplied audit text and match reasons are replaced with `[REDACTED]`.
- The audit trail must log the source reference, model or schema version, verification result, user correction, and match outcome.
- A failed commit must not transition to a successful state.
