---

description: "Actionable implementation tasks for scanned document upload"
---

# Tasks: Scanned Document Upload

**Input**: Design documents from `/specs/003-scanned-document-upload/`

**Prerequisites**: `plan.md`, `spec.md`, `research.md`, `data-model.md`, `contracts/shared-intake-changes.md`, `quickstart.md`

**Tests**: Included because the constitution requires every feature to define validation (Principle VI), the plan specifies .NET integration tests and Vitest/React Testing Library, and the existing test projects already use them. No new packages are added.

**Organization**: Tasks are grouped by user story. Every task has a sequential ID and a concrete file path. The server side of `file-upload` already exists (one `file`, PDF/JPEG/PNG, signature match, readable PDF, idempotency, `GET .../file`), so tasks only add what is missing.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependency on an incomplete task)
- **[Story]**: User story the task belongs to (US1, US2, US3)

## Phase 1: Setup

**Purpose**: Make the client API module neutral so the new feature does not import from `camera-capture`.

- [X] T001 Move `src/client/web/src/features/camera-capture/source-document-api.ts` to `src/client/web/src/features/source-documents/source-document-api.ts` without behavior changes, update its two importers (`src/client/web/src/features/camera-capture/CameraCapture.tsx` and `src/client/web/src/App.tsx`), and run `npm test` in `src/client/web` to confirm the camera-capture tests still pass.

---

## Phase 2: Foundational

**Purpose**: Shared limit, origin types, client request type, and contract edits that every story needs.

- [X] T002 [P] Add `src/shared/ILP.Shared/SourceDocuments/Constant/SourceDocumentLimits.cs` with `public const long FileUploadMaxBytes = 20 * 1024 * 1024;` (20,971,520 bytes; applies to `file-upload` only).
- [X] T003 [P] Add `src/shared/ILP.Shared/SourceDocuments/Dto/SourceDocumentOriginDto.cs` containing `SourceDocumentOriginDto` with an optional `UploadOriginDto? Upload` and `UploadOriginDto` with `string OriginalFileName`; per the data model, `originalFileName` is the final path segment only, control and Unicode format characters removed (categories Cc and Cf, including bidirectional overrides such as U+202E), trimmed, at most 255 characters, never empty.
- [X] T004 Add an optional `SourceDocumentOriginDto? Origin` property to `src/shared/ILP.Shared/SourceDocuments/Dto/SourceDocumentMetadataDto.cs` marked to be omitted (not written as `null`) when null, so `camera-capture` responses and stored records keep their current shape and existing records still load.
- [X] T005 [P] Edit `specs/001-camera-capture/contracts/source-document-intake.md` as listed in `specs/003-scanned-document-upload/contracts/shared-intake-changes.md`: state the 20 MB (20 x 1024 x 1024 bytes) `file-upload` limit and the empty-file `400`, replace "origin.upload.originalFileName is not recorded yet" with the recording rule, change the `origin` response row from "Planned" to present for `file-upload` and omitted for `camera-capture`, update the `file-upload` rows of the Errors table, and keep the camera-capture and mailbox limits unchanged. Do not remove the "upload screen is not built yet" wording until T028.
- [X] T006 Generalize `src/client/web/src/features/source-documents/source-document-api.ts` (after T001): make the request a discriminated union (`camera-capture` with `pages: Blob[]`, `file-upload` with `file: File`), send one `file` part for uploads with no `pages` parts, keep the camera behavior and its three-page check, extend `SourceDocumentResponse` with `mediaType`, `receivedAt`, and optional `origin?: { upload?: { originalFileName: string } }`, and replace the thrown `Error(title)` with a `SourceDocumentRequestError` carrying `status`, `title`, and `detail` (a thrown network failure stays distinguishable).

**Checkpoint**: Limit constant, origin DTOs, contract text, and a channel-agnostic client request are in place. Camera capture behavior is unchanged.

---

## Phase 3: User Story 1 - Upload an invoice file (Priority: P1) - MVP

