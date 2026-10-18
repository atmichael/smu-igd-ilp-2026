# Feature Specification: Scanned Document Upload

**Feature Branch**: `003-scanned-document-upload`

**Created**: 2026-10-18

**Status**: Draft

**Input**: User description: "As an accounts-payable user, I want to upload an invoice PDF or image so that it can enter the document-processing workflow. The user selects one PDF or image, sees whether it can be accepted, and receives a clear error if the file is unsupported, unreadable, or too large. The original remains available and associated with normalized pages and extracted text. Keep this feature to manual upload of one document at a time. Do not include camera capture, mailbox collection, document classification, AP matching, or payment approval. Submit through the shared source-document intake contract with `channel` = `file-upload` and one `file` part, so uploads produce the same source-document record as camera capture and mailbox collection. Keep the original file name only as display metadata in `origin.upload`; never trust it for type detection or storage paths."

## Clarifications

### Session 2026-10-18

- Q: Which checks should the upload screen run on the selected file before the user confirms? → A: Quick checks only (file name extension, size against the limit, not empty); the server alone decides content match and PDF readability, and its rejection is shown right after submission.
- Q: Which devices and browsers must the first release of upload support? → A: Current stable Chrome, Edge, Firefox, and Safari on desktop, plus current stable Safari on iOS/iPadOS and Chrome on Android, so users can upload PDFs and images from phones and tablets as well.
- Q: What exact maximum file size should upload accept and tell users about? → A: 20 MB, enforced by both the upload screen and the server, with the shared intake contract updated to state it.
- Q: Should the 20 MB limit apply only to file uploads or to every intake channel? → A: Only to `file-upload`; camera capture and mailbox keep their existing limits.
- Q: After a file is received, what should the user be able to do next on the upload screen? → A: See a confirmation with the source document ID, start another upload, and view or download the original just uploaded.
- Q: Should the upload screen also let users drag a file onto it? → A: Yes, as an optional shortcut on devices with a mouse or trackpad; choosing a file stays available everywhere, including phones and tablets where dragging is not possible.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upload an invoice file (Priority: P1)

As an accounts-payable user, I want to select an invoice PDF or image from my device and submit it so that it enters the document-processing workflow without being re-scanned or re-keyed.

**Why this priority**: Manual upload is the most direct intake path for invoices that already exist as files, and it delivers value before camera capture or mailbox collection is used.

**Independent Test**: Select a valid PDF and a valid image, one at a time, and submit each. Confirm that each produces one accepted source document, that the original file remains retrievable, and that the user sees confirmation that the document was received.

**Acceptance Scenarios**:

1. **Given** the user is on the upload workflow, **When** they select one PDF or image, **Then** the system shows the selected file's name and the result of quick checks (supported extension, within the size limit, not empty) before anything is submitted.
2. **Given** the user has selected an acceptable file, **When** they confirm submission, **Then** the system accepts it as one source document with the file-upload channel and tells the user it was received.
3. **Given** a document was accepted, **When** the confirmation is shown, **Then** it displays the source document ID and offers actions to upload another file and to view or download the original just uploaded.
4. **Given** a document was accepted, **When** the user or a later processing step retrieves it, **Then** the original file is available unchanged.
5. **Given** the user selected a multi-page PDF, **When** it is accepted, **Then** the source document records how many pages it contains.
6. **Given** the user selected the wrong file, **When** they replace the selection before submitting, **Then** only the replacement is submitted.
7. **Given** the user is on a device with a mouse or trackpad, **When** they drop one file onto the upload screen, **Then** it is treated exactly like a chosen file: its name and quick-check result are shown and nothing is submitted until they confirm.

---

### User Story 2 - Understand and recover from a rejected file (Priority: P1)

As an accounts-payable user, I want a clear explanation when my file cannot be accepted so that I can choose a different file or fix the problem without guessing.

**Why this priority**: Rejections are common (wrong format, protected PDFs, oversized scans). Without clear errors, users cannot recover and the feature blocks invoice intake.

**Independent Test**: Attempt to upload an unsupported file type, a file whose content does not match its type, a damaged PDF, a password-protected PDF, an empty file, and an oversized file. Confirm that each is rejected with a specific, plain-language reason and that no source document is created.

**Acceptance Scenarios**:

1. **Given** the user selects a file that is not a PDF or a supported image, **When** the system evaluates it, **Then** the user is told the file type is unsupported and which types are accepted.
2. **Given** the user selects a file whose content does not match its apparent type (for example, a renamed non-PDF), **When** it is submitted, **Then** it is rejected as unsupported and no document is created.
3. **Given** the user selects a damaged or password-protected PDF, **When** it is submitted, **Then** the user is told the PDF cannot be read and is asked to provide an unprotected, undamaged copy.
4. **Given** the user selects a file larger than the allowed size, **When** the system evaluates it, **Then** the user is told it is too large and what the limit is.
5. **Given** a submission is rejected or fails, **When** the user views the result, **Then** they can choose another file or retry without leaving the workflow, and nothing partial is stored.

