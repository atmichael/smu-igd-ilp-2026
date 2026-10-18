# Data Model: Scanned Document Upload

This feature reuses the existing source-document record and adds an optional `origin`. Names follow the owned terms in the [feature brief index](../../docs/planning/feature-briefs/feature-brief-index.md#owned-terms).

## Source Document (existing, extended)

Type: `SourceDocumentMetadataDto` (`ILP.Shared`). Owner: Features 01-03 (intake). Created once per accepted submission.

| Field | Type | Rules for `file-upload` |
|---|---|---|
| `sourceDocumentId` | GUID string | Server-assigned. |
| `channel` | string | `file-upload`. |
| `status` | string | `received` after intake. |
| `receivedAt` | timestamp | Server time of acceptance. |
| `submittedBy` | string | Authenticated user name. |
| `mediaType` | string | `application/pdf`, `image/jpeg`, or `image/png`, taken from the content signature. |
| `pageCount` | integer | PDF: pages counted; image: `1`. |
| `contentHash` | string | `sha256:` over the stored content. |
| `storageLocation` | string | Protected location of the original; never shown to the user. |
| `origin` | `SourceDocumentOriginDto`, optional | **New.** Present only when the channel has origin metadata; omitted (not `null`) for camera capture. |

State: `received` only. Later processing states belong to downstream features.

## Source Document Origin (new)

Type: `SourceDocumentOriginDto`. Holds channel-specific origin metadata; only the upload member is in scope here (mailbox adds its own in Feature 03).

| Field | Type | Rules |
|---|---|---|
| `upload` | `UploadOriginDto`, optional | Present only for `file-upload` and only when a usable file name exists. |

## Upload Origin (new)

Type: `UploadOriginDto`. Display-only information about the uploaded file.

| Field | Type | Rules |
|---|---|---|
| `originalFileName` | string | Final path segment only; control and Unicode format characters removed (categories Cc and Cf, including bidirectional overrides such as U+202E); trimmed; at most 255 characters; never empty. Never used for type detection, storage paths, or logs. |

## Original File (existing artifact)

Stored in `IDocumentContentStore` under the source document ID with a server-generated name: `original.pdf` for a PDF, `page-1.jpg` or `page-1.png` for an image. Unchanged after storage; byte-identical to the uploaded file; retrievable through `GET /api/source-documents/{sourceDocumentId}/file`. Later features derive pages and text from it (Feature 04) and link by `sourceDocumentId`.

## Upload Limits (new constant)

| Name | Value | Notes |
|---|---|---|
| `SourceDocumentLimits.FileUploadMaxBytes` | 20 x 1024 x 1024 = 20,971,520 | Applies to `file-upload` only. Mirrored in the client; stated in the shared intake contract. |

## Upload Attempt (client state only; not persisted)

Tracks one submission of one selected file in the browser.

| Field | Meaning |
|---|---|
| `file` | The selected file, or none. |
| `quickChecks` | Result of the extension, size, and not-empty checks, with a reason when failing. |
| `idempotencyKey` | Created when the file is selected; reused on retry of the same selection; replaced on a new selection or after success. |
| `status` | See transitions below. |
| `result` | The source document response, or a user-facing rejection reason. |

```text
idle -> selected -> submitting -> received -> idle ("Upload another")
         |    ^          |
         |    |          +--> rejected  (4xx)  -> selected (choose another file) or idle
         |    |          +--> failed    (5xx/network) -> submitting (retry, same key)
         +--> blocked (quick check failed) -> idle (replace or clear)
```

## Validation Summary

| Rule | Where decided | Response |
|---|---|---|
| Extension is `.pdf`, `.jpg`, `.jpeg`, `.png`; size 1 byte to 20 MB | Client (advisory) and server (size, empty) | Inline message before submit; `413`/`400` from server |
| Exactly one `file` part | Server | `400` |
| Declared type is PDF, JPEG, or PNG | Server | `415` |
| Content signature matches declared type | Server | `415` |
| PDF opens without a password; pages counted | Server | `415` |
| Size at most 20 MB | Server (and client) | `413` |
| Not empty | Server (and client) | `400` |
| Authenticated | Server | `401` |
| Same key, same content | Server | `201` with original record |
| Same key, different content | Server | `409` |
| Storage failure | Server | `500`; nothing stored |
