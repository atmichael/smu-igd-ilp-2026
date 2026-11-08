# Deterministic Invoice Verification

**Priority:** Must-have

As an accounts-payable user, I want invoice data checked with explicit rules so that arithmetic, tax, required-field, and vendor inconsistencies are caught before the record proceeds.

Validate candidate data independently of the LLM and select the required-field and calculation rules from the classified document type. Verification inputs must include source evidence, effective tax rules, and, when applicable, the original invoice.

For an IRAS simplified tax invoice, deterministically verify the presence of the eight minimum fields in Feature 16's versioned IRAS rule: supplier name; supplier address; supplier GST registration number; invoice issue date; invoice identifying number; description of goods or services supplied; total amount payable including GST chargeable; and a GST-inclusive pricing statement such as "Price payable includes GST." A supplier may issue a simplified tax invoice only when the GST-inclusive total does not exceed S$1,000. A sales voucher or debit note may double as a simplified tax invoice only if all required fields are present.

For a regular tax invoice, verify the tax-invoice title, supplier and customer identity and address details, GST registration number, issue and identifying information, supply descriptions, applicable GST rate, amount excluding GST, GST amount, and total including GST. Flag zero-rated supplies that do not state GST at 0%; when mixed supply types are present, verify their treatment and gross amounts are separately identified where required.

For a credit note, verify the prominent title where required for GST rate-change cases, credit note number and date, supplier and customer details, original invoice number and date, detailed credited supplies, credited amount, GST rate, credited GST amount, and total credited including tax. A credit note must adjust the corresponding invoice, and the original invoice may be cross-referenced or supported by alternative documentary evidence where the individual supply cannot be identified.

Treat a missing, blank, illegible, or unsupported value as a failed field check. Report each missing or invalid field with its field identifier and an actionable reason, while preserving the extracted value and source evidence. Required-field wording may be normalized case-insensitively, but must not be inferred from an extraction-confidence score or passed when source evidence is absent.

Verify GST calculations deterministically. For each applicable line or subtotal, verify rate application, GST = taxable value × effective rate, extension and subtotal arithmetic, and total = taxable amount + GST, using the configured rounding method. The subtotal method and item-by-item method must be allowed, but the supplier's selected method and rounding policy must be applied consistently. GST and totals should be rounded to the nearest cent unless the rule set and displayed evidence support an approved alternative; document any permitted nearest-five-cent cash rounding separately from GST. Do not silently adjust or reconcile amounts.

For foreign-currency documents, verify that amounts used for GST are converted to Singapore dollars using the configured approved exchange-rate rule, historical versus prevailing rule, and effective date; inconsistent or unavailable conversion data must be reported as unavailable. Credit-note foreign-currency handling must be consistent with the original invoice where required and use consistently applied exchange-rate policy.

Check numeric formats, vendor identity against approved reference data, and arithmetic. If reference data or a jurisdiction-specific business rule is unavailable, report that check as unavailable rather than passing it. Clarify pass, fail, unavailable, and not-applicable outcomes; invalid and uncertain checks route to human review. This feature does not approve payment.

The applicable requirements are based on the IRAS guidance at `https://www.iras.gov.sg/taxes/goods-services-tax-(gst)/basics-of-gst/invoicing-price-display-and-record-keeping/invoicing-customers`. Versioning, effective dates, authoritative sources, and updates are covered by `feature-16-reference-data-and-rules.md`. Three-way PO/receipt matching remains a separate feature.