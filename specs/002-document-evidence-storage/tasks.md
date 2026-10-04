# Tasks: Document Evidence Storage

**Input**
: Design documents from `/specs/002-document-evidence-storage/`

**Prerequisites**: plan.md (required), spec.md (required for user stories), data-model.md, contracts/, quickstart.md

**Tests**: The examples below include targeted service and API tests because the spec defines save, retrieval, provenance, deduplication, and failure-state validation requirements.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Establish the evidence package contract, repository surface, and test scaffolding for the backend feature.

- [X] T001 Create the evidence storage structure under `src/shared/ILP.Shared/Evidence/`, `src/server/ILP.Server/Features/EvidenceStorage/`, and `tests/ILP.Server.Tests/`
- [X] T002 [P] Add the shared project references and server dependency wiring in `src/shared/ILP.Shared/ILP.Shared.csproj` and `src/server/ILP.Server/ILP.Server.csproj`
- [X] T003 [P] Create the test harness skeleton in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` for package create/retrieve, provenance, deduplication, and failed-save scenarios

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Define the evidence model layer, validation rules, and repository interface before any story implementation begins.

- [X] T004 Create the core evidence types in `src/shared/ILP.Shared/Evidence/EvidencePackage.cs`, `SourceDocument.cs`, `StructuredRecord.cs`, `ProvenanceRecord.cs`, and `AuditEvent.cs` with the required identifiers, review-status enums, and retention metadata from `specs/002-document-evidence-storage/data-model.md`
- [X] T005 [P] Add the shared request/response DTOs for package creation, retrieval, finalization, and query filters in `src/shared/ILP.Shared/Evidence/EvidenceContracts.cs`
- [X] T006 [P] Implement the repository contract and validation surface in `src/server/ILP.Server/Features/EvidenceStorage/IEvidenceRepository.cs` and `src/server/ILP.Server/Features/EvidenceStorage/EvidenceValidation.cs`
- [X] T007 Implement the in-memory repository and deduplication logic in `src/server/ILP.Server/Features/EvidenceStorage/InMemoryEvidenceRepository.cs` using package, document, record, and audit identifiers plus the draft/final distinction
- [X] T008 Add sensitive-data guardrails and no-raw-log behavior in `src/server/ILP.Server/Features/EvidenceStorage/SensitiveDataGuard.cs` so invoice content and credentials remain protected while audit metadata remains available

**Checkpoint**: Foundation ready - user story implementation can now begin in parallel.

---

## Phase 3: User Story 1 - Retain and retrieve a complete evidence package (Priority: P1) 🎯 MVP

**Goal**: Persist a case-level evidence package with its source documents and related structured records, then retrieve it by package, case, document, or status.

**Independent Test**: Save a mixed invoice + purchase order + receipt package and confirm it can be retrieved later by package ID and case/document references without losing source metadata or review status.

### Tests for User Story 1

- [X] T009 [P] [US1] Add create/retrieve contract tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` for `POST /api/evidence-packages` and `GET /api/evidence-packages/{id}`
- [X] T010 [P] [US1] Add query-by-case and query-by-document tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` to validate retrieval by case ID, source reference, and review status

### Implementation for User Story 1

- [X] T011 [P] [US1] Create the evidence endpoints in `src/server/ILP.Server/Endpoints/EvidencePackages/EvidencePackagesEndpoints.cs` with create, retrieve, and query routes
- [X] T012 [US1] Implement the create-package workflow in `src/server/ILP.Server/Features/EvidenceStorage/EvidencePackageService.cs` with required document and record linkage and explicit finalization gating
- [X] T013 [US1] Implement the retrieval and query workflow in `src/server/ILP.Server/Features/EvidenceStorage/EvidencePackageService.cs` to return the full package, source metadata, record sets, and status fields
- [X] T014 [US1] Register the evidence service and endpoints in `src/server/ILP.Server/Program.cs` so the API is reachable under the existing ASP.NET Core middleware stack

**Checkpoint**: At this point, User Story 1 should be fully functional and independently testable.

---

## Phase 4: User Story 2 - Preserve provenance and correction history (Priority: P1)

**Goal**: Distinguish raw extracted values from corrected values and retain an append-only provenance and audit trail for each record.

**Independent Test**: Save a record where extraction differs from the reviewed value and verify the raw value, corrected value, provenance events, and audit trail are all retained without overwriting history.

### Tests for User Story 2

- [X] T015 [P] [US2] Add provenance-preservation tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` to assert raw value, corrected value, sourceReference, and timestamp metadata remain visible
- [X] T016 [P] [US2] Add audit event tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` covering model/schema versions, verification results, and user corrections as separate historical actions

### Implementation for User Story 2

- [X] T017 [P] [US2] Add provenance and audit metadata fields to `src/shared/ILP.Shared/Evidence/StructuredRecord.cs` and `src/shared/ILP.Shared/Evidence/ProvenanceRecord.cs` to preserve append-only change history
- [X] T018 [US2] Implement provenance recording and correction-version logic in `src/server/ILP.Server/Features/EvidenceStorage/EvidenceProvenanceService.cs`
- [X] T019 [US2] Implement audit-event creation for extraction, user correction, verification, and match milestones in `src/server/ILP.Server/Features/EvidenceStorage/EvidenceAuditService.cs`
- [X] T020 [US2] Connect provenance and audit outputs to the package service in `src/server/ILP.Server/Features/EvidenceStorage/EvidencePackageService.cs` so the package response includes lineage without overwriting prior values

**Checkpoint**: At this point, User Story 2 should be independently testable and compatible with User Story 1.

---

## Phase 5: User Story 3 - Keep evidence separated and auditable (Priority: P2)

**Goal**: Keep invoice payable lines, PO commitments, and receipt/service-acceptance evidence separate while retaining shared case traceability, deduplication, draft/final states, and failed-save handling.

**Independent Test**: Save invoice, PO, and receipt evidence for the same case and confirm they stay in separate record categories while the package remains linked to a shared case and a failed save is never shown as successful.

### Tests for User Story 3

- [X] T021 [P] [US3] Add duplicate and finalization tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` for repeated source+case submissions and explicit replacement workflows
- [X] T022 [P] [US3] Add failed-save and status-transition tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` for `failed`, `pending`, and `confirmed` states without success reporting after commit failure
- [X] T023 [P] [US3] Add match-outcome linkage tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` that confirm a match decision is linked to the evidence package and supporting records without exposing raw content in logs