**Goal**: Select one PDF or image, see the quick checks, submit it, and get a confirmation with the source document ID, "Upload another", and view/download of the original; the server keeps the original unchanged and records the display file name.

**Independent Test**: Run the API and web client. Submit a valid multi-page PDF, then a JPEG and a PNG. Each shows a confirmation with its source document ID, `channel` is `file-upload`, the PDF has its page count, `origin.upload.originalFileName` holds the name, and the downloaded original is byte-identical. A camera-capture submission still has no `origin`.

### Tests for User Story 1

- [X] T007 [P] [US1] Add API integration tests in `tests/ILP.Server.Tests/SourceDocumentIntakeTests.cs`: a `file-upload` response contains `origin.upload.originalFileName`; a path-like name (`..\..\evil.pdf`, `a/b/invoice.pdf`) is recorded as its final segment only; control characters and U+202E are removed and a name longer than 255 characters is cut to 255; a part with no usable name has no `origin`; a `camera-capture` response has no `origin` property; `GET /api/source-documents/{id}/file` returns bytes identical to the uploaded PDF and PNG with the stored `mediaType` and the response `sourceDocumentId` stays usable for later features such as Feature 04 (FR-009, FR-010); and no stored path or record field contains the client file name.
- [X] T008 [P] [US1] Add client tests in `src/client/web/src/features/file-upload/FileUpload.test.tsx` for: choosing a file shows its name as plain text and passing quick checks before anything is sent; replacing or clearing the selection sends only the replacement; confirming submits exactly one multipart request with `channel=file-upload`, one `file` part, and an `Idempotency-Key`; the loading state shows during submission; the confirmation shows the source document ID and offers "Upload another" and a view/download action; and "Upload another" returns to an empty selection.

### Implementation for User Story 1

