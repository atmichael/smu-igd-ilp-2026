# Research: Scanned Document Upload

All Technical Context items are resolved; no `NEEDS CLARIFICATION` remains. Findings come from the current code in `SourceDocumentsEndpoints.cs`, `Program.cs`, `SourceDocumentRepository.cs`, the web client, and the shared intake contract.

## Current state

- The `file-upload` branch of `POST /api/source-documents` already validates exactly one `file`, a declared type of PDF/JPEG/PNG, a matching signature, and a readable, unencrypted PDF (page count through PdfPig). It stores `original.pdf` or `page-1.<ext>`, supports idempotent retries, and serves the original at `GET /api/source-documents/{id}/file`.
- Missing: any file-size limit of its own (no request-size option is configured, so the framework default of about 30,000,000 bytes applies to every channel), an explicit empty-file rejection (an empty file currently falls through to `415` as "content mismatch"), the recorded `origin.upload`, and every client screen.
- The client only offers camera capture. Its API module lives under `features/camera-capture/` and accepts only `camera-capture`.

## Decisions

### 1. Enforce the 20 MB limit per channel after reading the form

- **Decision**: In the `file-upload` branch, compare `IFormFile.Length` to `SourceDocumentLimits.FileUploadMaxBytes` (20 x 1024 x 1024 = 20,971,520 bytes) before copying the file to memory. Return `413` with a user-safe message that states 20 MB. The framework request limit (about 30 MB) stays as the outer bound for all channels.
- **Rationale**: The channel is a form field, so it is not known before the form is read; a limit on the whole endpoint would also change camera capture and mailbox, which the spec forbids (FR-006). A file between 20 MB and the outer bound is rejected by our check; anything above the outer bound is rejected by the framework.
- **Alternatives considered**: An endpoint-wide request-size attribute (changes every channel); a separate upload route (breaks the shared intake); middleware that peeks at the channel (requires buffering the body twice).
- **Follow-up**: A request over the outer bound makes `ReadFormAsync` throw `BadHttpRequestException` with status 413 and no problem body. The endpoint should catch it and return the same `application/problem+json` 413 so clients always get a readable message; the client also falls back to a fixed message on any 413.

### 2. Keep the limit in one shared constant and mirror it in the client

- **Decision**: Define `SourceDocumentLimits.FileUploadMaxBytes` in `ILP.Shared`, mirror it as a client constant, and state it in the shared contract. A server test asserts the boundary (20 MiB accepted, one byte over rejected); a client test asserts the same boundary.
- **Rationale**: Matches how the client and server already share the contract by documentation; a configuration endpoint is unnecessary at pilot scale.
- **Alternatives considered**: A `GET` configuration endpoint for limits (extra API surface for one number); reading the limit from `appsettings` on the server only (client cannot show it without a round trip).

### 3. Treat client checks as advisory

- **Decision**: The screen checks extension (`.pdf`, `.jpg`, `.jpeg`, `.png`, case-insensitive), size at or below the limit, and non-empty. It sets `accept="application/pdf,image/jpeg,image/png"` on the picker, does not set `capture`, and sends the file unchanged. It never uses the name to decide what the file is; the server alone checks content and PDF readability.
- **Rationale**: Matches the clarification and FR-002/FR-004. A browser that reports an empty `File.type` for a valid file would be rejected by the server as unsupported; this is rare for these three types and is accepted rather than guessing a type from the name.
- **Alternatives considered**: Parsing PDFs in the browser (rejected in clarification: new dependency, memory cost, duplicated rules); mapping a missing type from the extension (would use the name to infer type).

### 4. Record the original file name from the multipart part, sanitized

- **Decision**: Read the `file` part's file name, keep only the final path segment (split on `/` and `\`), remove control characters, trim, and cap at 255 characters. Store it in `origin.upload.originalFileName`; omit `origin` when the result is empty. It is never used for storage paths or type detection and is not logged. The client renders it as plain text.
- **Rationale**: Satisfies FR-008/FR-017 and the contract note that the name is display-only. Server-side sanitizing protects other consumers of the record, not only this screen.
- **Alternatives considered**: A separate form field for the name (a second channel for untrusted input); storing the raw name (path-like names reach downstream displays).

### 5. Idempotency stays content-based

