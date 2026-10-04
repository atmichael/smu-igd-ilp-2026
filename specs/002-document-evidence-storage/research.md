# Research: Document Evidence Storage

## Repository findings

- The repository already defines a small source-document intake flow with a shared `SourceDocumentMetadata` contract and a minimal ASP.NET Core server. This feature is the durable follow-on responsible for evidence retention, retrieval, provenance, and auditability.
- The application architecture already separates client-side UI, backend API, and shared contracts. This feature belongs on the server and shared-contract side; it must not absorb ingestion, OCR, classification, matching logic, or payment review.
- The feature brief and spec clarify the key operational constraints: draft evidence is separate, final evidence is deduplicated by source and case, provenance must be preserved, and negative save states must not be reported as success.
- The repo has a .NET test project already in place (`tests/ILP.Server.Tests`), which is the natural place to add persistence/retrieval contract tests and failure-state checks.
- The material requirements around retention, audit, and review status represent a backend service concern rather than a front-end concern, so the design should remain server-first with a shared contract layer.

## Decisions

### Durable evidence package boundary

**Decision**: Create a server-owned evidence package boundary that persists the original source documents, associated structured records, provenance data, review statuses, and audit events as one package per case and source relationship. Keep the package model separate from the intake and extraction flows.

**Rationale**: The spec requires a single traceable evidence package that can be retrieved later by document, case, source reference, or review status without mixing intake logic with evidence retention. A dedicated package boundary makes deduplication, draft/final state, and auditability explicit.

**Alternatives considered**: Storing only structured records in a single table would lose the original source document and provenance chain; storing everything as unstructured blobs with no normalized records would make review and matching difficult.

### Compatibility with current intake contracts

**Decision**: Preserve the existing source-document intake DTOs and email/camera acquisition workflows as intake-only contracts. The evidence-storage feature will add a new durable evidence package layer that links back to source-document IDs rather than replacing or mutating the current intake models.

**Rationale**: The repository’s current code still models minimal source-document metadata and a Gmail-driven collector prototype, and those workflows are not yet durable evidence records. Keeping them intact avoids breaking the inbox or camera intake paths while still allowing the evidence package to retain package-level provenance and audit information.

**Alternatives considered**: Reusing or repurposing the current source-document DTO as the final evidence model would conflate intake metadata with durable case evidence and would risk breaking the active Gmail/camera workflows.

### Versioned provenance and correction model

**Decision**: Every structured value will preserve both the original extracted value and any later approved or corrected value as separate provenance entries keyed to a stable record ID. Audit events will reference the changed record or package and never overwrite older history.

**Rationale**: The feature explicitly requires distinguishing extraction from correction and verification outcomes. The server must retain both the raw fact and the later authoritative state, plus the event timeline explaining how the final value was reached.

**Alternatives considered**: Overwriting the original value on correction would lose context and fail the auditability requirement. Storing only the current value would make disputes impossible to reconstruct.

### Draft vs. final evidence lifecycle

**Decision**: Draft evidence will be stored as a separate pending package or record subset with a status such as `draft` or `pending-review`, and it will require a formal finalization step before it becomes approved evidence. Final evidence will be deduplicated by source + case unless a replacement or reprocessing workflow creates an explicit new version.

**Rationale**: The spec states that draft work must remain separate and that final evidence deduplication must be enforced. This also prevents the system from treating incomplete saves as successful outcomes.

**Alternatives considered**: Flattening draft and final evidence into one table would blur lifecycle state and increase false-positive match confidence. Allowing duplicate finals would create conflicting evidence in the same case.

### Separation of document categories

**Decision**: Keep three persistent categories—invoice payable lines, purchase-order commitments, and receipt/service-acceptance evidence—distinct in the model and retrieval view while allowing each to be attached to the same case and match review.

**Rationale**: This preserves traceability without cross-contaminating evidence. The system can still link all categories to a common case or review package without implying they are interchangeable.

**Alternatives considered**: Using a single generic evidence table would hide the source-document type and make mismatch explanation harder. A generic record list could be acceptable for storage, but not for audit clarity.

### Secure logging and failure-state handling

**Decision**: The service will sanitize logs and restrict raw invoice contents and credentials to protected evidence and audit channels. A save operation must only transition to success after durable commit confirmation; otherwise the package remains `failed` or `pending`.

**Rationale**: The constitution and feature requirements explicitly forbid raw invoice content and credentials in ordinary logs and require successful state to mean confirmed persistence.

**Alternatives considered**: Logging summaries only is safer than logging raw content, but even summaries must not reveal protected source detail. The persistence pipeline must also maintain a transactional pattern that prevents misleading success states after partial writes.

### Retention and archive policy

**Decision**: Finalized evidence will default to a 3-year pilot retention before archival or deletion, with a production policy review required before go-live. The implementation records `retentionUntil` and supports archive once it has passed (content and values are then withheld on retrieval); deletion is not automated and waits for the production policy review.

**Rationale**: The spec explicitly answers the retention clarification and states the default retention period and policy review requirement. The architecture should therefore keep retention metadata and a future archive workflow hook without forcing a broad implementation in this feature.

**Alternatives considered**: A shorter or unlimited retention period would conflict with the agreed pilot default. Immediate deletion would violate traceability and audit requirements.

### Retrieval and match auditability

**Decision**: Retrieval operations will support lookup by evidence package ID, case ID, document ID (evidence or intake), source reference, and review status, as required by FR-013. Match outcomes will retain a link to the evidence package and supporting record IDs used for the decision so later review can explain the basis for a match or discrepancy.

**Rationale**: The feature must support later audit and review and the match outcome must remain anchored to actual stored evidence rather than transient memory. This keeps the evidence package useful beyond initial intake.

**Alternatives considered**: Returning only the latest record by case would hide lineage and make disputes impossible to resolve. A purely document-centric lookup would miss the review package relationship.

## References

- Repository architecture: [docs/architecture/invoice-ocr-architecture.md](../../docs/architecture/invoice-ocr-architecture.md)
- Feature brief: [docs/planning/feature-briefs/feature-08-document-evidence-storage.md](../../docs/planning/feature-briefs/feature-08-document-evidence-storage.md)
- ASP.NET Core minimal API patterns: existing server project in [src/server/ILP.Server/Program.cs](../../src/server/ILP.Server/Program.cs)
- Shared contract patterns: [src/shared/ILP.Shared/SourceDocuments/SourceDocumentMetadata.cs](../../src/shared/ILP.Shared/SourceDocuments/SourceDocumentMetadata.cs)
