# Scanned Document Upload

**Priority:** Must-have

As an accounts-payable user, I want to upload an invoice PDF or image so that it can enter the document-processing workflow.

The user selects one PDF or image, sees whether it can be accepted, and receives a clear error if the file is unsupported, unreadable, or too large. The original remains available and associated with normalized pages and extracted text.

Keep this feature to manual upload of one document at a time. Do not include camera capture, mailbox collection, document classification, AP matching, or payment approval.

Submit through the [shared source-document intake contract](../../../specs/001-camera-capture/contracts/source-document-intake.md#channels) with `channel` = `file-upload` and one `file` part, so uploads produce the same source-document record as camera capture and mailbox collection. Keep the original file name only as display metadata in `origin.upload`; never trust it for type detection or storage paths.