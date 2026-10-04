# Source Document Intake Contract

**Status**: Shared intake contract for every document channel. `camera-capture` is implemented (Feature 02); `file-upload` (Feature 01) and `mailbox` (Feature 03) are planned and currently rejected with `400`.

## Operation

`POST /api/source-documents`

Every channel creates the same source-document record through this one endpoint; only the request parts and the channel-specific `origin` differ. Downstream features (text extraction, classification, evidence storage) never branch on the channel. Evidence storage (Feature 08) links its `EvidenceDocument` to the result through `sourceDocumentId`. Authorization is the application-wide policy supplied by Feature 17.

Each submission has an `Idempotency-Key` UUID. The client reuses that key only when retrying the identical submission after an ambiguous network result. Reuse of the same key and payload returns the original source-document result; reuse with different content returns `409 Conflict`. This behavior depends on the deduplication capability planned in Feature 18.

## Channels

| `channel` | Owner | One source document is | Request parts | `origin` |
|---|---|---|---|---|
| `camera-capture` | Feature 02 | one confirmed capture | 1-3 `pages` (`image/jpeg`) | none |
| `file-upload` | Feature 01 | one selected file | one `file` (PDF or image) | `upload.originalFileName` (display only, never trusted) |
| `mailbox` | Feature 03 | one email attachment | one `file`, submitted by the collector service | `mailbox.messageId`, `from`, `subject`, `receivedAt`, `attachmentName`, `attachmentIndex` |

An email with several attachments produces one source document per attachment. Mailbox duplicates are detected by `messageId` + `attachmentIndex` + `contentHash`.

## Request (camera-capture)

`Content-Type: multipart/form-data` (the client must let `FormData` set the boundary).

| Part | Cardinality | Value |
|---|---:|---|
| `channel` | exactly one | `camera-capture` |
| `pages` | 1-3 | Repeated `image/jpeg` binary parts, serialized in the accepted page order. |

The server validates the number and order of pages, declared media type, actual image signature, and configured request-size limit. It must not trust a client filename or infer the channel from the filename. The client must not send individual pages before the user confirms the complete document.

### Shared intake requirements

- One `channel` field is required and must be a supported channel.
- Camera captures allow one to three JPEG page parts, ordered and preserved by the server.
- Duplicate requests with the same payload and `Idempotency-Key` must be treated as the same submission.
- Any invalid request must be rejected atomically before storage is committed.

## Success response

`201 Created` after the complete source document is accepted by the shared storage boundary. All channels return the same shape:

```json
{
  "sourceDocumentId": "5a14d651-c487-4e8b-babc-a81051053d7b",
  "channel": "camera-capture",
  "status": "received",
  "receivedAt": "2026-10-04T10:00:00Z",
  "submittedBy": "camera-capture-test-user",
  "mediaType": "image/jpeg",
  "pageCount": 2,
  "contentHash": "sha256:...",
  "storageLocation": "protected://source-documents/5a14d651-c487-4e8b-babc-a81051053d7b"
}
```

| Field | Rules |
|---|---|
| `sourceDocumentId` | Server-assigned UUID; the ID evidence storage refers to as `sourceDocumentId`. |
| `channel` | `camera-capture`, `file-upload`, or `mailbox`. |
| `status` | `received` after intake; later processing states belong to downstream features. |
| `receivedAt` | Server time the submission was accepted. |
| `submittedBy` | Authenticated user, or the collector service identity for `mailbox`. |
| `mediaType` | Media type of the stored content. |
| `pageCount` | Pages accepted (camera) or detected (file). |
| `contentHash` | SHA-256 over the received content; used with channel identifiers for duplicate detection. |
| `storageLocation` | Where the original file is kept in the protected document content store (Feature 08). Pages are stored with server-generated names; client filenames are never used. Evidence documents that reference this `sourceDocumentId` inherit it. |
| `origin` | Planned; present only for `file-upload` and `mailbox` (see Channels). |

The response must not claim success if only some pages were stored. The content store writes all pages or none; if storage fails the API returns `500`, creates no source document, and a retry with the same `Idempotency-Key` is processed as a new attempt.

## Errors

Use `application/problem+json` with a stable, non-sensitive error code and a user-safe message.

| Status | Meaning | Client behavior |
|---|---|---|
| `400` | Missing/invalid channel, empty pages, or page count outside 1-3 | Keep session pages for correction/review; do not mark submitted. |
| `401` / `403` | Application authentication or authorization failure | Explain that submission is unavailable; retain pages until user retries or exits. |
| `409` | Idempotency key was reused with different content | Do not retry automatically; explain that the submission could not be reconciled. |
| `413` | Configured total request-size limit exceeded | Explain that the capture is too large; retain pages for a safe retry path. |
| `415` | Unsupported media type or invalid image content | Explain the capture could not be accepted; do not create a document. |
| `5xx` | Intake or evidence-storage failure | Report a recoverable submission failure; do not report success or a partial document. |

Do not log image bytes, captured page content, or untrusted filenames. Do not expose storage paths or internal exception details in error responses. Camera/API credentials are not added by this feature.
