# Feature Specification: Document Evidence Storage

**Feature Branch**: `002-document-evidence-storage`

**Related Feature Brief**: Feature 08 — Evidence storage

**Created**: 2026-10-04

**Status**: Draft

**Input**: User description: "As an accounts-payable user, I want confirmed invoice, purchase-order, and receipt records retained with their source and review status so that matching is traceable and documents can be retrieved later. Store the original document and structured records with provenance that distinguishes extracted values from user corrections. Keep invoice payable lines separate from purchase-order commitments and receipt/service-acceptance evidence. Maintain an audit event trail for source references, model and schema versions, verification results, user corrections, and match outcomes. Do not put raw invoice contents or credentials in ordinary logs. A failed save must not appear successful. Clarify retention, duplicate handling, draft storage, and the identifiers used to associate records. Keep this feature to persistence and retrieval; do not include ingestion, extraction, classification, matching rules, or payment approval."

## Clarifications

### Session 2026-10-04

- Q: How long should finalized evidence be retained before archival or deletion? → A: 3-year pilot retention; review and extend to the production retention policy before go-live.
- Q: How should the system handle a repeated submission for the same source document or case? → A: Deduplicate final evidence for the same source and case; allow a new version only on explicit replacement or reprocessing.
- Q: How should draft evidence be stored before it is approved for the final evidence set? → A: Keep drafts as separate pending records with an explicit review status and finalization step.
- Q: What identifier model should tie source documents, structured records, and audit events together? → A: Use a case-level evidence package ID plus document and record IDs, with each audit event linked to the changed record or package.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Retain and retrieve a complete evidence package (Priority: P1)

As an accounts-payable user, I want all confirmed invoice, purchase-order, and receipt evidence stored with its source and review status so that I can later retrieve the exact document and record set used in a match.

**Why this priority**: Traceability is the foundation of reliable AP review and dispute handling; without reliable retention, the match cannot be trusted or audited.

**Independent Test**: Create or confirm a document packet containing an invoice, a purchase order, and a receipt, then retrieve it later by document and related record identifiers. Confirm that each stored item remains linked to its source and current review status.

**Acceptance Scenarios**:

1. **Given** an invoice, purchase order, and receipt have all been confirmed for a case, **When** the evidence package is saved, **Then** the original documents and their associated structured records are retained together with their source and review status.
2. **Given** an evidence package has been stored, **When** the user searches by invoice, purchase order, receipt, or case identifier, **Then** they can retrieve the matching document packet and associated records.
3. **Given** a review status has changed, **When** the evidence package is reopened, **Then** the current status is visible alongside the stored source and evidence history.

---

### User Story 2 - Preserve provenance and correction history (Priority: P1)

As an AP reviewer, I want to distinguish extracted values from user corrections and verification results so that I can trust what changed and why the final record is considered valid.

**Why this priority**: Provenance is essential when an invoice line, a purchase-order commitment, or a receipt claim is disputed; reviewers must know which values came from extraction, which were corrected, and which were verified.

**Independent Test**: Save a record where extraction differs from the approved value, then review the stored evidence to confirm that both the original extracted value and the corrected value are preserved with their source and timestamp.

**Acceptance Scenarios**:

1. **Given** an extracted financial value differs from the confirmed value, **When** the corrected record is saved, **Then** both the extracted value and the user-approved value are stored with their provenance and review status.
2. **Given** a verification result is recorded, **When** the evidence package is reviewed, **Then** the verification outcome, version information, and source reference are visible without overwriting the original evidence.
3. **Given** a user correction is made, **When** the audit trail is inspected, **Then** the correction event is recorded as a separate historical action linked to the same record.

---

### User Story 3 - Keep evidence separated and auditable (Priority: P2)

As an AP operations lead, I want invoice payable lines, purchase-order commitments, and receipt/service-acceptance evidence kept separate while still linked to the same case so that matching remains transparent and auditable.

**Why this priority**: Fine-grained distinctions between document types prevent cross-contamination and support defensible decisions when a mismatch is identified.

**Independent Test**: Save invoice, purchase-order, and receipt records for the same supplier case and confirm that the system preserves their separate record categories while retaining clear case-level relationships.

**Acceptance Scenarios**:

1. **Given** invoice payable lines, purchase-order commitments, and receipt evidence are stored for the same case, **When** the records are reviewed, **Then** each category remains distinct and traceable to its original source document.
2. **Given** a match outcome is recorded, **When** the case is audited, **Then** the matching result and the evidence used to reach it can be linked without exposing raw invoice contents in routine logs.
3. **Given** a save operation fails, **When** the user reviews the case, **Then** the system shows a failed or pending state rather than a misleading successful result.

---

### Edge Cases

