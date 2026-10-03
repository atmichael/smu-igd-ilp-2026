---

description: "Actionable implementation tasks for camera document capture"
---

# Tasks: Camera Document Capture

**Input**: Design documents from `/specs/001-camera-capture/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/source-document-intake.md`, `quickstart.md`

**Tests**: Included because the plan specifies Vitest/React Testing Library and .NET integration tests, and the constitution requires feature validation.

**Organization**: Tasks are grouped by user story. Every task has a sequential ID and concrete file path.

## Phase 1: Setup

**Purpose**: Establish the client and server test foundations used by the stories.

- [X] T001 [P] Add Vitest, React Testing Library, and jsdom configuration plus an `npm test` script in `src/client/web/package.json`, `src/client/web/package-lock.json`, and `src/client/web/vite.config.ts`.
- [X] T002 [P] Create the xUnit API integration-test project with a project reference to `src/server/ILP.Server/ILP.Server.csproj`, and add `tests/ILP.Server.Tests/ILP.Server.Tests.csproj` to `ILP.slnx`.

---

## Phase 2: Foundational

**Purpose**: Define shared state and contract prerequisites before story implementation.

- [X] T003 [P] Add source-document metadata types in `src/shared/ILP.Shared/SourceDocuments/SourceDocumentMetadata.cs` with `source=camera-capture`, server-assigned `sourceDocumentId`, `pageCount` constrained to 1-3, and `status=received`.
- [X] T004 [P] Implement the client capture state and transition model in `src/client/web/src/features/camera-capture/camera-capture-model.ts` using the states `idle`, `requestingPermission`, `liveCapture`, `pageReview`, `documentReview`, `submitting`, `submitted`, `recoverableError`, and `cancelled`; enforce zero to three accepted pages, one pending page, and one-based positions from 1 through 3.
- [X] T005 Align `specs/001-camera-capture/contracts/source-document-intake.md` with the shared scanned-upload contract and the Feature 08 storage, Feature 17 authorization, and Feature 18 idempotency interfaces before implementing the endpoint; preserve one shared intake path and do not add camera-specific persistence.
- [X] T006 [P] Configure the ASP.NET Core development CORS policy for the configured HTTPS React origin in `src/server/ILP.Server/Program.cs` and `src/server/ILP.Server/appsettings.Development.json` without allowing arbitrary origins.

**Checkpoint**: Shared metadata, client state, endpoint contract, and API development origin are defined. User-story implementation must use the Feature 08/17/18 service contracts when available; do not substitute local storage or bypass authorization/deduplication.

---

## Phase 3: User Story 1 - Capture an invoice document (Priority: P1) - MVP

**Goal**: Capture, review, order, and explicitly submit one source document containing one to three original camera images.

**Independent Test**: On current stable Safari for iOS/iPadOS and Chrome on Android over HTTPS, capture one and three pages, retake/remove a page, then submit. Verify one `POST /api/source-documents` request contains the accepted JPEG pages in order with `source=camera-capture`, and a successful response returns one source-document ID. Verify cancel sends no request.

### Tests for User Story 1

- [X] T007 [P] [US1] Add client tests in `src/client/web/src/features/camera-capture/CameraCapture.test.tsx` for explicit camera start, JPEG preview, accept/retake/remove, accepted order, the three-page maximum, and confirm-versus-cancel submission behavior.
- [X] T008 [P] [US1] Add API integration tests in `tests/ILP.Server.Tests/SourceDocumentIntakeTests.cs` for multipart source metadata, one-to-three ordered JPEG parts, server-assigned IDs, idempotent same-payload retries, and atomic rejection of invalid content/count/size without partial storage.

### Implementation for User Story 1

- [X] T009 [P] [US1] Implement explicit video-only camera acquisition, rear-camera preference with device fallback, still-frame JPEG `Blob` capture, and page preview in `src/client/web/src/features/camera-capture/CameraCapture.tsx`.
- [X] T010 [US1] Implement document review in `src/client/web/src/features/camera-capture/CameraCapture.tsx` and `src/client/web/src/features/camera-capture/CameraCapture.css`; accept one to three pages, preserve accepted order, renumber one-based positions from 1 through 3 after removal, and prevent adding a fourth page until one is removed.
- [X] T011 [P] [US1] Implement `src/client/web/src/features/camera-capture/source-document-api.ts` to send one `multipart/form-data` request after confirmation with `source=camera-capture`, repeated `pages` parts in accepted order, and one UUID `Idempotency-Key` reused only for retries of the identical payload.
- [X] T012 [P] [US1] Implement `POST /api/source-documents` in `src/server/ILP.Server/Endpoints/SourceDocuments/SourceDocumentsEndpoints.cs`; validate exactly one source value and one to three `image/jpeg` pages by media type, image signature, order, and configured request-size limit; require the app authorization policy; delegate atomic persistence to Feature 08 and deduplication to Feature 18; return `201` with the server-assigned ID only after the full document is stored.
- [X] T013 [US1] Integrate `CameraCapture` with the app shell in `src/client/web/src/App.tsx`, expose a host exit callback that returns to source selection without losing parent-owned work, and show the accepted source-document ID/status after successful submission.

**Checkpoint**: P1 works end to end when the shared intake, evidence-storage, authorization, and deduplication dependencies are available; no OCR or upload-picker behavior is included.

