# Tasks: Reference Data and Business Rules

**Input**: Design documents from `/specs/004-reference-data-and-rules/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/

**Organization**: Tasks are grouped by user story so each story can be implemented and validated independently.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel when the tasks modify different files and have no incomplete task dependencies.
- **[Story]**: User-story label required for story-phase tasks; setup, foundational, and polish tasks do not use labels.
- Every task includes an exact repository-relative file path.

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the feature namespace and shared contracts without changing production behavior.

- [X] T001 Create the server feature folder structure in `src/server/ILP.Server/Features/ReferenceData/{Adapters,Endpoints,Repository,Service}/`.
- [X] T002 Create the shared contract folder structure in `src/shared/ILP.Shared/ReferenceData/{Dto,Contracts}/`.
- [X] T003 [P] Add the reference-data server test fixture in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement shared persistence and contracts required by every user story.

**CRITICAL**: Complete this phase before starting user-story implementation.

- [X] T004 Add immutable shared reference snapshot, synchronization-run, source, and outcome DTOs in `src/shared/ILP.Shared/ReferenceData/Dto/ReferenceDataContracts.cs`.
- [X] T005 Add reference-record, alias, synchronization-run, snapshot, and audit repository interfaces in `src/server/ILP.Server/Features/ReferenceData/Repository/IReferenceDataRepository.cs`.
- [X] T006 Implement a deterministic file-backed repository with atomic snapshot publication in `src/server/ILP.Server/Features/ReferenceData/Repository/FileReferenceDataRepository.cs`.
- [X] T007 Add source adapter contracts for vendor, purchasing, and policy sources in `src/server/ILP.Server/Features/ReferenceData/Adapters/IReferenceDataSourceAdapter.cs`.
- [X] T008 Register reference-data feature services, repositories, and the reconciliation background service in `src/server/ILP.Server/Features/ReferenceData/ReferenceDataServiceCollectionExtensions.cs`.
- [X] T009 Add deterministic outcome reason codes and unverified recovery guidance in `src/shared/ILP.Shared/ReferenceData/Contracts/ReferenceDataOutcome.cs`.
- [X] T010 [P] Add synchronization-run and snapshot effective-period validators in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceDataSnapshotValidator.cs`.

**Checkpoint**: Shared contracts and atomic local snapshot persistence are ready for all stories.

---

## Phase 3: User Story 1 - Apply Approved Reference Data to Invoice Checks (Priority: P1) MVP

**Goal**: Resolve vendor, purchasing, currency, tax, and format references from approved local snapshots and trace their exact versions.

**Independent Test**: A known ERP vendor alias and purchase reference produce a complete decision trace, while missing required records produce explicit unverified outcomes without false success.

### Tests for User Story 1

- [X] T011 [P] [US1] Add unit tests for vendor aliases, canonical identity, missing records, effective-date selection, unsupported currency, missing currency conversion, and invoice-format applicability in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.
- [X] T012 [P] [US1] Add integration tests for atomic ERP vendor snapshot publication, unavailable-source behavior, and read-only ERP currency/purchasing adapters in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.

### Implementation for User Story 1

- [X] T013 [P] [US1] Implement ERP vendor-master source mapping and alias extraction in `src/server/ILP.Server/Features/ReferenceData/Adapters/ErpVendorSourceAdapter.cs`.
- [X] T014 [P] [US1] Implement ERP purchasing source mapping for purchase orders and receipts in `src/server/ILP.Server/Features/ReferenceData/Adapters/ErpPurchasingSourceAdapter.cs`.
- [X] T015 [US1] Implement reference lookup and effective-date selection for vendor, purchasing, currency, tax, and invoice-format records in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceDataLookupService.cs` (depends on T004-T010).
- [X] T016 [US1] Implement canonical alias resolution without merging distinct identities in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceAliasResolver.cs`.
- [X] T017 [US1] Implement a decision trace that records snapshot, source, record, and rule versions in `src/server/ILP.Server/Features/ReferenceData/Service/RuleDecisionTraceService.cs`.
- [X] T018 [US1] Add reference query and decision-trace endpoints in `src/server/ILP.Server/Features/ReferenceData/Endpoints/ReferenceDataEndpoints.cs`.

**Checkpoint**: User Story 1 independently verifies approved reference resolution and provenance.

