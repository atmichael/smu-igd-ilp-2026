# Feature Specification: Camera Document Capture

**Feature Branch**: `001-camera-capture`

**Created**: 2026-10-03

**Status**: Draft

**Input**: User description: "As an accounts-payable user, I want to capture a document with my device camera so that a paper invoice can enter the document-processing workflow without first being scanned separately. The user can start a capture, preview the image, retake it, or accept it for processing. If camera access is unavailable or denied, the user receives a clear explanation and can leave the workflow without losing existing work. An accepted image remains associated with its source. Keep this feature to capturing one document at a time. Do not include mailbox collection, OCR behavior, classification, AP matching, or payment approval."

## Clarifications

### Session 2026-10-03

- Q: Which device types must the first release support for camera capture? → A: Mobile phones and tablets.
- Q: What should the next document-processing step receive after the user submits a multi-page capture? → A: Original captured images grouped as one source document.
- Q: If the user cancels or leaves before submission, what should happen to the captured pages? → A: Keep pages only during the active session and discard them on cancel or exit.
- Q: Which browser support commitment should the first release make for camera capture? → A: Current stable Safari on iOS/iPadOS and Chrome on Android.
- Q: What maximum number of pages should one capture session accept? → A: 3 pages.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Capture an invoice document (Priority: P1)

As an accounts-payable user, I want to capture a paper invoice with my device camera so that I can submit it without scanning it separately.

**Why this priority**: Camera capture is a direct intake path for paper invoices and is useful even before mailbox collection or matching is available.

**Independent Test**: Using a supported camera-enabled device, capture and submit a one- or multi-page invoice. Confirm that the submitted document contains the captured pages in order and is available to the next document-processing step.

**Acceptance Scenarios**:

1. **Given** the user has granted camera access, **When** they capture an invoice page, **Then** they can preview the image before accepting it.
2. **Given** the user is previewing a captured page, **When** they retake or remove it, **Then** the rejected page is not included in the submitted document.
3. **Given** the invoice has multiple pages, **When** the user captures and accepts each page, **Then** the submitted document contains all accepted pages in capture order.
4. **Given** the user has reviewed the captured pages, **When** they confirm submission, **Then** the complete capture is sent as one source document and remains associated with its capture source.
5. **Given** the user cancels before submission, **When** they leave the capture flow, **Then** no partial document is submitted.

### User Story 2 - Recover from camera access or capture problems (Priority: P2)

As an accounts-payable user, I want a clear recovery path when camera capture is unavailable so that I can retry or leave without losing control of the workflow.

**Why this priority**: Camera access depends on the user's device and permission choices; clear recovery prevents a failed capture from blocking invoice processing.

**Independent Test**: Deny camera permission, use a device without an available camera, and simulate a capture failure. Verify that each case explains what happened and offers retry when possible or a safe way to exit.

**Acceptance Scenarios**:

1. **Given** camera permission is denied, **When** the user starts capture, **Then** the system explains that access is unavailable and does not submit a document.
2. **Given** no camera is available or capture fails, **When** the failure occurs, **Then** the user can retry where possible or return to document source selection without losing existing work.

---

### Edge Cases

- The captured image is too dark, blurred, rotated, or cropped for the user to verify.
- The user retakes or removes a page after capturing later pages.
- The device loses camera availability during a capture session.
- The user exits while a page is being captured or previewed.
- A multi-page capture is interrupted before submission.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST let the user explicitly start a camera capture session and request the access needed to capture an image.
- **FR-002**: The system MUST show each captured page for review before it is included in a submitted document.
- **FR-003**: The user MUST be able to retake or remove a captured page before submission.
- **FR-004**: The system MUST support capturing multiple pages as one document and preserve their accepted capture order.
- **FR-005**: The system MUST submit a capture only after the user confirms it; cancellation MUST NOT submit a partial document.
- **FR-006**: The system MUST associate an accepted capture with its camera-capture source and retain the original captured pages for downstream processing.
- **FR-007**: When permission is denied, a camera is unavailable, or capture fails, the system MUST explain the problem and offer retry or a safe exit where possible.
- **FR-008**: The system MUST keep camera capture limited to one document per capture session; mailbox collection, OCR, classification, matching, and payment approval are outside this feature.
- **FR-009**: The first release MUST support camera capture on mobile phones and tablets.
- **FR-010**: On submission, the system MUST hand off the original captured page images grouped as one source document.
- **FR-011**: The system MUST keep unsubmitted captured pages only during the active capture session and discard them on cancellation or exit without discarding work that existed before capture began.
- **FR-012**: The first release MUST support camera capture in current stable Safari on iOS/iPadOS and current stable Chrome on Android.
- **FR-013**: A capture session MUST accept no more than three pages for one source document.
- **FR-014**: The system MUST require the application authorization policy before accepting a source-document intake request and MUST reject unauthorized requests without persisting any document content.
- **FR-015**: The system MUST support idempotent submission retries for the same payload using the same Idempotency-Key and MUST return the same result without creating a duplicate source document.
- **FR-016**: The system MUST validate the multipart source-document request before persisting any page data and MUST reject invalid source values, non-JPEG content, malformed images, page counts outside 1-3, or requests exceeding the configured size limit without leaving partial storage.

### Key Entities

- **Capture Session**: One user-initiated attempt to capture a single invoice document; includes its current state and accepted pages.
- **Captured Page**: An image captured during a session, with its position in the document and whether the user accepted, retook, or removed it.
- **Source Document**: The complete set of pages the user confirmed for downstream processing, with camera capture recorded as its channel.

### Intake Contract

The camera capture flow submits one source document through the shared source-document intake endpoint. The request uses `multipart/form-data` and includes:

- one required `channel` field with value `camera-capture`
- one to three ordered page parts named `pages`
- each page part must be `image/jpeg` and must validate as a valid JPEG image before acceptance
- one `Idempotency-Key` header for retry-safe submission

The response is a `201 Created` with the server-assigned `sourceDocumentId` for the accepted document. Repeated requests with the same payload and the same `Idempotency-Key` must not produce duplicate source documents.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 9 out of 10 representative users can capture, review, and submit a multi-page invoice within two minutes without assistance.
- **SC-002**: In the supported-device test set, 100% of submitted captures contain exactly the accepted pages in the order shown to the user.
- **SC-003**: In all tested permission-denied, camera-unavailable, capture-failure, and cancellation cases, no partial document is submitted and the user receives a clear next action.
- **SC-004**: Users can identify and retake or remove an unacceptable page before submission in every tested capture scenario.

## Assumptions

- Users have access to a camera-enabled device and may grant camera permission.
- One capture session represents one invoice document; a document may contain multiple pages.
- Existing or future document upload is a separate intake path; this feature allows the user to return to source selection but does not implement upload.
- OCR, image enhancement, document classification, extraction, and financial verification occur in later features.
- The two-minute and 9-of-10 usability targets are initial product targets and may be adjusted by stakeholders before planning.