---

### User Story 3 - Retry safely after a network problem (Priority: P2)

As an accounts-payable user, I want to retry a submission after a connection problem without creating duplicate documents so that I can trust the result.

**Why this priority**: A lost response should not force users to wonder whether the invoice was received, and duplicates create downstream rework.

**Independent Test**: Submit a file, simulate a lost response, and retry the identical submission. Confirm that exactly one source document exists and the retry returns the same result as the original.

**Acceptance Scenarios**:

1. **Given** a submission outcome is unknown due to a connection problem, **When** the user retries the same file, **Then** the system returns the original result and does not create a second document.
2. **Given** a submission failed because storage was unavailable, **When** the user retries, **Then** the system treats it as a new attempt and the user sees either a received confirmation or a clear failure.
3. **Given** a retry would reuse a submission identity with different file content, **When** it is submitted, **Then** the user is told the submission could not be reconciled and no document is created or changed.

---

### Edge Cases

- The file name suggests one type (for example `.pdf`) but the content is another; the content decides acceptance and the name is never used to detect type.
- The file name contains path-like or unusual characters; it is shown only as display text and never influences where the document is stored.
- The user selects or drops more than one file at once; only one file per submission is allowed, the user is told so, and any file already selected stays selected.
- The user selects an empty (zero-byte) file.
- The user's session expires before submission; the user is told submission is unavailable and can retry after signing in again.
- A multi-page PDF with a very large page count or very large images is near the 20 MB size limit; a file of exactly 20,971,520 bytes is accepted and one byte over is rejected. No page-count limit applies; the file-size limit is the only bound on PDFs.
- A scanned image is rotated, blurry, or low contrast; intake accepts any readable supported image and leaves quality feedback to later features.
- The user leaves the upload workflow before confirming; nothing is submitted.
- On a phone or tablet, the user selects a PDF or image already stored on the device; taking a new photo with the camera is the separate camera-capture feature.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: Users MUST be able to select exactly one PDF or image file from their device and explicitly confirm submission as one document; each submission MUST contain exactly one file.
- **FR-002**: The system MUST show the selected file's display name and the result of quick checks (supported extension, within the size limit, not empty) before submission, MUST let the user replace or clear the selection, and MUST NOT present the quick checks as a guarantee of acceptance; content match and PDF readability are decided only by the server after submission, and its result is shown immediately.
- **FR-003**: The system MUST accept PDF, JPEG, and PNG files and MUST reject all other types with a message that lists the accepted types.
- **FR-004**: The system MUST decide file type from the file's actual content, MUST reject content that does not match its declared type, and MUST NOT use the original file name to detect type.
- **FR-005**: The system MUST reject a PDF that is damaged, unreadable, or password-protected, and MUST tell the user how to provide a usable copy.
- **FR-006**: The system MUST reject empty files and files larger than 20 MB (20 × 1024 × 1024 = 20,971,520 bytes), and MUST tell the user the 20 MB limit when a file is too large. The upload screen and the server MUST enforce the same limit for the file-upload channel only, and the shared intake contract MUST state it per channel; this feature MUST NOT change the size limits of camera capture or mailbox collection.
- **FR-007**: The system MUST submit uploads through the shared source-document intake with the file-upload channel and one file, so that an upload produces the same source-document record as camera capture and mailbox collection.
- **FR-008**: The system MUST record the original file name only as display metadata in the upload origin of the source document, and MUST NOT use it for type detection, storage locations, or logs of untrusted content.
- **FR-009**: The system MUST keep the original file unchanged and retrievable for the accepted source document, and MUST record its content type, page count (for PDFs, the number of pages), and a content hash.
- **FR-010**: The system MUST expose the accepted source document's identity so that page normalization and text extraction (Feature 04) can attach their results to the same document without altering the original.
- **FR-011**: The system MUST validate the complete submission before storing anything and MUST store nothing if validation or storage fails; a document MUST NOT be reported as received unless it was fully stored.
- **FR-012**: The system MUST make an identical retry safe: repeating the same submission after an unknown outcome returns the original result without creating a duplicate, and reusing a submission identity with different content is rejected without changing any stored document.
- **FR-013**: The system MUST require the application's authorization policy before accepting an upload and MUST reject unauthorized requests without persisting content.
- **FR-014**: Every rejection MUST give a plain-language reason and a next action, MUST preserve the user's place in the workflow, and MUST NOT expose storage paths or internal error details.
- **FR-015**: The system MUST show clear loading (in progress), success (received), and error states. The success state MUST show the source document ID and offer actions to upload another file and to view or download the original just uploaded. The upload workflow MUST be fully operable by keyboard with visible focus, accessible labels, and status messages that do not depend on color alone.
- **FR-016**: Camera capture, mailbox collection, document classification, AP matching, and payment approval are outside this feature.
- **FR-017**: The system MUST NOT write file contents or untrusted file names to logs.
- **FR-018**: The first release MUST support the upload workflow in current stable Chrome, Edge, Firefox, and Safari on desktop, and in current stable Safari on iOS/iPadOS and Chrome on Android, including selecting a PDF or image stored on the phone or tablet; the layout MUST work at viewport widths from 320 CSS pixels with no horizontal scrolling, interactive controls at least 44 × 44 CSS pixels, and every action reachable without zooming.
- **FR-019**: The upload screen MUST also accept a single file dropped onto it as an optional shortcut, applying the same quick checks as a chosen file and never submitting on drop; choosing a file MUST remain available on every supported browser. The drag hint MUST appear only on devices with a mouse or trackpad, and a file dropped on the page MUST NOT replace the page in the browser.