---

## Phase 4: User Story 2 - Select the Correct Tax-Document Rule (Priority: P1)

**Goal**: Apply only requirements for the classified tax-document variant and report ambiguity for review.

**Independent Test**: Representative full tax invoice, simplified tax invoice, receipt, debit note, sales voucher, and credit note cases select their own rule package and never substitute another variant.

### Tests for User Story 2

- [X] T019 [P] [US2] Add document-variant and classification ambiguity unit tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.
- [X] T020 [P] [US2] Add effective-period and conflicting-rule-package integration tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.

### Implementation for User Story 2

- [X] T021 [P] [US2] Implement immutable business-rule package DTOs and document-variant validation in `src/shared/ILP.Shared/ReferenceData/Dto/BusinessRulePackage.cs`.
- [X] T022 [US2] Implement deterministic jurisdiction, variant, and effective-date rule selection in `src/server/ILP.Server/Features/ReferenceData/Service/DocumentRuleSelector.cs`.
- [X] T023 [US2] Implement ambiguous-classification handling as `RequiresReview` in `src/server/ILP.Server/Features/ReferenceData/Service/DocumentRuleSelector.cs`.
- [X] T024 [US2] Add rule-package query and variant applicability endpoints in `src/server/ILP.Server/Features/ReferenceData/Endpoints/ReferenceRuleEndpoints.cs`.

**Checkpoint**: User Story 2 independently selects and traces the correct tax-document variant.

---

## Phase 5: User Story 3 - Maintain Traceable Singapore GST Policy (Priority: P1)

**Goal**: Preserve IRAS GST policy, rates, thresholds, rounding, currency conversion, and credit-note adjustments with effective and review metadata.

**Independent Test**: GST cases before and after a policy effective date select different immutable rule versions and missing policy produces an unverified result.

### Tests for User Story 3

- [X] T025 [P] [US3] Add GST threshold, required-field, rounding, and credit-note adjustment tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.
- [X] T026 [P] [US3] Add foreign-currency conversion and policy-effective-date tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.
- [X] T027 [P] [US3] Add IRAS source review metadata, overdue-review reporting, last-reviewed and next-review persistence, and missing-policy tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.

### Implementation for User Story 3

- [X] T028 [P] [US3] Implement the IRAS policy-source adapter for the official IRAS invoicing guidance URL in `src/server/ILP.Server/Features/ReferenceData/Adapters/IRasPolicySourceAdapter.cs`.
- [X] T029 [US3] Implement deterministic GST policy validation, including simplified-invoice threshold S$1,000, rounding, foreign-currency conversion, and credit-note adjustments in `src/server/ILP.Server/Features/ReferenceData/Service/SingaporeGstPolicyValidator.cs`.
- [X] T030 [US3] Implement immutable Singapore GST policy-package publication in `src/server/ILP.Server/Features/ReferenceData/Service/SingaporeGstPolicyService.cs`.
- [X] T031 [US3] Add review metadata persistence and review-status, overdue-review, and next-review endpoints in `src/server/ILP.Server/Features/ReferenceData/Endpoints/PolicySourceEndpoints.cs`.

**Checkpoint**: User Story 3 independently reproduces GST decisions across effective rule versions.

---

## Phase 6: User Story 4 - Report and Govern Reference-Data Changes (Priority: P2)

**Goal**: Safely synchronize sources, publish approved complete snapshots, and audit governed changes and synchronization failures.

**Independent Test**: Update-triggered refresh, daily reconciliation, manual refresh, failed partial synchronization, conflicts, approval, and audit history work through controlled scenarios.

### Tests for User Story 4

