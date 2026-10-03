# Straight-Through Review Orchestration

**Priority:** Should-have

As an accounts-payable user, I want records that meet approved quality checks to move to a ready state automatically while uncertain records go to an exception queue so that routine invoices need less manual handling.

Route a record to ready only when required fields are present, deterministic verification passes, and configured confidence criteria have been evaluated against a representative, reviewed dataset. Route low-quality OCR, uncertain extraction, failed checks, missing matching evidence, and discrepancies to the appropriate human queue with reasons. Log every routing decision. A ready record is not approved for payment.

Clarify quality thresholds, who can change routing policy, and how automation can be disabled. Complete `feature-20-model-quality-evaluation.md` before enabling straight-through routing. Do not allow model-reported confidence alone to auto-clear a record or let this workflow approve or pay invoices.