### Key Entities

- **Source Document**: The record created when a file is accepted by intake. For uploads it has the file-upload channel, content type, page count, content hash, received time, submitting user, and a link to the original file. It is the same record that camera capture and mailbox collection create.
- **Original File**: The unchanged PDF or image the user selected. It is retained and retrievable, and later features derive pages and text from it without modifying it.
- **Upload Origin**: Display-only information about an uploaded file, namely its original file name. It is never used for type detection or storage.
- **Upload Attempt**: One user action to submit a selected file, including its outcome (received, rejected with a reason, or failed) and the submission identity that makes retries safe.

### Intake Contract

Uploads use the [shared source-document intake contract](../001-camera-capture/contracts/source-document-intake.md#channels) and do not define a separate contract. A submission contains:

- one `channel` field with value `file-upload`
- one `file` part that is a PDF, JPEG, or PNG
- one `Idempotency-Key` for retry-safe submission

A successful submission returns the same source-document result as other channels. Rejections use the shared error responses (invalid request, unauthorized, key conflict, too large, unsupported or unreadable content, and storage failure). The `origin.upload` display metadata is part of the shared contract's planned `origin`; recording it is in scope for this feature, and any change to the shared contract is made in that contract, not duplicated here.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 9 out of 10 representative users can select, review, and submit a valid invoice file within one minute without assistance.
- **SC-002**: 100% of valid PDF, JPEG, and PNG test files of 20 MB (20,971,520 bytes) or less are accepted, and the stored original is byte-identical to the selected file.
- **SC-003**: 100% of unsupported, mismatched-content, damaged, password-protected, empty, and oversized test files are rejected with a specific plain-language reason and leave no stored document.
- **SC-004**: In every tested retry scenario (lost response, repeated identical submission), exactly one source document exists afterward; no tested scenario produces a duplicate or partial document.
- **SC-005**: Files whose names claim one type but whose content is another, and files with unusual or path-like names, are never accepted on the basis of the name and never affect where the document is stored.
- **SC-006**: Every state of the upload workflow (idle, selected, in progress, received, rejected, failed) can be completed using only the keyboard and meets WCAG 2.2 AA contrast.
- **SC-007**: A document accepted by upload is indistinguishable in structure from a camera-captured or mailbox-collected source document for downstream steps, except for its channel and origin.
- **SC-008**: On each supported desktop and mobile browser, a valid PDF and a valid image can be selected, submitted, and confirmed as received, and a rejected file shows its reason, with no horizontal scrolling at a 320 CSS pixel viewport width and every action reachable.

## Assumptions

- Users are authenticated; until the application-wide access control feature is delivered, the development sign-in applies.
- The maximum file size is 20 MB. The intake service currently allows about 30 MB, so a file-upload-specific limit and the shared intake contract are updated to 20 MB as part of this feature, while other channels keep their existing limits; a different business limit is a later product decision.
- Supported file types are PDF, JPEG, and PNG, as listed in the shared intake contract; other image formats (such as HEIC or TIFF) are out of scope for this release.
- The server side of file-upload intake already exists; this feature delivers the user-facing upload workflow and completes the recording of the original file name as display metadata.
- Page normalization, OCR, and text extraction belong to Feature 04. This feature guarantees only that the original remains available and linked by the source document's identity so those results can be attached later.
- Document quality feedback (Feature 19), duplicate detection beyond retry safety (Feature 18), classification, AP matching, and payment approval are separate features.
- Accepted pilot risk: no PDF page-count or parse-time limit is set, so a crafted PDF within the file-size limit may be expensive to read; protection against this is revisited with processing reliability (Feature 18).
- The one-minute and 9-of-10 usability targets are initial product targets and may be adjusted by stakeholders before planning.
- Frontend work follows the project's shared Material UI theme and design-system conventions (constitution Principles VII and VIII).