- [X] T032 [P] [US4] Add notification-supported, reconciliation-only, update-trigger, daily reconciliation, manual-refresh, and notification-fallback tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.
- [X] T033 [P] [US4] Add partial, unavailable, overlapping-effective-period, retroactive, and conflict synchronization tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.
- [X] T034 [P] [US4] Add requester/approver separation, audit-history, payment-approval absence, and external-authoritative-system read-only boundary tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs`.

### Implementation for User Story 4

- [X] T035 [US4] Implement update-trigger reconciliation and atomic candidate staging in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceDataSynchronizationService.cs`.
- [X] T036 [US4] Implement the daily reconciliation fallback and authorization-gated manual refresh in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceDataReconciliationBackgroundService.cs`.
- [X] T037 [US4] Implement source-update notification handling with adapter capability detection and reconciliation fallback in `src/server/ILP.Server/Features/ReferenceData/Endpoints/ReferenceSourceUpdateEndpoints.cs`.
- [X] T038 [US4] Implement governed change submission and requester/approver separation in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceChangeApprovalService.cs`.
- [X] T039 [US4] Implement complete change and synchronization audit entries in `src/server/ILP.Server/Features/ReferenceData/Service/ReferenceDataAuditService.cs`.
- [X] T040 [US4] Add synchronization status, reconciliation, rule-change, approval, and snapshot endpoints in `src/server/ILP.Server/Features/ReferenceData/Endpoints/ReferenceGovernanceEndpoints.cs`.

**Checkpoint**: User Story 4 independently prevents partial publication and preserves governed change history.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Complete observability, documentation, and validation across all stories.

- [X] T041 [P] Add synchronization, approval, publication, and unverified-outcome structured logging guidance in `docs/architecture/reference-data-and-rules.md`.
- [X] T042 [P] Add the quickstart scenarios for ERP refresh, IRAS policy, atomic publication, conflicts, and decision replay in `specs/004-reference-data-and-rules/quickstart.md`.
- [X] T043 [P] Verify contract names and identifiers against `specs/004-reference-data-and-rules/contracts/reference-data-and-rules.md` and add mismatches to `specs/004-reference-data-and-rules/plan.md` if any exist.
- [X] T044 Run the feature tests in `tests/ILP.Server.Tests/ReferenceDataAndRulesTests.cs` and resolve failures.
- [X] T045 Run `specs/004-reference-data-and-rules/quickstart.md` validation and record evidence in the feature directory.
- [X] T046 Review all required unverified, failed-sync, conflict, effective-period, and provenance paths against the constitution in `specs/004-reference-data-and-rules/plan.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; starts immediately.
- **Foundational (Phase 2)**: Depends on Setup. It blocks all user stories.
- **User Stories (Phase 3-6)**: Depend on Foundational completion and may proceed in priority order or in parallel after their shared contracts are stable.
- **Polish (Phase 7)**: Depends on all selected user stories being complete.

### User Story Dependencies

- **User Story 1 (P1)**: Starts after Foundation; no dependency on other stories.
- **User Story 2 (P1)**: Starts after Foundation; integrates with shared decision trace but remains independently testable.
- **User Story 3 (P1)**: Starts after Foundation; reuses User Story 2 rule-package selection and User Story 1 provenance.
- **User Story 4 (P2)**: Starts after Foundation; reuses repository and provenance foundations and governs publication for all stories.

### Within Each User Story

- Tests are written before implementation and must fail before implementation where executable.
- Shared models precede services.
- Services precede endpoints and integration.
- Story checkpoints require its independent test scenarios to pass before the next story begins.

## Parallel Opportunities

- T003 can run alongside setup tasks because it modifies only a test file.
- T010 can run alongside repository and registration work after the shared contract shape is known.
- In User Story 1, ERP vendor and purchasing adapters and their tests can proceed in parallel.
- In User Story 2, rule DTO and tests can proceed in parallel.
- In User Story 3, source adapter and policy tests can proceed in parallel.
- In User Story 4, notification, reconciliation, audit, and approval tests can proceed in parallel after shared foundations are stable.

## Parallel Example: User Story 1

```text
T011 Contract/unit tests for vendor aliases and missing records
T012 ERP snapshot integration tests
T013 ERP vendor adapter
T014 ERP purchasing adapter
T017 Rule decision trace
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete Setup and Foundation.
2. Complete User Story 1.
3. Validate vendor, purchasing, missing-reference, and provenance behavior independently.
4. Stop before document variants or governance work if the MVP needs early review.

### Incremental Delivery

1. Deliver User Story 1 reference resolution and provenance.
2. Add User Story 2 document variant selection.
3. Add User Story 3 Singapore GST policy.
4. Add User Story 4 synchronization and governance.
5. Finish cross-cutting validation and documentation.

### Scope Assumption

The implementation plan intentionally leaves source integration protocols and persistent storage technology for implementation adapters. The tasks do not add payment approval or write access to external authoritative systems.
