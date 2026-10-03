# Model Quality Evaluation

**Priority:** Should-have; prerequisite for straight-through processing

As an invoice-processing maintainer, I want extraction quality measured against reviewed examples so that model, prompt, OCR, or schema changes do not silently reduce accuracy.

Maintain an approved evaluation set with expected document types and invoice fields. Measure field-level correctness, missing-value behavior, schema validity, and relevant OCR quality for each supported processing version. Compare candidate changes against the accepted baseline and report regressions before deployment. Keep evaluation data protected and distinguish test fixtures from operational invoice records.

Clarify how examples are approved or de-identified, which metrics and minimum thresholds apply, how model/OCR/prompt/schema versions are identified, and who approves a baseline change. Do not use an LLM's self-reported confidence as ground truth.