---

## Phase 4: User Story 2 - Recover from camera access or capture problems (Priority: P2)

**Goal**: Explain camera/capture/submission failures and let the user retry where possible or exit safely without submitting partial work.

**Independent Test**: With mocked permissions and camera APIs, exercise denied permission, no camera, hardware/capture failure, interruption, and upload failure. Verify a clear next action, no submission on cancel, recoverable captured pages retained only while the session remains active, prior application work preserved, and active camera tracks stopped when leaving live capture.

### Tests for User Story 2

- [X] T014 [US2] Add recovery tests in `src/client/web/src/features/camera-capture/CameraCapture.test.tsx` for permission denial, unavailable camera, `NotReadableError`, capture interruption, failed upload/retry, safe exit, no partial request, stopped tracks, and revoked preview URLs.

### Implementation for User Story 2

- [X] T015 [US2] Map secure-context, permission-denied, unavailable-device, hardware, and still-capture failures to user-safe recovery states and retry/exit actions in `src/client/web/src/features/camera-capture/CameraCapture.tsx`.
- [X] T016 [US2] Complete lifecycle cleanup in `src/client/web/src/features/camera-capture/CameraCapture.tsx` and `src/client/web/src/features/camera-capture/camera-capture-model.ts`; stop every media track and revoke all preview URLs on retake, removal, cancel, exit, submission completion, and component unmount, while preserving prior app state.

**Checkpoint**: Permission and runtime failures recover safely and cannot submit partial captures.

---

## Phase 5: Polish and Cross-Cutting Validation

**Purpose**: Verify the feature against the supported-device matrix and documented workflow.

- [X] T017 [P] Run the client lint, test, and build commands plus `dotnet test .\tests\ILP.Server.Tests\ILP.Server.Tests.csproj`, execute the HTTPS device scenarios in `specs/001-camera-capture/quickstart.md`, and record device/OS/browser outcomes in that file.

---

## Dependencies and Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies; client and server test tooling can be established in parallel.
- **Foundational (Phase 2)**: Depends on setup; blocks story implementation until the state model, shared metadata, intake contract, and configured client origin exist.
- **User Story 1 (Phase 3)**: Depends on the foundation and on compatible Feature 01 intake, Feature 08 storage, Feature 17 authorization, and Feature 18 idempotency contracts/services.
- **User Story 2 (Phase 4)**: Depends on the shared capture component and state transitions from User Story 1; it does not add a second submission or storage path.
- **Polish (Phase 5)**: Depends on both stories and the external integration dependencies being available for end-to-end verification.

### User Story Dependencies

- **US1 (P1)**: Starts after Phase 2; it is the MVP story.
- **US2 (P2)**: Starts after the US1 camera component/state model is present; recovery can then be tested independently using mocked camera and API failures.

### Parallel Opportunities

- T001 and T002 can run in parallel because they touch separate client and server test infrastructure.
- T003, T004, and T006 can run in parallel after setup; T005 should reconcile the shared API contract before endpoint implementation.
- T007 and T008 can run in parallel because they use different test projects/files.
- T009, T011, and T012 can proceed in parallel after the foundational contract and shared metadata are fixed; T010 follows T009, and T013 integrates the completed client and endpoint.
- US2 begins after US1's capture state/UI is integrated; its failure tests precede its recovery changes.

## Parallel Example: User Story 1

```text
Task: T007 Add client capture and page-order tests in src/client/web/src/features/camera-capture/CameraCapture.test.tsx
Task: T008 Add intake contract tests in tests/ILP.Server.Tests/SourceDocumentIntakeTests.cs

After the shared contract is fixed:
Task: T009 Implement camera stream and still capture in src/client/web/src/features/camera-capture/CameraCapture.tsx
Task: T011 Implement multipart submission in src/client/web/src/features/camera-capture/source-document-api.ts
Task: T012 Implement the shared intake endpoint in src/server/ILP.Server/Endpoints/SourceDocuments/SourceDocumentsEndpoints.cs
```

## Implementation Strategy

### MVP First (User Story 1 Only)

1. Complete setup and foundational tasks.
2. Complete User Story 1 and validate capture, review, order, page cap, and confirmed submission independently.
3. Confirm Feature 08/17/18 integrations before claiming persistence and end-to-end success.
4. Deliver User Story 2 recovery behavior after the core capture journey is stable.

### Incremental Delivery

1. Establish tests, shared metadata, client session model, and intake contract.
2. Deliver US1 as the camera-capture MVP without OCR, classification, or document-picker upload.
3. Add US2 permission/camera/submission recovery while preserving the US1 flow.
4. Complete HTTPS device and API contract validation using the quickstart.

### External Dependencies

- Feature 01 must converge scanned upload on the same source-document intake contract.
- Feature 08 must provide durable ordered-page storage and provenance; do not store images as Base64 or add a camera-specific store here.
- Feature 17 must supply application-wide authorization for the intake endpoint.
- Feature 18 must provide idempotency behavior for retries using the same `Idempotency-Key` and payload.

## Notes

- Every task is a checklist item with a sequential ID, an optional `[P]` only when files can be changed independently, a story label only in story phases, and concrete file paths.
- Automated tests are included because they are specified in the plan; physical-device tests remain manual and require HTTPS.
- No database engine or binary-storage format is selected by this task list.