- [X] T009 [US1] In `src/server/ILP.Server/Features/EvidenceStorage/Endpoints/SourceDocumentsEndpoints.cs`, read `files[0].FileName` in `ReadFileUploadAsync`, sanitize it (final segment after splitting on `/` and `\`, control and Unicode format characters removed (categories Cc and Cf, including bidirectional overrides such as U+202E), trimmed, at most 255 characters, omitted when empty), carry it on `IntakeContent`, and set `Origin.Upload.OriginalFileName` on the metadata for `file-upload` only. Do not use the name for type detection, content-store names (`original.pdf`, `page-1.<ext>`), the idempotency payload hash, or any log message.
- [X] T010 [P] [US1] Create `src/client/web/src/features/file-upload/file-upload-model.ts` with `FILE_UPLOAD_MAX_BYTES = 20 * 1024 * 1024`, the accepted extensions `.pdf`, `.jpg`, `.jpeg`, `.png` (case-insensitive), `checkSelectedFile(file)` returning pass or a reason for an unsupported extension (listing the accepted types), a file over 20 MB (naming the 20 MB limit), or an empty file, and the attempt state and transitions `idle`, `selected`, `blocked`, `submitting`, `received`, `rejected`, `failed` from the data model. The idempotency key is created when a file is selected and replaced on a new selection or after success. Do not use the file name to decide anything beyond the advisory extension check.
- [X] T011 [US1] Create `src/client/web/src/features/file-upload/FileUpload.tsx` using MUI components: a labelled file input with `accept="application/pdf,image/jpeg,image/png"`, no `multiple`, and no `capture` attribute; show the selected name and the quick-check result; allow replace and clear; a Submit button that is disabled while quick checks fail or while submitting; a visible loading state; keyboard-operable controls with visible focus; and a layout that works at a 320 CSS pixel viewport width with no horizontal scrolling and controls at least 44 × 44 CSS pixels. Render the file name and server-supplied text as plain text only.
- [X] T012 [US1] In `src/client/web/src/features/source-documents/source-document-api.ts` add `fetchOriginalFile(sourceDocumentId)` that calls `GET /api/source-documents/{id}/file` with the same development credentials as intake and returns a `Blob`; then in `src/client/web/src/features/file-upload/FileUpload.tsx` add the received state that shows the source document ID, an "Upload another" action, and a view/download action that opens or saves the blob through an object URL, revokes the URL when replaced or on unmount, and shows an error message if the fetch fails.
- [X] T013 [US1] Integrate in `src/client/web/src/App.tsx`: add an "Upload a file" action beside "Start camera capture" on the source-selection screen, render `FileUpload` with a `submitSourceDocument` call using the `file-upload` request, provide an exit callback that returns to source selection, and make the shared confirmation neutral about the channel (it is shown for both camera capture and upload without losing the existing camera flow).

**Checkpoint**: A valid PDF or image can be uploaded end to end, the original is retrievable and unchanged, and the display name is recorded.

---

## Phase 4: User Story 2 - Understand and recover from a rejected file (Priority: P1)

**Goal**: Every unsupported, mismatched, unreadable, empty, or oversized file is rejected with a specific plain-language reason and next action, with nothing stored and the user kept in place.

**Independent Test**: Attempt a `.txt` file, a text file renamed `.pdf`, a password-protected PDF, a damaged PDF, an empty file, a file of exactly 20 MB, and a file one byte over 20 MB. Each rejection shows its reason and a next action; no source document is created for any rejected file; the exactly-20-MB file is accepted.

### Tests for User Story 2

- [X] T014 [US2] Add API integration tests in `tests/ILP.Server.Tests/SourceDocumentIntakeTests.cs`: an empty `file` part returns `400`; a file of exactly 20 x 1024 x 1024 bytes is accepted; one byte more returns `413` as `application/problem+json` stating the 20 MB limit; a request over the framework request limit also returns `413` as `application/problem+json`; a `camera-capture` request larger than 20 MiB is still accepted (limit unchanged); mismatched content, a password-protected PDF, and a damaged PDF return `415`; an unauthenticated upload returns `401`; and in every rejected case no source document record or content exists afterward.
- [X] T015 [US2] Add client tests in `src/client/web/src/features/file-upload/FileUpload.test.tsx`: a `.txt` file is blocked with the list of accepted types and cannot be submitted; a zero-byte file and a file of 20 x 1024 x 1024 + 1 bytes are blocked (the latter naming the 20 MB limit); a file of exactly 20 x 1024 x 1024 bytes passes; the picker has no `multiple` attribute; server `415`, `413`, and `400` responses display their `detail` with a "Choose another file" action and keep the user on the screen; a `401` explains that upload is unavailable and keeps the selection; no message contains a storage path; and leaving the screen before confirming sends no request.

### Implementation for User Story 2

- [X] T016 [US2] In `src/server/ILP.Server/Features/EvidenceStorage/Endpoints/SourceDocumentsEndpoints.cs`, in `ReadFileUploadAsync` before `ReadBytesAsync`, return `400` ("Empty file.", detail "Choose a file that is not empty.") when the `file` part length is 0 and `413` as `application/problem+json` (detail "The file is too large. The limit is 20 MB.") when its length exceeds `SourceDocumentLimits.FileUploadMaxBytes`; apply these checks to `file-upload` only so camera-capture and mailbox limits do not change.
- [X] T017 [US2] In `src/server/ILP.Server/Features/EvidenceStorage/Endpoints/SourceDocumentsEndpoints.cs`, catch the `BadHttpRequestException` with status 413 that `ReadFormAsync` throws when the whole request exceeds the framework limit and return the same `application/problem+json` 413 with the user-safe 20 MB message for `file-upload`; the response exposes no paths or exception details.
- [X] T018 [US2] In `src/client/web/src/features/file-upload/file-upload-model.ts` add `describeSubmissionFailure(error)` mapping `SourceDocumentRequestError` and network failures to the user-facing message and next action in the status table of `specs/003-scanned-document-upload/contracts/shared-intake-changes.md`: prefer the server `detail` for `400`, `413`, and `415` with the fixed fallback text when absent; fixed text for `401`, `409`, `5xx`, and network errors; every result names a next action and never includes storage paths or exception text.
- [X] T019 [US2] In `src/client/web/src/features/file-upload/FileUpload.tsx` show blocked and rejected states with the message from `describeSubmissionFailure`, an icon and text so status never relies on color alone, `role="alert"` or an equivalent live-region announcement, a "Choose another file" action that returns to the selection state, and no loss of the user's place; do not store or submit anything for blocked files.

**Checkpoint**: Every rejection path is explained and leaves nothing stored.

---

## Phase 5: User Story 3 - Retry safely after a network problem (Priority: P2)

**Goal**: A retry after an unknown outcome returns the original result without creating a duplicate; a reused key with different content is not accepted.

**Independent Test**: Submit a file and simulate a lost response; retry the identical submission and confirm exactly one source document exists and the retry returns the original result. Retry after a storage failure and after a key conflict and confirm the specified behavior.

### Tests for User Story 3

- [X] T020 [US3] Add API integration tests in `tests/ILP.Server.Tests/SourceDocumentIntakeTests.cs` for `file-upload`: the same `Idempotency-Key` and same bytes returns `201` with the original `sourceDocumentId` and exactly one stored document, including when the second request has a different file name; the same key with different bytes returns `409` and changes nothing stored; and a storage failure followed by a retry with the same key is processed as a new attempt and succeeds.
- [X] T021 [US3] Add client tests in `src/client/web/src/features/file-upload/FileUpload.test.tsx` for the key lifetime: the key is created when a file is selected; the same key is sent on a retry after a network error and after a `5xx` with the selection kept; a new key is used after choosing a different file and after a successful submission; a `409` shows the reconcile message and requires choosing the file again without an automatic retry.

### Implementation for User Story 3

- [X] T022 [US3] In `src/client/web/src/features/file-upload/FileUpload.tsx` and `src/client/web/src/features/file-upload/file-upload-model.ts`, add the `failed` state with a "Try again" action that resubmits the same file with the same `Idempotency-Key`, keep the selection and the key after network and `5xx` failures, clear the key and require reselection after a `409`, and never retry automatically. The server already enforces idempotency; no endpoint change is needed.

**Checkpoint**: Lost-response retries produce exactly one source document.

---

## Phase 6: Polish and Cross-Cutting Validation

**Purpose**: Privacy, accessibility, browser coverage, and documentation status.

- [X] T023 Add a test in `tests/ILP.Server.Tests/SourceDocumentIntakeTests.cs` that captures server log output through a test logger provider while submitting an accepted upload and rejected uploads (unsupported type, oversize, mismatched content) with a distinctive file name and a distinctive content marker, and asserts neither appears in any log entry or problem response; then review `src/server/ILP.Server/Features/EvidenceStorage/Endpoints/SourceDocumentsEndpoints.cs` for any remaining statement that could log them (FR-017, Principle V).
- [X] T024 Run the automated checks from `specs/003-scanned-document-upload/quickstart.md`: `dotnet test tests/ILP.Server.Tests`, then in `src/client/web` run `npm test`, `npm run lint`, and `npm run build`; fix failures.
- [ ] T025 Execute manual scenarios 1-14 from `specs/003-scanned-document-upload/quickstart.md` (including the keyboard-only run and the WCAG 2.2 AA contrast check) against the running API and web client, and fix defects found in `src/client/web/src/features/file-upload/FileUpload.tsx`.
- [ ] T026 Run the browser and device matrix from `specs/003-scanned-document-upload/quickstart.md` (scenarios 1, 2, 4, 6, and 8) on current stable Chrome, Edge, Firefox, and Safari on desktop, Safari on iOS/iPadOS, and Chrome on Android; confirm the picker does not force the camera, there is no horizontal scrolling at a 320 CSS pixel viewport width, and controls are at least 44 × 44 CSS pixels.
- [X] T027 [P] Confirm `src/client/web/src/features/file-upload/FileUpload.tsx` uses MUI components and the theme from `src/client/web/src/main.tsx` with no feature-specific palette or typography, so it can be aligned when the design-system document is published (Principle VII).
- [X] T028 Update `specs/001-camera-capture/contracts/source-document-intake.md` status line to remove "the upload screen is not built yet", and update the Feature 01 status in `docs/planning/feature-briefs/feature-brief-index.md` (status map and Feature Status list) to match the verified result of T024-T026.

---

## Phase 7: Drag and drop (added after the first implementation)

**Purpose**: Optional drop shortcut on devices with a mouse or trackpad (FR-019). Choosing a file remains the only route on phones and tablets.

- [X] T029 [US1] In `src/client/web/src/features/file-upload/FileUpload.tsx` accept one dropped file on the upload screen using the same `selectFile` quick checks as a chosen file and never submit on drop; cancel the default for any file drag (also after the document was received or while submitting, where the drop is ignored) so the browser never opens the file; show "Only one file can be uploaded at a time. Drop a single file." for several files and keep the current selection; highlight the screen while a file is dragged over it; show the drag hint only when `(hover: hover) and (pointer: fine)` matches.
- [X] T030 [US1] Add tests in `src/client/web/src/features/file-upload/FileUpload.test.tsx` for a dropped file selecting like a chosen one, the same quick checks, several dropped files, replacing a selection, ignoring non-file drags, ignoring a drop after receipt while still cancelling the default, and the hint shown only with a mouse or trackpad.
- [ ] T031 [US1] Add manual scenarios 13-14 (drop one file on desktop; drop several files) to `specs/003-scanned-document-upload/quickstart.md` and run them in current stable Chrome, Edge, Firefox, and Safari on desktop; confirm on Safari on iOS/iPadOS and Chrome on Android that no drag hint is shown and "Choose file" works.

---

## Dependencies and Execution Order

- **Phase 1** (T001) first; T006 needs T001.
- **Phase 2**: T002, T003, T005 are parallel; T004 needs T003; T006 needs T001. Phase 2 must finish before any story.
- **US1** needs Phase 2. Within US1: T007 and T008 are parallel and may be written first; T009 needs T002-T004; T010 is parallel; T011 needs T006 and T010; T012 needs T011; T013 needs T012.
- **US2** server tasks (T014, T016, T017) depend only on Phase 2 and T009 (same file, so sequence T009, then T016, then T017). Client tasks T015, T018, T019 need US1's T010-T012; T015 follows T008 (same test file), T018 needs T006 and T010 (same model file).
- **US3** needs US1's component (T011, T012); T020 follows T014 (same test file) and needs no endpoint change because the server behavior exists; T021 follows T015 (same test file); T022 follows T018 (same model file).
- **Polish** needs all stories. T028 is last.

User stories are independent where noted: US1 is the MVP; US2 completes the rejection paths; US3 adds retry behavior.

## Parallel Execution Examples

```text
Phase 2:      T002 + T003 + T005   (then T004 after T003; T006 after T001)
US1 tests:    T007 + T008          (server tests and client tests)
US1 build:    T009 (server)  ||  T010 (client model) -> T011 -> T012 -> T013
US2:          T014 + T015 together; T016 -> T017 on the server while T018 -> T019 on the client
US3:          T020 + T021 together; then T022
Polish:       T023 -> T024 -> T025 -> T026 -> T028 (T027 alongside T024)
```

## Implementation Strategy

1. **MVP (User Story 1)**: Phases 1-2 and Phase 3. This delivers a working upload, a received confirmation, and a retrievable original with its display name.
2. **Complete rejection handling (User Story 2)**: add the 20 MB limit, empty-file rejection, and readable errors. Treat this as part of the first release because every real upload needs it.
3. **Retry safety (User Story 3)**: client retry behavior on top of the existing server idempotency.
4. **Polish**: privacy review, automated and manual verification, browser matrix, then update the contract status line and the feature index.

Because the server side already exists, each phase can be verified against the running API without waiting for later stories.
