# Data Model: Document Evidence Storage

The data model below supports the durable retention, retrieval, provenance, draft/final lifecycle, and auditability requirements for AP evidence packages.

## EvidencePackage

The top-level grouping for one review case and all related evidence.

| Field | Type | Rules |
|---|---|---|
| `evidencePackageId` | UUID | Stable package identifier for the case-level evidence set. |
| `caseId` | string | Business case identifier the package belongs to. |
| `sourceType` | enum | One of `invoice`, `purchase-order`, `receipt`, or `mixed` for multi-document packages. |
| `status` | enum | `draft`, `pending-review`, `confirmed`, `rejected`, `superseded`, `archived`. |
| `createdAt` | timestamp | Creation time of the package. |
| `updatedAt` | timestamp | Last modification time. |
| `retentionPolicy` | enum/string | Default `pilot-3-year`; can be extended by production policy review. |
| `relatedMatchReviewId` | string? | Optional link to a three-way match or review decision that used this package. |

### Package rules

- A package groups one or more source documents and related structured records for the same case.
- Final packages are deduplicated by source + case unless an explicit replacement or reprocessing workflow creates a new version.
- Draft packages must remain separate and are not treated as approved evidence until finalized.

## SourceDocument

The original invoice, purchase order, receipt, or associated source artifact retained as the authoritative source for a case.

| Field | Type | Rules |
|---|---|---|
| `sourceDocumentId` | UUID | Stable document identifier. |
| `evidencePackageId` | UUID | Parent package identifier. |
| `documentType` | enum | `invoice`, `purchase-order`, `receipt`, `other-evidence`. |
| `sourceReference` | string | Original reference from upstream source (document number, mailbox item ID, or similar). |
| `storageLocation` | string | Protected storage path or object identifier for the original file. |
| `checksum` | string | Integrity hash used to detect tampering or repeats. |
| `reviewStatus` | enum | `draft`, `pending-review`, `reviewed`, `confirmed`, `rejected`, `superseded`, `archived`. |
| `contentHash` | string | Hash of the original file bytes, not the raw content itself. |
| `createdAt` | timestamp | Time the original file was retained. |

### Source-document rules

- Original documents and structured records remain separate artifacts while sharing the same package ID.
- A source document can have multiple related record sets but remains tied to a single package and case.
- Archived or retention-restricted documents must still retain metadata and access constraints without exposing content in ordinary logs.

## StructuredRecord

A structured value extracted from or associated with a source document.

| Field | Type | Rules |
|---|---|---|
| `recordId` | UUID | Stable identifier for the record. |
| `sourceDocumentId` | UUID | Parent document identifier. |
| `recordCategory` | enum | `invoice-line`, `po-commitment`, `receipt-evidence`, `service-acceptance`. |
| `recordType` | string | For example `amount`, `quantity`, `vendor`, `line-item`, `match-key`. |
| `rawValue` | string? | Original extracted or observed value before any user or system correction. |
| `currentValue` | string? | Current approved or corrected value used in the evidence set. |
| `reviewStatus` | enum | `draft`, `pending-review`, `reviewed`, `confirmed`, `rejected`, `superseded`, `archived`. |
| `provenanceVersion` | integer | Monotonic version number for value changes. |
| `sourceReference` | string | Source location or model/schema reference for the value. |
| `modelVersion` | string? | Model or extraction version used for the value. |
| `schemaVersion` | string? | Contract/schema version associated with the record. |
| `verificationStatus` | enum? | `unverified`, `passed`, `failed`, `needs-review`. |
| `matchedEvidenceId` | UUID? | Optional reference to the other record or supporting evidence used in a match. |

### Record rules

- Each record distinguishes raw extracted values from the current approved value.
- The value history is tracked through provenance entries rather than by overwriting the record.
- Invoice payable lines, PO commitments, and receipt/service-acceptance records may coexist in the same package but remain separately categorized.

## ProvenanceRecord

Historical explanation of how a record value changed.

