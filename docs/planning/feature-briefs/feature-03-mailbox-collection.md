# Mailbox Document Collection

**Priority:** Should-have

As an accounts-payable user, I want to collect business-document attachments from an authorized mailbox so that emailed invoices can enter the same processing workflow as uploaded documents.

The user can identify a relevant message and attachment, import a supported document, and see whether collection succeeded. Preserve enough source-message information to trace an imported document back to its email. Do not delete or alter a message as part of import unless that behavior is explicitly agreed.

Specify the mailbox interaction and access permissions. In particular, clarify whether users select messages or the system scans a mailbox automatically, and how duplicate attachments are handled. Keep OCR, classification, AP matching, and payment approval out of scope.

Submit each supported attachment through the [shared source-document intake contract](../../../specs/001-camera-capture/contracts/source-document-intake.md#channels) with `channel` = `mailbox`: one attachment is one source document, and `origin.mailbox` keeps the message ID, sender, subject, received time, attachment name, and attachment index. Detect duplicates by message ID, attachment index, and content hash. The current collector prototype (`src/server/ILP.Collector`) sends attachments straight to extraction and must be moved onto this path; it must also stop printing email bodies, delete temporary attachment files, and read the mailbox address and credential location from configuration.