- A duplicate record is submitted for the same original document or matching case.
- A draft version exists alongside a final confirmed record.
- A record is corrected after it has already been used in a matching decision.
- A save operation fails after part of the evidence is prepared but before the final package is committed.
- A user attempts to retrieve evidence when the source document has been archived or retention policy would restrict access.
- A match outcome is recorded without a matching record or with partial supporting evidence.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST retain each confirmed invoice, purchase-order, and receipt as part of a traceable evidence package that includes the original document and its related structured records.
- **FR-002**: The system MUST store original source documents and structured records as separate but linked artifacts so that the source can be retrieved without losing the extracted or reviewed values.
- **FR-003**: The system MUST preserve provenance for every captured value so that it clearly distinguishes extracted values from user corrections, authoritative changes, and verification outcomes.
- **FR-004**: The system MUST keep invoice payable lines separate from purchase-order commitments and receipt or service-acceptance evidence while still allowing them to be associated with the same case or match review.
- **FR-005**: The system MUST maintain an audit event trail covering source references, model and schema versions, verification results, user corrections, and match outcomes.
- **FR-006**: The system MUST record the review status of each stored document and structured record so that users can distinguish draft, pending-review, reviewed, confirmed, rejected, superseded, and archived states.
- **FR-007**: The system MUST not record raw invoice contents or credentials in ordinary logs; sensitive details MUST remain within protected evidence and audit channels.
- **FR-008**: The system MUST not show a successful save state when persistence fails; failed or incomplete saves MUST remain clearly marked as unsuccessful or pending.
- **FR-009**: The system MUST associate each evidence record with stable identifiers that allow the original document, record set, related case, and audit events to be linked consistently through a case-level evidence package ID, document ID, and record ID model.
- **FR-010**: The system MUST store draft or in-progress evidence as separate pending records with a distinct review status and MUST require an explicit finalization step before the record is treated as approved evidence.
- **FR-011**: The system MUST deduplicate final evidence records for the same original source and case, and MUST allow a new evidence version only when a replacement or reprocessing workflow explicitly marks the resubmission as a new version.
- **FR-012**: The system MUST support retention rules that keep finalized evidence for a 3-year pilot default before any archive or deletion workflow, with a production policy review required before go-live to confirm whether a longer retention period is needed.
- **FR-013**: The system MUST enable retrieval of stored evidence by document identifier, related case, source reference, or review status so that matching and audit review can happen later.
- **FR-014**: The system MUST preserve the relationship between a match outcome and the evidence used to produce it so that downstream review can explain the basis for a match or discrepancy.
- **FR-015**: The system MUST record the event that created or changed a record in a way that supports later review without overwriting prior evidence.
- **FR-016**: The system MUST treat the existing source-document intake models and Gmail/camera acquisition workflows as intake-level contracts that remain valid for ingestion and source capture, while the evidence package layer adds a separate durable provenance and audit model that references those source document IDs rather than replacing them.

### Key Entities

- **Evidence Package**: The complete set of documents and structured records associated with a confirmed invoice, purchase order, or receipt for a single review case.
- **Source Document**: The original invoice, purchase order, receipt, or related file retained as the authoritative physical or digital source for a case.
- **Structured Record**: An extracted or confirmed payable line, purchase-order commitment, or receipt/service-acceptance entry, with its own provenance and review status.
- **Provenance Record**: The history of how a value was created or changed, including source, extraction, correction, verification, and reviewer actions.
- **Audit Event**: A historical record of a material action such as source attachment, model or schema versioning, verification result, user correction, or match outcome.
- **Review Status**: The current lifecycle state of a document or record, such as draft, pending-review, reviewed, confirmed, rejected, superseded, or archived.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can retrieve the stored evidence package for a documented AP case in under 10 seconds for 95% of normal retrieval requests.
- **SC-002**: 100% of finalized evidence packages include source provenance, review status, and an audit trail for the records they contain.
- **SC-003**: 100% of stored invoice, purchase-order, and receipt entries remain separately identifiable while still being traceable to the same case or match review.
- **SC-004**: 100% of failed save attempts are shown as failed or pending, with no successful completion state reported until persistence is confirmed.
- **SC-005**: 95% of reviewed cases can show the full evidence lineage needed to explain a match, correction, or discrepancy without re-running ingestion or extraction.

## Assumptions

- Evidence retention follows a 3-year pilot default for finalized records, with a production policy review required before go-live to confirm any longer retention schedule or archival treatment.
- Duplicate handling uses a consistent source and case identifier so that repeated submissions are recognized and do not create multiple final artifacts unless the workflow explicitly creates a new version.
- Draft and in-progress work is retained as separate pending evidence records with their own review status until an explicit finalization step promotes them to approved evidence.
- Existing application identity and authorization controls limit who can view, change, or archive stored evidence.
- The current source-document intake DTOs and mailbox/camera intake workflows remain valid as intake-level contracts for collecting or importing source records; this feature adds an additional durable evidence package layer that references source document IDs rather than redefining those intake contracts.
- This feature covers persistence, retrieval, provenance, and auditability only; ingestion, extraction, classification, matching rules, and payment approval remain separate responsibilities.
