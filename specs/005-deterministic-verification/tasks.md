# Tasks: Deterministic Invoice Verification

**Input**: Design documents from `/specs/005-deterministic-verification/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/, quickstart.md

## Phase 1: Setup (Shared Infrastructure)

- [X] T001 Create the verification server feature folders in `src/server/ILP.Server/Features/Verification/` and shared contract folders in `src/shared/ILP.Shared/Verification/`.
- [X] T002 Create frontend verification feature folders in `src/client/web/src/features/verification/`.
- [X] T003 [P] Add verification server test fixture file in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T004 [P] Add verification review endpoint test fixture file in `tests/ILP.Server.Tests/VerificationReviewTests.cs`.

---

## Phase 2: Foundational (Blocking Prerequisites)

- [X] T006 Add shared verification outcome enums (`Pass`, `Fail`, `Unavailable`, `NotApplicable`) in `src/shared/ILP.Shared/Verification/Enums/VerificationOutcome.cs`.
- [X] T007 [P] Add shared verification run status and check type enums in `src/shared/ILP.Shared/Verification/Enums/VerificationEnums.cs`.
- [X] T008 Add shared verification run, check, and calculation trace DTOs in `src/shared/ILP.Shared/Verification/Contracts/VerificationContracts.cs`.
- [X] T009 [P] Add review-item and resolution DTOs in `src/shared/ILP.Shared/Verification/Contracts/VerificationReviewContracts.cs`.
- [X] T010 Add immutable verification run persistence models in `src/server/ILP.Server/Features/Verification/Persistence/VerificationPersistenceModels.cs`.
- [X] T011 Add verification repository interface and implementation using the existing server persistence pattern in `src/server/ILP.Server/Features/Verification/Persistence/IVerificationRepository.cs` and `src/server/ILP.Server/Features/Verification/Persistence/VerificationRepository.cs`.
- [X] T012 Add decimal calculation primitives and rounding policies in `src/server/ILP.Server/Features/Verification/Service/VerificationCalculationPolicy.cs`.
- [X] T013 Add evidence and rule dependency lookup guards in `src/server/ILP.Server/Features/Verification/Service/VerificationDependencyGuard.cs`, returning `Unavailable` without passing missing dependencies.
- [X] T014 Add verification service registration extension in `src/server/ILP.Server/Features/Verification/VerificationServiceCollectionExtensions.cs`.
- [X] T015 [P] Add frontend verification API types in `src/client/web/src/features/verification/model/verification-model.ts`.
- [X] T016 [P] Add frontend verification API client in `src/client/web/src/features/verification/service/verification-service.ts`.
- [X] T017 Add server error handling for invalid inputs and missing evidence references in `src/server/ILP.Server/Features/Verification/Endpoints/VerificationEndpoints.cs`.
- [X] T005 [P] Register verification feature modules in `src/server/ILP.Server/Program.cs` after the verification service contracts and registration extension exist.

**Checkpoint**: Shared contracts, persistence, calculation primitives, dependency guards, and API registration are ready.

---

## Phase 3: User Story 1 - Verify document-specific required fields (Priority: P1) 🎯 MVP

**Goal**: Evaluate evidence-backed required fields for simplified tax invoices, regular tax invoices, and credit notes without using extraction confidence.

**Independent Test**: Verify complete, missing, blank, illegible, unsupported, and S$1,000-threshold simplified-invoice fixtures and confirm field-level outcomes and evidence references.

### Tests for User Story 1

- [X] T018 [P] [US1] Add simplified-invoice eight-field tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T019 [P] [US1] Add required-field evidence, confidence-independence, and threshold tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T020 [P] [US1] Add regular-invoice title and required-field tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.

### Implementation for User Story 1

- [X] T021 [P] [US1] Create required-field rule models for simplified, regular, and credit-note variants in `src/server/ILP.Server/Features/Verification/Service/VerificationRuleModels.cs`.
- [X] T022 [US1] Implement field-presence and evidence validation in `src/server/ILP.Server/Features/Verification/Service/RequiredFieldVerifier.cs`.
- [X] T023 [US1] Implement simplified-invoice threshold and eight-field verification in `src/server/ILP.Server/Features/Verification/Service/SimplifiedTaxInvoiceVerifier.cs`.
- [X] T024 [US1] Implement regular tax-invoice required-field selection in `src/server/ILP.Server/Features/Verification/Service/RegularTaxInvoiceVerifier.cs`.
- [X] T025 [US1] Implement sales-voucher and debit-note simplified-invoice applicability checks in `src/server/ILP.Server/Features/Verification/Service/SimplifiedDocumentVerifier.cs`.
- [X] T026 [US1] Add document-classification rule orchestration that discards stale results when classification changes in `src/server/ILP.Server/Features/Verification/Service/VerificationOrchestrator.cs`.
- [X] T027 [US1] Add start and get verification-run endpoints in `src/server/ILP.Server/Features/Verification/Endpoints/VerificationEndpoints.cs`.
- [X] T028 [US1] Add review-item reason and evidence display in `src/client/web/src/features/verification/components/VerificationCheckList.tsx`.

**Checkpoint**: Required-field verification and blocked review runs are independently functional.

---

## Phase 4: User Story 2 - Verify tax invoice and credit-note requirements (Priority: P1)

**Goal**: Apply regular tax-invoice and credit-note rules, including mixed supplies, zero-rated supplies, original invoice adjustments, and conditional GST exception evidence.

**Independent Test**: Verify representative zero-rated, mixed-supply, rate-change, original-invoice adjustment, and no-GST-adjustment credit-note fixtures.

### Tests for User Story 2

- [X] T029 [P] [US2] Add regular zero-rated and mixed-supply tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T030 [P] [US2] Add credit-note required-field and original-invoice relationship tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T031 [P] [US2] Add credit-note no-GST-adjustment exception evidence tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.

### Implementation for User Story 2

- [X] T032 [P] [US2] Implement zero-rated supply wording and 0% rate checks in `src/server/ILP.Server/Features/Verification/Service/ZeroRatedSupplyVerifier.cs`.
- [X] T033 [P] [US2] Implement separate GST-treatment and gross-amount validation in `src/server/ILP.Server/Features/Verification/Service/MixedSupplyVerifier.cs`.
- [X] T034 [US2] Implement credit-note required-field verification in `src/server/ILP.Server/Features/Verification/Service/CreditNoteVerifier.cs`.
- [X] T035 [US2] Implement original-invoice adjustment and alternative-document evidence checks in `src/server/ILP.Server/Features/Verification/Service/OriginalInvoiceAdjustmentVerifier.cs`.
- [X] T036 [US2] Implement the conditional "This is not a credit note for GST purposes" exception in `src/server/ILP.Server/Features/Verification/Service/CreditNoteGstExceptionVerifier.cs`.
- [X] T037 [US2] Integrate US2 rule engines into `src/server/ILP.Server/Features/Verification/Service/VerificationOrchestrator.cs`.
- [ ] T038 [US2] Add credit-note and mixed-supply evidence presentation in `src/client/web/src/features/verification/components/VerificationDetail.tsx`.

**Checkpoint**: Document-specific tax requirements and credit-note adjustments can be independently tested.

---

## Phase 5: User Story 3 - Verify GST and numeric arithmetic deterministically (Priority: P1)

**Goal**: Verify decimal GST calculations, line extensions, subtotals, totals, numeric formats, rounding, and separate cash rounding without silently adjusting values.

**Independent Test**: Run subtotal and item-by-item fixtures covering valid arithmetic, one-cent post-rounding differences, permitted cash rounding, and unavailable policy.

### Tests for User Story 3

- [X] T039 [P] [US3] Add subtotal-method GST tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T040 [P] [US3] Add item-by-item-method GST tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T041 [P] [US3] Add extension, subtotal, total, unsupported-format, and one-cent difference tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T042 [P] [US3] Add nearest-five-cent cash-rounding separation tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.

### Implementation for User Story 3

- [X] T043 [P] [US3] Implement taxable extensions, subtotal arithmetic, and total arithmetic in `src/server/ILP.Server/Features/Verification/Service/InvoiceArithmeticVerifier.cs`.
- [X] T044 [P] [US3] Implement item-by-item GST calculation in `src/server/ILP.Server/Features/Verification/Service/ItemByItemGstVerifier.cs`.
- [X] T045 [P] [US3] Implement subtotal GST calculation in `src/server/ILP.Server/Features/Verification/Service/SubtotalGstVerifier.cs`.
- [X] T046 [US3] Implement configured cent rounding and prohibited post-rounding tolerance in `src/server/ILP.Server/Features/Verification/Service/GstRoundingVerifier.cs`.
- [X] T047 [US3] Implement supported numeric-format parsing and unsupported-format outcomes in `src/server/ILP.Server/Features/Verification/Service/NumericFormatVerifier.cs`.
- [X] T048 [US3] Implement separate nearest-five-cent cash-rounding calculation trace in `src/server/ILP.Server/Features/Verification/Service/CashRoundingVerifier.cs`.
- [ ] T049 [US3] Persist immutable calculation traces for every applicable arithmetic check in `src/server/ILP.Server/Features/Verification/Persistence/VerificationRepository.cs`.
- [ ] T050 [US3] Add calculation-trace endpoint and output contract in `src/server/ILP.Server/Features/Verification/Endpoints/VerificationEndpoints.cs`.

**Checkpoint**: GST and numeric calculations are deterministic, reproducible, and do not alter source amounts.

---

## Phase 6: User Story 4 - Verify foreign-currency and vendor rules (Priority: P2)

**Goal**: Validate vendor identity and Singapore-dollar conversions against approved effective-date reference data, failing closed as unavailable when dependencies are missing or inconsistent.

**Independent Test**: Test known/unknown vendors, historical/prevailing exchange rates, missing/conflicting rates, and credit-note consistency fixtures.

### Tests for User Story 4

- [X] T051 [P] [US4] Add approved vendor and alias matching tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T052 [P] [US4] Add historical and prevailing foreign-currency conversion tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [X] T053 [P] [US4] Add unavailable/conflicting conversion and credit-note consistency tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.

### Implementation for User Story 4

- [X] T054 [P] [US4] Implement approved vendor identity and alias checks in `src/server/ILP.Server/Features/Verification/Service/VendorIdentityVerifier.cs`.
- [X] T055 [P] [US4] Implement approved exchange-rate lookup and effective-date selection in `src/server/ILP.Server/Features/Verification/Service/ExchangeRateVerifier.cs`.
- [X] T056 [P] [US4] Implement Singapore-dollar GST conversion checks in `src/server/ILP.Server/Features/Verification/Service/ForeignCurrencyGstVerifier.cs`.
- [X] T057 [P] [US4] Implement credit-note currency-policy consistency in `src/server/ILP.Server/Features/Verification/Service/ForeignCurrencyCreditNoteVerifier.cs`.
- [X] T058 [P] Integrate vendor and currency results into the verification run in `src/server/ILP.Server/Features/Verification/Service/VerificationOrchestrator.cs`.
- [X] T059 [P] Display unavailable currency/vendor dependencies and recovery actions in `src/client/web/src/features/verification/components/VerificationDependencyNotice.tsx`.

**Checkpoint**: Vendor and foreign-currency checks are provenance-based and fail closed.

---

## Phase 7: User Story 5 - Route outcomes to review without approving payment (Priority: P2)

**Goal**: Present explicit outcomes, block mixed unavailable runs, preserve audit provenance, allow MVP signed-in resolution, and prohibit payment approval.

**Independent Test**: Exercise pass, fail, unavailable, and not-applicable runs plus signed-in resolution and changed-input re-verification without payment state.

### Tests for User Story 5

- [X] T060 [P] [US5] Add mixed pass and unavailable run-status tests in `tests/ILP.Server.Tests/DeterministicVerificationTests.cs`.
- [ ] T061 [P] [US5] Add signed-in review-resolution audit tests in `tests/ILP.Server.Tests/VerificationReviewTests.cs`.
- [ ] T062 [P] [US5] Add re-verification and stale-run invalidation tests in `tests/ILP.Server.Tests/VerificationReviewTests.cs`.

### Implementation for User Story 5

- [X] T063 [P] [US5] Implement overall run status derivation in `src/server/ILP.Server/Features/Verification/Service/VerificationRunStatusService.cs`.
- [X] T064 [P] [US5] Implement review-item creation and resolution persistence in `src/server/ILP.Server/Features/Verification/Review/VerificationReviewService.cs`.
- [X] T065 [US5] Add signed-in-user resolution endpoint in `src/server/ILP.Server/Features/Verification/Endpoints/VerificationEndpoints.cs`, without role authorization in the MVP.
- [X] T066 [US5] Add provenance-preserving resolution audit entry in `src/server/ILP.Server/Features/Verification/Review/VerificationReviewAuditService.cs`.
- [X] T067 [US5] Implement change-triggered new-run invalidation in `src/server/ILP.Server/Features/Verification/Service/VerificationReevaluationService.cs`.
- [X] T068 [US5] Add verification review page and resolution form to `src/client/web/src/features/verification/VerificationReviewPage.tsx`.
- [ ] T069 [US5] Integrate verification review status into `src/client/web/src/features/document/DocumentDetailsPage.tsx`.
- [ ] T070 [US5] Add frontend tests for accessible loading, error, blocked, resolved, and no-payment states in `src/client/web/src/features/verification/VerificationReviewPage.test.tsx`.
- [X] T078 [US5] Require signed-in authentication on verification review-resolution endpoints and capture the authenticated user from `ClaimsPrincipal` in `src/server/ILP.Server/Features/Verification/Endpoints/VerificationEndpoints.cs`.
- [X] T079 [US5] Persist the authenticated user identifier in review-resolution audit entries in `src/server/ILP.Server/Features/Verification/Review/VerificationReviewAuditService.cs` without trusting frontend user identity.
- [X] T080 [US5] Ensure verification contracts, server endpoints, and review UI expose no payment-approval action or payment-status transition in `src/shared/ILP.Shared/Verification/`, `src/server/ILP.Server/Features/Verification/`, and `src/client/web/src/features/verification/`.
- [ ] T081 [US5] Add an end-to-end test proving successful verification does not change or expose payment status in `tests/ILP.Server.Tests/VerificationReviewTests.cs`.

**Checkpoint**: Reviewable verification results, MVP resolution, and immutable provenance work without payment approval.

---

## Phase 8: Polish & Cross-Cutting Concerns

- [X] T071 [P] Add final contract and API documentation in `specs/005-deterministic-verification/contracts/deterministic-verification.md`.
- [ ] T072 [P] Add deterministic test fixtures and README guidance in `tests/ILP.Server.Tests/DeterministicVerificationFixtures/README.md`.
- [ ] T073 Validate required-field, calculation, currency, credit-note, and review flows against `specs/005-deterministic-verification/quickstart.md`.
- [ ] T074 Run server verification and review tests with `dotnet test tests/ILP.Server.Tests/ILP.Server.Tests.csproj`.
- [ ] T075 Run frontend verification tests with `npm test --prefix src/client/web`.
- [X] T076 Review provenance, naming, sensitive-data logging, and payment-boundary invariants against `.specify/memory/constitution.md`.
- [X] T077 [P] Update feature documentation and implementation status in `specs/005-deterministic-verification/plan.md` and `specs/005-deterministic-verification/quickstart.md`.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; can start immediately.
- **Foundational (Phase 2)**: Depends on Setup completion and blocks every user story.
- **User Stories (Phase 3+)**: Depend on Foundational completion. US1, US2, US3, US4, and US5 are priority-ordered but independently testable once their prerequisites exist; US5 integrates review APIs and UI with earlier outcomes.
- **Polish (Phase 8)**: Depends on the desired user stories being complete.

### User Story Dependencies

- **US1 (P1)**: No dependency on other user stories after the Foundation.
- **US2 (P1)**: Integrates with US1 document-run orchestration but can be implemented and tested with its representative fixtures independently.
- **US3 (P1)**: Uses foundational decimal policies and run/check models, with no dependency on US2.
- **US4 (P2)**: Uses foundational dependency guards and run/check models, independently testable.
- **US5 (P2)**: Uses outcomes and review-check models from all verification stories and is therefore implemented after at least US1 is available.

### Within Each User Story

- Tests are written first and validated as failing where the environment supports TDD.
- Models/contracts precede services.
- Services precede endpoints and UI integration.
- Every story-specific checkpoint requires its independent tests to pass.

---

## Parallel Execution Examples

### User Story 1

```text
T018 Simplified-field tests
T019 Evidence/confidence/threshold tests
T020 Regular-invoice field tests
T021 Required-field rule models
T023 Simplified-tax verifier
T024 Regular-tax verifier
```

### User Story 2

```text
T029 Zero-rated and mixed-supply tests
T030 Credit-note relationship tests
T031 GST-exception tests
T032 Zero-rated verifier
T033 Mixed-supply verifier
T038 Verification detail UI
```

### User Story 3

```text
T039 Subtotal tests
T040 Item-by-item tests
T041 Arithmetic and format tests
T044 Item-by-item verifier
T045 Subtotal verifier
T048 Cash-rounding verifier
```

### User Story 4

```text
T051 Vendor tests
T052 Currency tests
T053 Failure tests
T054 Vendor verifier
T055 Exchange-rate verifier
T059 Dependency UI
```

### User Story 5

```text
T060 Mixed-run tests
T061 Resolution tests
T062 Reverification tests
T063 Run-status service
T064 Review service
T068 Review page
```

---

## Implementation Strategy

### MVP First: User Story 1

1. Complete Phase 1 setup and Phase 2 foundations.
2. Complete User Story 1 required-field verification.
3. Run T018-T020 and the US1 quickstart scenario.
4. Stop and demonstrate evidence-backed simplified, regular, and credit-note required-field blocking.

### Incremental Delivery

1. Foundation + US1: required-field evidence MVP.
2. Add US3: deterministic GST calculations.
3. Add US2: document-specific and credit-note rules.
4. Add US4: vendor and foreign-currency checks.
5. Add US5: review resolution and re-verification.
6. Complete polish and full quickstart validation.

---

## Notes

- Tasks marked `[P]` affect different files and have no dependency on another incomplete task in the same group.
- User Story labels map tasks to spec user journeys.
- All automated financial decisions remain server-side and deterministic.
- A review resolution records a user decision but does not mutate the automated verification result.
- No task approves payment.

---