| Field | Type | Rules |
|---|---|---|
| `provenanceId` | UUID | Unique provenance event identifier. |
| `recordId` | UUID | Related record being changed. |
| `evidencePackageId` | UUID | Parent package for traceability. |
| `eventType` | enum | `extracted`, `corrected`, `verified`, `rejected`, `superseded`, `finalized`. |
| `actorType` | enum | `model`, `user`, `system`, `reviewer`. |
| `actorId` | string? | Human or service actor identifier if applicable. |
| `previousValue` | string? | Prior recorded value before the event. |
| `newValue` | string? | Value after the event. |
| `sourceReference` | string | Origin document, model context, or external reference. |
| `timestamp` | timestamp | Event time. |
| `reason` | string? | Explanation for correction or verification outcome. |

### Provenance rules

- Provenance entries are append-only and never overwrite prior history.
- Extraction, human correction, verification, and match outcome events are stored as separate events tied to the same record or package.
- Sensitive invoice content remains inside the protected evidence store and audit channels; ordinary logs do not include it.

## AuditEvent

Historical system action that materially changes evidence state or records a decision.

| Field | Type | Rules |
|---|---|---|
| `auditEventId` | UUID | Unique event identifier. |
| `evidencePackageId` | UUID | Related package. |
| `recordId` | UUID? | Optional record or record set changed. |
| `eventType` | enum | `source-attached`, `model-versioned`, `schema-versioned`, `verification-result`, `user-correction`, `match-outcome`, `finalized`, `rejected`, `archived`. |
| `actorType` | enum | `system`, `model`, `user`, `reviewer`. |
| `actorId` | string? | Actor identity if available. |
| `message` | string | Human-readable audit summary without raw sensitive payloads. |
| `metadata` | JSON | Additional structured metadata such as versions, result codes, and match references. |
| `timestamp` | timestamp | Event time. |

### Audit-event rules

- Each material action is persisted as its own event; no event overwrites the prior evidence trail.
- The system records source references, schema or model versions, verification results, user corrections, and match outcomes.
- Failed saves remain in a failed or pending state and are not reported as successful completions.

## ReviewStatus

The lifecycle state of a document or record.

| Status | Meaning |
|---|---|
| `draft` | In-progress or not ready for approval. |
| `pending-review` | Candidate evidence awaiting a human check. |
| `reviewed` | Reviewed and considered usable. |
| `confirmed` | Approved and retained as final evidence. |
| `rejected` | Not accepted for final evidence. |
| `superseded` | Replaced by a later version or reprocessing event. |
| `archived` | Retention workflow has moved the record to archive status. |

## Relationship summary

- One `EvidencePackage` contains many `SourceDocument` records.
- One `SourceDocument` contains many `StructuredRecord` records.
- One `StructuredRecord` has many `ProvenanceRecord` rows over time.
- One `EvidencePackage` has many `AuditEvent` rows.
- `MatchReview` or downstream decision systems reference the package and evidence IDs, not the transient raw document content.

## Compatibility with existing intake contracts

The existing source-document intake DTOs and the Gmail/camera acquisition workflows remain valid as intake-level contracts for collecting or importing source evidence. This feature does not replace those models with the final evidence package model. Instead, the evidence package references the source-document IDs and their source metadata while adding package-level provenance, review status, and audit history. This keeps the current inbox workflow and intake pipeline stable while preserving the durable evidence model required for traceability.

## State transitions

- `draft` -> `pending-review` -> `confirmed` after explicit finalization.
- A `confirmed` record may become `superseded` when a replacement or reprocessing flow creates a new version.
- Any failed or incomplete persistence operation remains `failed` or `pending` and is never marked `confirmed`.
- Finalized packages support archive/deletion only after retention policy criteria are met.

## Validation rules

- `evidencePackageId`, `sourceDocumentId`, and `recordId` must be unique and stable for their lifecycle.
- `reviewStatus` is required for each document and structured record.
- `final` or `confirmed` evidence cannot be created without an explicit finalization step.
- Duplicate final evidence for the same original document + case must be deduplicated unless replacement is explicit.
- Sensitive data must stay in protected storage; names, raw content, or credentials must not be inserted into ordinary logs.