### Implementation for User Story 3

- [X] T024 [P] [US3] Add explicit package and record lifecycle status models in `src/shared/ILP.Shared/Evidence/EvidenceStatus.cs` and `src/shared/ILP.Shared/Evidence/ReviewStatus.cs` for draft, pending-review, reviewed, confirmed, rejected, superseded, and archived values
- [X] T025 [US3] Implement deduplication, replacement-version handling, and finalization validation in `src/server/ILP.Server/Features/EvidenceStorage/EvidencePackageService.cs`
- [X] T026 [US3] Implement retention-policy and archive gating for the 3-year pilot default in `src/server/ILP.Server/Features/EvidenceStorage/EvidenceRetentionPolicy.cs`
- [X] T027 [US3] Add match-linkage and failure-state persistence handling in `src/server/ILP.Server/Features/EvidenceStorage/EvidenceMatchLinkService.cs` so audit history can explain a match or discrepancy without exposing raw invoice content

**Checkpoint**: All user stories should now be independently functional and ready for review.

---

## Phase 6: Polish & Cross-Cutting Concerns

**Purpose**: Finalize the feature, validate the operational path, and make sure the implementation matches the quickstart and repository conventions.

- [X] T028 [P] Run the evidence storage validation scenarios from `specs/002-document-evidence-storage/quickstart.md` against the API and server tests to confirm the end-to-end flow
- [X] T029 [P] Add final contract and documentation cleanup in `readme.md` and `docs/architecture/invoice-ocr-architecture.md` so the evidence storage boundary remains clearly separated from ingestion, extraction, and matching responsibilities
- [X] T030 [P] Refactor cross-cutting validation and error handling across `src/server/ILP.Server/Program.cs` and the evidence feature files so failed saves stay pending/failed and no successful state is emitted prematurely
- [X] T031 [US3] Add retention archive/deletion workflow in `src/server/ILP.Server/Features/EvidenceStorage/EvidenceRetentionPolicy.cs` for the 3-year pilot default and production policy review before go-live
- [X] T032 [P] Add performance and evidence-lineage regression tests in `tests/ILP.Server.Tests/EvidenceStorageTests.cs` to validate package retrieval under 10 seconds for 95% of normal requests and preserve full lineage without re-running ingestion

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies - can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion - blocks all story work
- **User Story 1 (Phase 3)**: Depends on Foundational completion - MVP deliverable
- **User Story 2 (Phase 4)**: Depends on Foundational completion and can run alongside Story 1 when needed
- **User Story 3 (Phase 5)**: Depends on Foundational completion and can run in parallel with Story 1/2 once the core package contract exists
- **Polish (Phase 6)**: Depends on all desired stories being complete

