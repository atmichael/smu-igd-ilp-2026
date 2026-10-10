# Research: Camera Document Capture

## Repository findings

- The React client is a Vite 8 / React 19 / TypeScript 6 starter. The ASP.NET Core 10 minimal API is still a scaffold with only a sample weather endpoint; `ILP.Shared` has no application contract yet.
- The camera-capture brief owns the camera interaction. Scanned upload is a separate intake path. Feature 08 owns durable evidence storage and retrieval, while access control and deduplication are also planned as separate features.
- No test project or frontend test runner exists. Focused test infrastructure must be added as part of implementation.
- The target architecture requires React to use the ASP.NET Core API and requires original source evidence to remain available. Camera code must not call OCR providers or introduce local permanent storage.

## Decisions

### Browser capture API

**Decision**: Use `navigator.mediaDevices.getUserMedia` with video only, initiated by an explicit user action. Prefer the rear-facing camera when available, but allow the browser/device fallback. Capture a still frame to a JPEG `Blob`; do not record audio or video and do not add image enhancement or OCR.

**Rationale**: This gives the flow control over a live preview, capture, review, retake, page order, and cancellation across the two specified mobile browser families. The browser prompts for permission and returns explicit failure modes for denied access, missing devices, and hardware errors.

**Alternatives considered**: A file input with the `capture` hint is simpler, but native picker behavior and review flow vary by browser. The `ImageCapture` API has narrower support than the required browser set. A video recording workflow is out of scope.

### Camera lifetime and local privacy

**Decision**: Request camera access only while the live capture view is active. Stop every media track when leaving live capture, before page review, and on cancel/unmount. Store accepted JPEG blobs and preview object URLs only in active session state; revoke URLs and release references when a page is replaced/removed or the session ends. Do not upload until explicit confirmation.

**Rationale**: This meets the no-partial-submit and discard-on-exit requirements while promptly releasing camera hardware and limiting local retention.

### Secure context requirement

**Decision**: Device camera tests must run from a secure origin. `getUserMedia` is unavailable on an ordinary HTTP Vite network address; localhost is considered secure only on the same device. Use a trusted HTTPS development host or HTTPS test deployment for physical-device tests.

**Rationale**: This is a browser platform constraint, not an application permission bug. The setup guide's `--host` option only exposes Vite to the network and does not by itself enable camera access.

### Submission and system boundaries

**Decision**: Submit one explicit confirmation as `multipart/form-data` to the shared `POST /api/source-documents` intake contract. Include `channel=camera-capture` and one to three repeated `pages` JPEG parts in accepted order. Return a source-document ID only after the shared intake/storage boundary has accepted the complete set. Camera capture does not implement a private persistence pipeline.

**Rationale**: Multipart uploads avoid base64 expansion and match ASP.NET Core's supported file-upload model. A common source-document intake contract lets camera and scanned upload converge. Durable storage is provided by Feature 08; the API boundary verifies the entire request and must not create a partial source document.

**Alternatives considered**: A camera-specific storage endpoint would duplicate the future scanned-upload intake path. JSON/base64 increases payload size and memory use. Storing images in the React app would violate the server/client boundary and source-retention requirement.

### Upload validation and failures

**Decision**: The server validates page count, declared media type, image signature, and configured request-size limits. Ignore client filenames for storage, assign server-generated names, return structured client-safe errors, and keep image bytes out of logs. Reject a request that is not wholly valid; do not expose a partial document. Do not override framework body-size defaults until supported-device captures are measured; provide a clear recoverable size error and make the effective limit configurable if benchmarks require it.

**Rationale**: Client validation improves feedback but is not a security boundary. ASP.NET Core documents server-side file validation, untrusted filenames, and request-size limits. Buffering strategy and effective limits must be checked against the deployed host.

### Test approach

**Decision**: Add Vitest and React Testing Library for the client state machine and mocked camera permission/stream behavior. Add .NET integration tests for multipart contract validation and atomic rejection. Also run a manual device matrix on current stable Safari for iOS/iPadOS and Chrome for Android over HTTPS.

**Rationale**: Unit tests can deterministically exercise permission denial, camera absence, track cleanup, retake/removal, ordering, page cap, cancel, and submission failures. Physical-device checks remain necessary for real permissions and camera hardware. There are no existing test conventions or test projects to reuse.

## References

- [MDN: `MediaDevices.getUserMedia()`](https://developer.mozilla.org/en-US/docs/Web/API/MediaDevices/getUserMedia): permission behavior, secure-context requirement, camera constraints, and failure modes.
- [MDN: `MediaStreamTrack.stop()`](https://developer.mozilla.org/en-US/docs/Web/API/MediaStreamTrack/stop): release acquired camera tracks when capture is not active.
- [Microsoft Learn: Upload files in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/mvc/models/file-uploads?view=aspnetcore-10.0): multipart handling, file validation, untrusted filenames, storage choices, and request-size limits.
- Repository architecture and ownership notes: [invoice-ocr-architecture.md](../../docs/architecture/invoice-ocr-architecture.md), [feature-01-scanned-document-upload.md](../../docs/planning/feature-briefs/feature-01-scanned-document-upload.md), [feature-02-camera-capture.md](../../docs/planning/feature-briefs/feature-02-camera-capture.md), [feature-08-document-evidence-storage.md](../../docs/planning/feature-briefs/feature-08-document-evidence-storage.md).