- **Decision**: Keep `payloadHash = channel + page hashes`. The file name is not part of the hash, so a retry with the same key and the same bytes returns the original record even if the name differs.
- **Rationale**: The name is display metadata; the existing retry and conflict rules (FR-012) remain unchanged.
- **Alternatives considered**: Include the name in the hash (a renamed retry would conflict, which helps no one).

### 6. Client idempotency-key lifetime

- **Decision**: Create a new key when a file is selected or replaced and after a successful submission. Reuse the same key on retry of the same selection after a network error or `5xx`. After a definitive rejection (`4xx` other than `409`), the user must choose a file again, so the key changes naturally.
- **Rationale**: Gives the lost-response retry guarantee of User Story 3 without creating a second document.
- **Alternatives considered**: A new key on every click (would create duplicates after a lost response).

### 7. View or download the original with an authenticated fetch

- **Decision**: Fetch `GET /api/source-documents/{id}/file` with the same credentials as intake, create an object URL from the response, and open or download it; revoke the URL when replaced or unmounted. Fall back to an error message if it fails.
- **Rationale**: The endpoint requires authorization, so a plain link would not carry the credentials; this also works once Feature 17 replaces the development scheme.
- **Alternatives considered**: A plain anchor to the API URL (unauthenticated); a signed link (new server capability, out of scope).

### 8. Move the client API module

- **Decision**: Move `source-document-api.ts` to `features/source-documents/`, make the request a discriminated union (`camera-capture` with `pages`, `file-upload` with `file`), and update the two importers. `SourceDocumentResponse` gains `mediaType`, `origin`, and the other contract fields it already returns.
- **Rationale**: Avoids a parallel API module (Principle IX) and a feature importing from another feature's folder.
- **Alternatives considered**: A second module under `file-upload` (duplicate request code); leaving the file in `camera-capture` (wrong dependency direction).

### 9. Error presentation

- **Decision**: Display the server's `detail` (already user-safe by contract) for `400`, `413`, and `415`, with fixed fallback text per status. Map `401` to "submission is unavailable; sign in again", `409` to "could not be reconciled", `5xx` and network failures to a retry message that keeps the selection. See [contracts/shared-intake-changes.md](contracts/shared-intake-changes.md).
- **Rationale**: Each rejection reason in the spec (unsupported, unreadable PDF, too large, empty) already has a distinct server message; the client adds the next action.
- **Alternatives considered**: Stable error codes in the response (the contract asks for them but the endpoint does not emit them today; adding them is a shared-contract change owned outside this feature).

### 10. Verification approach

- **Decision**: Automated server tests for the boundary, empty file, origin recording and sanitizing, mismatched content, password-protected PDF, retry and conflict. Vitest for quick checks, state transitions, key reuse, and success/rejection rendering. Manual verification across the six browsers and for keyboard and contrast (no browser-automation or accessibility-test packages are added).
- **Rationale**: Matches the constitution (deterministic rules tested; external checks explicit) and avoids new dependencies.
- **Alternatives considered**: Adding Playwright and an accessibility checker (valuable later, a larger tooling decision than this feature).

### 11. Frontend design system gap

- **Decision**: Use MUI components consistent with the existing camera-capture and `App.tsx` screens and record the Principle VII deviation.
- **Rationale**: The design-system document does not exist yet; the MUI theme in `src/client/web/src/main.tsx` is used. The same deviation is already accepted for Feature 02.
- **Alternatives considered**: Creating the design system here (out of scope).

### 12. No PDF page-count limit (accepted pilot risk)

- **Decision**: Do not add a page-count or parse-time limit. The 20 MB file-size limit is the only bound on PDFs.
- **Rationale**: Confirmed product decision; a limit would reject some legitimate long documents, and reliability protections are owned by Feature 18. A crafted PDF within 20 MB may still be expensive for PdfPig to read; this is accepted for the pilot and recorded in the spec assumptions.
- **Alternatives considered**: A 100- or 200-page limit with a split-the-file message (new user-facing rule and tests); a parse timeout (PdfPig cannot be cancelled mid-parse).

### 13. Stable error codes deferred

- **Decision**: Keep the current problem responses (title and detail). The client prefers the server `detail` and uses fixed fallback text per status, so a wording change does not break it.
- **Rationale**: The shared contract asks for a stable error code on every channel, but adding one changes camera capture and mailbox responses too and belongs to a shared-contract change, not this feature.
- **Alternatives considered**: Adding a `code` extension member only for `file-upload` (a channel-specific shape the shared contract is meant to avoid).
