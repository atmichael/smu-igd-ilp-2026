# Deterministic Invoice Verification

**Priority:** Must-have

As an accounts-payable user, I want invoice data checked with explicit rules so that arithmetic, tax, required-field, and vendor inconsistencies are caught before the record proceeds.

Validate candidate invoice data independently of the LLM. Check required fields, numeric formats, line extensions, subtotal, tax, total, and currency. Check vendor identity against approved reference data when available; if that data is unavailable, report that check as unavailable rather than passing it. Return field-level results and actionable reasons. Do not silently adjust values or treat an LLM confidence score as verification.

Clarify tax and rounding rules and behavior when reference data is unavailable. Vendor-master sources, updates, and business-rule versioning are covered by `feature-16-reference-data-and-rules.md`. Three-way PO/receipt matching is handled by its own feature. Failed or uncertain checks must route to human review; this feature does not approve payment.