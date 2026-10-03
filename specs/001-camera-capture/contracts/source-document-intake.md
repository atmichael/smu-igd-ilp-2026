# Source Document Intake Contract

**Status**: Proposed dependency contract for camera capture; align with the shared scanned-upload and evidence-storage features before implementation.

## Operation

`POST /api/source-documents`

The React client sends a single request only after the user confirms the complete capture. The endpoint is shared intake, not a camera-specific storage path. It must write through the evidence-storage service owned by Feature 08 and apply the application-wide authorization policy supplied by Feature 17.

Each confirmed capture has an `Idempotency-Key` UUID. The client reuses that key only when retrying the identical submission after an ambiguous network result. Reuse of the same key and payload returns the original source-document result; reuse with different content returns `409 Conflict`. This behavior depends on the deduplication capability planned in Feature 18.

## Request

`Content-Type: multipart/form-data` (the client must let `FormData` set the boundary).

| Part | Cardinality | Value |
|---|---:|---|
| `source` | exactly one | `camera-capture` |
| `pages` | 1-3 | Repeated `image/jpeg` binary parts, serialized in the accepted page order. |

The server validates the number and order of pages, declared media type, actual image signature, and configured request-size limit. It must not trust a client filename or infer source from the filename. The client must not send individual pages before the user confirms the complete document.

### Shared intake requirements

- One `source` field is required and must equal `camera-capture`.
- One to three JPEG page parts are allowed.
- Parts are ordered and must be preserved by the server.
- Duplicate requests with the same payload and `Idempotency-Key` must be treated as the same submission.
- Any invalid request must be rejected atomically before storage is committed.

## Success response

`201 Created` after the complete source document and all pages are accepted by the shared storage boundary.

```json
{
  "sourceDocumentId": "5a14d651-c487-4e8b-babc-a81051053d7b",
  "source": "camera-capture",
  "pageCount": 2,
  "status": "received"
}
```

The response must not claim success if only some pages were stored. Storage rollback/cleanup is required if persistence fails during the request.

## Errors

Use `application/problem+json` with a stable, non-sensitive error code and a user-safe message.

| Status | Meaning | Client behavior |
|---|---|---|
| `400` | Missing/invalid source, empty pages, or page count outside 1-3 | Keep session pages for correction/review; do not mark submitted. |
| `401` / `403` | Application authentication or authorization failure | Explain that submission is unavailable; retain pages until user retries or exits. |
| `409` | Idempotency key was reused with different content | Do not retry automatically; explain that the submission could not be reconciled. |
| `413` | Configured total request-size limit exceeded | Explain that the capture is too large; retain pages for a safe retry path. |
| `415` | Unsupported media type or invalid image content | Explain the capture could not be accepted; do not create a document. |
| `5xx` | Intake or evidence-storage failure | Report a recoverable submission failure; do not report success or a partial document. |

Do not log image bytes, captured page content, or untrusted filenames. Do not expose storage paths or internal exception details in error responses. Camera/API credentials are not added by this feature.