### User Story Dependencies

- **User Story 1 (P1)**: No dependencies on other stories; this is the MVP path
- **User Story 2 (P1)**: Depends on the same core evidence package contract and can be developed in parallel with Story 1 after the foundation is ready
- **User Story 3 (P2)**: Depends on the shared evidence package contract and status model, but should remain independently testable

### Parallel Opportunities

- Setup tasks T001-T003 can run in parallel
- Foundational tasks T005-T008 can run in parallel once T004 establishes the shared model structure
- User Story 1 tests T009-T010 can run in parallel
- User Story 2 tests T015-T016 can run in parallel
- User Story 3 tests T021-T023 can run in parallel
- All polish tasks T028-T030 can run in parallel after story completion

---

## Parallel Example: User Story 1

```bash
# Launch package creation and retrieval tests together
Task: "Add create/retrieve contract tests in tests/ILP.Server.Tests/EvidenceStorageTests.cs"
Task: "Add query-by-case and query-by-document tests in tests/ILP.Server.Tests/EvidenceStorageTests.cs"

# Launch endpoint and service work together
Task: "Create the evidence endpoints in src/server/ILP.Server/Endpoints/EvidencePackages/EvidencePackagesEndpoints.cs"
Task: "Implement the create-package workflow in src/server/ILP.Server/Features/EvidenceStorage/EvidencePackageService.cs"
```

---

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational
3. Complete Phase 3: User Story 1
4. Stop and validate the API via `tests/ILP.Server.Tests/EvidenceStorageTests.cs`
5. Treat User Story 1 as the minimum viable proof of durable evidence persistence and retrieval

### Incremental Delivery

1. Foundation ready -> add User Story 1
2. Add provenance and correction history -> verify Story 2 independently
3. Add separation, deduplication, and failure-state logic -> verify Story 3 independently
4. Run cross-cutting validation and polish after all stories are green

### Parallel Team Strategy

With multiple developers:

1. One person completes Setup + Foundational together
2. Developer A focuses on Story 1 persistence and retrieval
3. Developer B focuses on Story 2 provenance and audit trail
4. Developer C focuses on Story 3 separation, deduplication, and retention logic
5. Final polish and contract validation occur after story completion

---

## Notes

- [P] tasks represent different files or independent validation activities with no required ordering
- [Story] labels map tasks to the user stories from `specs/002-document-evidence-storage/spec.md`
- Each story remains independently completable and testable
- Storage safety and privacy constraints are non-negotiable requirements from the feature spec and constitution
- All tasks intentionally keep the feature inside persistence, retrieval, provenance, and auditability, excluding extraction, classification, matching rules, and payment approval
