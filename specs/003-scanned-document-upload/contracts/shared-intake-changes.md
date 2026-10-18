# Shared Intake Contract Changes for `file-upload`

The shared contract remains [source-document-intake.md](../../001-camera-capture/contracts/source-document-intake.md). This feature does not define a second contract. The edits below must be made in that file when this feature is implemented; this page lists them so reviewers can verify them.

## Contract edits

| Section | Change |
|---|---|
| Status line | Remove "the upload screen is not built yet" once the screen ships. |
| Channels table, `file-upload` `origin` | Keep `upload.originalFileName`; state it is now recorded. |
| Request (file-upload) | State the 20 MB (20 x 1024 x 1024 bytes) limit for the `file` part and that an empty file is rejected. Replace "`origin.upload.originalFileName` is not recorded yet" with the recording rule below. |
| Success response, `origin` row | Change "Planned" to: present for `file-upload` (omitted for `camera-capture`); `origin.upload.originalFileName` is the sanitized display name. |
| Errors table | See the status table below. |

## `origin` in the response (file-upload)

```json
{
  "sourceDocumentId": "5a14d651-c487-4e8b-babc-a81051053d7b",
  "channel": "file-upload",
  "status": "received",
  "receivedAt": "2026-10-18T10:00:00Z",
  "submittedBy": "camera-capture-test-user",
  "mediaType": "application/pdf",
  "pageCount": 3,
  "contentHash": "sha256:...",
  "storageLocation": "protected://source-documents/5a14d651-c487-4e8b-babc-a81051053d7b",
  "origin": { "upload": { "originalFileName": "invoice-0042.pdf" } }
}
```

`originalFileName` rules: final path segment only, control and Unicode format characters removed (categories Cc and Cf, including bidirectional overrides such as U+202E), trimmed, at most 255 characters; the property and `origin` are omitted when nothing usable remains. It is display metadata: it is never trusted for type detection, never used in storage paths, and never logged.

## Status codes for `file-upload`

| Status | Condition | Message shown to the user (server `detail`, or the fallback if absent) | Client next action |
|---|---|---|---|
| `201` | Accepted, or identical retry with the same key | Document received, with source document ID | Offer "Upload another" and view/download of the original |
| `400` | Missing `Idempotency-Key`, not exactly one `file`, or an empty file | "Choose one file that is not empty." | Keep the user on the screen; let them choose a file |
| `401` | Not authenticated | "Upload is unavailable. Sign in again and retry." | Keep the selection; retry after signing in |
| `409` | Same key, different content | "This submission could not be reconciled. Choose the file again." | Do not retry automatically; clear the key |
| `413` | `file` larger than 20 MB, or the whole request over the framework limit | "The file is too large. The limit is 20 MB." | Choose another file |
| `415` | Not PDF/JPEG/PNG, content does not match the declared type, or the PDF is damaged or password-protected | "The file is not a supported type or could not be read. Upload a PDF, JPEG, or PNG; PDFs must not be password-protected." (use the server `detail` when present) | Choose another file |
| `5xx` | Storage or intake failure | "The document could not be stored. Try again." | Retry with the same key; selection kept |
| Network error | No response | "Connection problem. Try again; the document will not be duplicated." | Retry with the same key; selection kept |

The `413` problem response is returned as `application/problem+json` in both cases (the framework request-limit failure must be caught and converted), so clients always receive a readable message. Responses never include storage paths or exception details.

## Unchanged

Request parts (`channel` = `file-upload`, exactly one `file`), the `Idempotency-Key` rules, the read endpoints, the success-response fields, and the limits of `camera-capture` and `mailbox`.

## Known gap

The shared contract asks for a stable error code in each problem response; the endpoint returns only `title` and `detail` today. This feature does not add codes (see research decision 13). The client prefers the server `detail` and falls back to the fixed text above, so a wording change does not break it.
