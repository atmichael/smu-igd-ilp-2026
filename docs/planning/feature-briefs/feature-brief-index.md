# Feature Briefs for Spec Kit

These files are concise inputs, not completed specifications. Feed one brief at a time to `/speckit-specify`. Priority is recorded here rather than in filenames so the files remain stable as the roadmap changes.

## Feature Workflow

1. Choose the next brief from the priority and dependency lists below.
2. In Copilot Chat at the repository root, run `/speckit-specify` and tell it to read the selected brief as the feature description. For example: `/speckit-specify Read docs/planning/feature-briefs/feature-02-camera-capture.md and use it as the feature requirements. Preserve its scope and flag unresolved product decisions; do not implement the feature.`
3. Review the generated `specs/NNN-feature-name/spec.md` and its `checklists/requirements.md`. The specification should describe user needs and acceptance outcomes, not prescribe implementation details.
4. Resolve any important open questions with `/speckit-clarify`, then continue with `/speckit-plan`, `/speckit-tasks`, `/speckit-analyze`, and `/speckit-implement`. Use `/speckit-converge` to check for remaining work.

Specify creates one feature per invocation and updates `.specify/feature.json` to point to the active feature. Complete or pause that feature's workflow before running `/speckit-specify` for another brief, so downstream commands continue to target the intended spec.

See [the five-layer AP processing flow](../ap-three-way-matching-flow.md). Only supplier invoices create AP invoice lines. Orders describe what was authorized; receipt or service-acceptance documents describe what was received. Basic audit events are required from the first release; analytics dashboards can follow.

## Must Have: MVP

Build in this dependency order:

1. [Scanned document upload](feature-01-scanned-document-upload.md) - accept one PDF or image.
2. [Camera capture](feature-02-camera-capture.md) - capture a paper invoice image.
3. [Document normalization and text extraction](feature-04-raw-text-extraction.md) - use PDF text when available, otherwise preprocess and OCR.
4. [Document classification](feature-05-document-classification.md) - route each type to the correct schema.
5. [Schema-constrained invoice extraction](feature-06-ap-line-item-conversion.md) - return candidate invoice JSON.
6. [Deterministic verification](feature-12-deterministic-verification.md) - validate required fields, arithmetic, tax, and vendor data when a reference source exists.
7. [Human review](feature-07-extraction-review-dashboard.md) - correct uncertain or invalid candidate data.
8. [Document, record, and audit storage](feature-08-document-evidence-storage.md) - retain provenance, corrections, and processing events.

This delivers an image/PDF-to-reviewed-invoice MVP. Advanced quality scoring and inbox ingestion can follow. Do not enable automated clean-record completion until the model-evaluation feature is complete.

## Should Have: Phase 2 and Production Readiness

Work in this suggested order; some items can proceed in parallel:

9. [Mailbox collection](feature-03-mailbox-collection.md) - begin with user-selected messages and attachments.
10. [Procurement evidence extraction](feature-09-procurement-evidence-extraction.md) - structure buyer orders and receipt/service-acceptance evidence.
11. [Document packet tracking](feature-15-document-packet-tracking.md) - show what arrived, what is processing, and which expected documents are pending.
12. [Three-way matching](feature-10-three-way-matching.md) - deterministically compare invoice, buyer order, and receipt evidence.
13. [Match review and resolution](feature-11-three-way-match-review.md) - resolve missing or discrepant evidence and rerun matching.
14. [Straight-through review orchestration](feature-13-straight-through-review-orchestration.md) - move only demonstrably clean records to a ready status; never approve payment.

Three-way matching is part of the target product, but it depends on reliable purchase-order, receipt, vendor, and tolerance data that are not present in the current invoice-only samples. Access control is required before exposing real invoice data to multiple users. Model evaluation is a prerequisite for straight-through processing.

## Good to Have: Stretch Goal

15. [Audit analytics](feature-14-audit-analytics.md) - dashboards for extraction accuracy, overrides, exception rates, and error trends. Capture the underlying audit events in the MVP so the metrics can be added later.
# Feature Briefs for Spec Kit

These files are concise inputs, not completed specifications. Feed one brief at a time to `/speckit-specify`. Priority is recorded here rather than in filenames so the files remain stable if the roadmap changes.

See [the five-layer AP processing flow](ap-three-way-matching-flow.md). Only supplier invoices create AP invoice lines. Orders describe what was authorized; receipt or service-acceptance documents describe what was received. Basic audit events are required from the first release; analytics dashboards can follow.

## Must Have: MVP

Build in this dependency order:

1. [Scanned document upload](feature-01-scanned-document-upload.md) - accept one PDF or image.
2. [Camera capture](feature-02-camera-capture.md) - capture a paper invoice image.
3. [Document normalization and text extraction](feature-04-raw-text-extraction.md) - use PDF text when available, otherwise preprocess and OCR.
4. [Document classification](feature-05-document-classification.md) - route each type to the correct schema.
5. [Schema-constrained invoice extraction](feature-06-ap-line-item-conversion.md) - return candidate invoice JSON.
6. [Deterministic verification](feature-12-deterministic-verification.md) - validate required fields, arithmetic, tax totals, and vendor data when a reference source exists.
7. [Human review](feature-07-extraction-review-dashboard.md) - correct uncertain or invalid candidate data.
8. [Document, record, and audit storage](feature-08-document-evidence-storage.md) - retain provenance, corrections, and processing events.

This delivers a useful image/PDF-to-reviewed-invoice slice. It does not yet promise automatic mailbox processing or three-way matching.

## Should Have: Phase 2

Implement when mailbox access and purchase/receipt evidence sources are agreed:

9. [Mailbox collection](feature-03-mailbox-collection.md) - begin with user-selected messages and attachments.
10. [Procurement evidence extraction](feature-09-procurement-evidence-extraction.md) - structure buyer orders and receipt/service-acceptance evidence.
11. [Document packet tracking](feature-15-document-packet-tracking.md) - show what has arrived, what is processing, and which expected documents are pending.
12. [Three-way matching](feature-10-three-way-matching.md) - deterministically compare invoice, buyer order, and receipt evidence.
13. [Match review and resolution](feature-11-three-way-match-review.md) - let users resolve missing or discrepant evidence and rerun matching.
14. [Straight-through review orchestration](feature-13-straight-through-review-orchestration.md) - move only demonstrably clean records to a ready status; never approve payment.

Three-way matching is part of the target product, but it depends on reliable purchase-order, receipt, vendor, and tolerance data that are not present in the current invoice-only samples.

The `feature-XX` prefixes are stable brief identifiers; use the ordering in these priority sections as the suggested implementation sequence.

## Good to Have: Stretch Goal

14. [Audit analytics](feature-14-audit-analytics.md) - dashboards for extraction accuracy, overrides, exception rates, and error trends. Start collecting the underlying audit events in the MVP so these metrics can be added later.