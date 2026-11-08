# Feature Specification: Deterministic Invoice Verification

**Feature Branch**: `005-deterministic-verification`

**Created**: 2026-11-07

**Status**: Draft

**Input**: User description: "Deterministic invoice verification with explicit document-type rules, source evidence, GST calculations, currency handling, vendor checks, and human-review outcomes"

## Clarifications

### Session 2026-11-07

- Q: When a displayed GST or total differs from its deterministically calculated value, should verification fail on any cent difference or allow a configured rounding tolerance? → A: Fail on any cent difference after the approved rounding policy has been applied.
- Q: If a credit note states "This is not a credit note for GST purposes," supported by written supplier-and-customer agreement, should verification recognize IRAS’s no-GST-adjustment exception? → A: Recognize the exception only when all required IRAS conditions and supporting evidence are present.
- Q: What overall status should a verification run receive when it contains passes but also one or more unavailable checks? → A: Block the entire run pending human review.
- Q: Which users may resolve a failed or unavailable verification check in the MVP? → A: Any signed-in user (option B).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Verify document-specific required fields (Priority: P1)

As an accounts-payable reviewer, I want the required fields for the classified invoice type checked independently of extraction confidence, so that incomplete or unsupported documents cannot proceed as complete.

**Why this priority**: Required-field failures are fundamental validation errors and can affect tax-document validity before any calculation review.

**Independent Test**: Run representative simplified tax invoices, regular tax invoices, and credit notes with complete, missing, blank, illegible, and unsupported values, then verify each field result and its evidence.

**Acceptance Scenarios**:

1. **Given** a document classified as a simplified tax invoice, **When** verification runs with the effective IRAS rule set, **Then** all eight required fields are checked, including supplier identity, address and GST registration, document date and number, supply description, GST-inclusive total, and GST-inclusive pricing statement.
2. **Given** a required value is missing, blank, illegible, or unsupported by evidence, **When** verification evaluates that field, **Then** the result is fail and identifies the field, an actionable reason, the extracted value if any, and source evidence.
3. **Given** required wording differs only in letter case, **When** verification evaluates the wording, **Then** it may pass only when the required meaning is supported by source evidence.
4. **Given** a simplified tax invoice exceeds S$1,000 inclusive of GST, **When** verification completes, **Then** the threshold requirement fails and the result requires human review.
5. **Given** a sales voucher or debit note has been classified as capable of serving as a simplified tax invoice, **When** any simplified-invoice required field is absent, **Then** that field fails rather than treating the document type substitution as sufficient.

---

### User Story 2 - Verify tax invoice and credit-note requirements (Priority: P1)

As an accounts-payable reviewer, I want regular tax invoice and credit-note requirements checked using the document-specific rule set, so that supplier, customer, supply, original-document, and adjustment details are not silently inferred.

**Why this priority**: These document types have distinct legal and reconciliation requirements, and applying the wrong requirements creates false acceptance.

**Independent Test**: Verify valid and invalid representative full tax invoices and credit notes, including zero-rated, mixed-supply, rate-change, and original-invoice adjustment scenarios.

**Acceptance Scenarios**:

1. **Given** a regular tax invoice, **When** verification runs, **Then** it evaluates the title, supplier and customer identities and addresses, GST registration details, issue and identifying information, supply descriptions, applicable GST rates, taxable amounts, GST amounts, and totals.
2. **Given** a zero-rated supply does not state GST at 0%, **When** the supply is verified, **Then** the affected check fails or is unavailable if the governing rule cannot be applied confidently.
3. **Given** supplies include multiple GST treatments, **When** each treatment and gross amount cannot be separately identified as required, **Then** the affected checks fail with specific field identifiers and reasons.
4. **Given** a credit note, **When** verification runs, **Then** it evaluates the prominent title when required, credit number and date, supplier and customer details, original invoice number and date, credited supplies, rates, amounts, credited GST, and total including tax.
5. **Given** a credit note adjusts an original invoice, **When** documentary references or alternative evidence cannot demonstrate the adjustment, **Then** the adjustment check fails rather than passing on a name or amount match alone.
6. **Given** a credit note states "This is not a credit note for GST purposes" with supporting agreement, **When** verification evaluates the credit note, **Then** it recognizes the no-GST-adjustment exception only when all required conditions and evidence are present.

---

### User Story 3 - Verify GST and numeric arithmetic deterministically (Priority: P1)

As an accounts-payable reviewer, I want GST, line extensions, subtotals, totals, formats, and rounding checked by explicit rules, so that extraction errors and inconsistent calculations are visible before review.

**Why this priority**: Arithmetic and rounding directly affect invoice validity and financial outcomes; they must not depend on an LLM or silent reconciliation.

**Independent Test**: Process representative SGD and foreign-currency invoices using subtotal and item-by-item methods, valid and invalid arithmetic, alternative rounding, cash rounding, and missing policy data.

**Acceptance Scenarios**:

1. **Given** applicable invoice lines or subtotals, **When** verification runs, **Then** it checks applicable GST rate, taxable-value multiplication, extensions, subtotals, and total taxable amount plus GST using the effective rule and supplier-selected method.
2. **Given** GST is displayed without the configured rounding to the nearest cent, **When** the displayed evidence and approved rule support no alternative, **Then** the affected calculation check fails with expected and displayed values.
3. **Given** a supported supplier policy permits nearest-five-cent cash rounding, **When** cash rounding is identified separately, **Then** GST remains rounded by its configured policy and the cash-rounding adjustment is reported separately.
4. **Given** a calculation requires unavailable rounding, rate, conversion, or method policy, **When** verification runs, **Then** the affected check is unavailable rather than pass or fail.
5. **Given** displayed amounts conflict or rounding produces a difference, **When** verification reports the result, **Then** it does not silently adjust, reconcile, or replace extracted values.

---

### User Story 4 - Verify foreign-currency and vendor rules (Priority: P2)

As an accounts-payable reviewer, I want foreign-currency conversions and vendor identity checked against approved effective rules, so that uncertain source rates or unknown vendors cannot be treated as verified.

**Why this priority**: Currency policy and vendor identity are necessary inputs for GST and business validation, and missing reference data must fail closed.

**Independent Test**: Verify foreign-currency and credit-note scenarios with approved, unavailable, inconsistent, historical, prevailing, and effective-date exchange-rate inputs, plus known and unknown vendors.

**Acceptance Scenarios**:

1. **Given** amounts requiring GST conversion, **When** verification selects the configured historical or prevailing approved exchange-rate rule, **Then** the conversion uses the effective rate for the applicable effective date and records the rule and source evidence.
2. **Given** foreign-currency conversion data is inconsistent, expired, unavailable, or unsupported for the document effective date, **When** verification checks conversion, **Then** the check is unavailable and identifies the missing or conflicting input.
3. **Given** a credit note relates to a foreign-currency original invoice, **When** required to preserve consistency, **Then** its conversion policy matches the original invoice or the applicable approved alternative is explicitly recorded.
4. **Given** vendor identity is present in approved vendor reference data, **When** verification runs, **Then** it records the matching approved vendor identity and any relevant alias.
5. **Given** vendor or approved jurisdiction-specific reference data cannot be resolved, **When** verification runs, **Then** affected checks are unavailable rather than passed or failed due to an unsupported identity.

---

### User Story 5 - Route outcomes to review without approving payment (Priority: P2)

As an accounts-payable operations owner, I want each check to have an explicit outcome and actionable review information, so that reviewers can distinguish verified success from invalid or unknown checks without payment approval.

**Why this priority**: Clear outcomes preserve auditability and prevent uncertain results from entering downstream processing.

**Independent Test**: Review completed verification runs containing pass, fail, unavailable, and not-applicable results and confirm routing, explanations, evidence preservation, and absence of payment approval.

**Acceptance Scenarios**:

1. **Given** a verification check is valid and supported by evidence, **When** evaluation completes, **Then** the outcome is pass.
2. **Given** a check is contradicted, absent, invalid, illegible, or unsupported by evidence, **When** evaluation completes, **Then** the outcome is fail with a field identifier and actionable reason.
3. **Given** required reference data or policy is unavailable, **When** a check cannot be determined, **Then** the outcome is unavailable and the missing dependency is identified.
4. **Given** a rule does not apply to the document or supply, **When** verification completes, **Then** the outcome is not-applicable and the applicability reason is preserved.
5. **Given** any fail or unavailable outcome, **When** verification is completed, **Then** the item routes to human review; payment approval remains outside the feature.

### Edge Cases

- Required wording or numeric values are present but their source evidence is missing, unreadable, or not tied to the candidate value.
- The classified document variant changes after verification starts; verification must reselect rules and invalidate results based on superseded classification data.
- A sales voucher, debit note, and simplified tax invoice satisfy some but not all variant-specific requirements.
- The invoice effective date has no matching rule version, conflicts with another rule version, or falls before newly introduced requirements.
- Calculations use both subtotal and item-by-item methods, and one line passes while another uses a conflicting method.
- The document contains mixed zero-rated, standard-rated, exempt, and other supply types; each treatment and gross amount must be assessed according to applicable rules.
- The original invoice is not identifiable by supply but has a valid cross-reference or alternative documentary evidence sufficient under the governing rule.
- Foreign-currency conversion rates differ between document date, GST date, historical date, and prevailing date policy.
- Credit notes omit direct line references but are linked by approved credit-to-original-invoice evidence.
- Numeric strings contain unsupported separators, negative values, signs, symbols, or precision that cannot be interpreted deterministically.
- Extraction confidence is high but evidence is absent, and extraction confidence is low but clear evidence supports the value; confidence alone must never determine required-field validity.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST verify candidate invoice data independently of LLM judgment and extraction-confidence scores.
- **FR-002**: The product MUST use the classified document type to select document-specific required-field and calculation rules.
- **FR-003**: Verification MUST be performed against source evidence, the effective tax-rule version, and the original invoice when required by the document type.
- **FR-004**: For an IRAS simplified tax invoice, the product MUST verify the supplier name, supplier address, supplier GST registration number, issue date, document identifying number, supply description, GST-inclusive total payable, and GST-inclusive pricing statement.
- **FR-005**: The product MUST fail the simplified-invoice document check when its GST-inclusive total exceeds S$1,000.
- **FR-006**: A sales voucher or debit note MUST satisfy the applicable simplified-invoice requirements before it is accepted as a simplified tax invoice.
- **FR-007**: For regular tax invoices, the product MUST verify title, supplier and customer identities and addresses, GST registration details, issue and identifying information, supply descriptions, GST rates, taxable amounts, GST amounts, and totals including GST.
- **FR-008**: The product MUST fail or mark unavailable the check for a zero-rated supply that does not identify GST at 0% when the governing evidence or rule cannot support the required treatment.
- **FR-009**: When mixed supply treatments are displayed, the product MUST verify treatment and gross amounts separately where required by the effective rule.
- **FR-010**: For credit notes, the product MUST verify the prominent title where required, credit note number and date, supplier and customer details, original invoice number and date, credited supplies, credited amount, GST rate, credited GST amount, and total including GST.
- **FR-011**: The product MUST verify that a credit note adjusts the corresponding original invoice by supported cross-reference or approved alternative documentary evidence, except where an evidenced IRAS-approved no-GST-adjustment exception applies, including written supplier-and-customer agreement, the fully taxable customer condition, and the statement "This is not a credit note for GST purposes".
- **FR-012**: Missing, blank, illegible, unsupported, conflicting, or unreadable values MUST produce a failed field check when the required value can otherwise be determined.
- **FR-013**: Each failed field check MUST preserve the extracted value, field identifier, source evidence reference, and an actionable reason.
- **FR-014**: Required wording MUST be case-insensitive only when normalization does not alter meaning and source evidence supports the candidate value.
- **FR-015**: For every applicable line or subtotal, the product MUST verify rate application, GST as taxable value multiplied by the effective rate, extension arithmetic, subtotal arithmetic, and total as taxable amount plus GST.
- **FR-016**: The product MUST support the supplier-selected subtotal method and item-by-item method and apply the chosen method consistently.
- **FR-017**: The product MUST apply the effective configured rounding method, with GST and totals rounded to the nearest cent by default unless approved evidence permits an alternative. After the selected approved method has been applied, any remaining cent difference between displayed GST or total and the calculated value MUST fail the check; a pre-rounding difference between calculation methods is not a tolerance.
- **FR-018**: Permitted nearest-five-cent cash rounding MUST be reported separately from GST rounding; verification MUST NOT incorporate cash rounding into GST calculations unless the applicable rule requires it.
- **FR-019**: The product MUST NOT silently adjust or reconcile mismatched amounts.
- **FR-020**: The product MUST validate supported numeric formats and report unsupported formats as failed or unavailable when the governing format policy is unknown.
- **FR-021**: For foreign-currency documents, the product MUST verify Singapore-dollar GST conversion using the configured exchange-rate rule, historical or prevailing basis, effective date, and approved conversion source.
- **FR-022**: Unavailable or inconsistent foreign-currency conversion data MUST produce unavailable conversion checks and preserve the missing or conflicting dependency.
- **FR-023**: Credit-note foreign-currency conversion MUST be consistent with the original invoice where required by the effective rule and MUST use the same configured policy basis.
- **FR-024**: Vendor identity MUST be checked against approved synchronized reference data, including known aliases where approved.
- **FR-025**: If a required vendor record, jurisdiction-specific rule, rate, rounding policy, exchange rate, or original-document relationship is unavailable, the affected checks MUST be unavailable rather than passed.
- **FR-026**: Verification outcomes MUST distinguish pass, fail, unavailable, and not-applicable, with applicability and dependency information.
- **FR-027**: Invalid or uncertain verification outcomes MUST route to human review.
- **FR-028**: The product MUST preserve the extracted value, normalized value where applicable, evidence reference, selected rule version, calculation inputs, and outcome rationale for audit and reproduction.
- **FR-029**: Verification MUST not approve payment or authorize downstream payment.
- **FR-030**: The product MUST re-evaluate affected results when classification, evidence, rule version, vendor reference, exchange-rate data, or original-invoice data changes.
- **FR-031**: A verification run containing one or more applicable unavailable checks MUST be blocked pending human review, even when other checks pass; the run MUST NOT be reported as verified.
- **FR-032**: For the MVP, any signed-in user MUST be able to resolve a failed or unavailable review item. Resolving an item MUST not overwrite the automated outcome; resolution requires a recorded reason and, when evidence or inputs change, a new verification run.

### Key Entities *(include if feature involves data)*

- **Verification Run**: One deterministic evaluation of a classified document using a captured evidence package, effective rule set, and any related original invoice.
- **Verification Check**: An individual required-field, document-rule, vendor, currency, format, or arithmetic assessment with an outcome, reason, field identifier, and rule trace.
- **Verification Outcome**: Pass, fail, unavailable, or not-applicable, including whether the result requires human review.
- **Source Evidence Reference**: A traceable link between an extracted or displayed value and its location in the source document.
- **Effective Business Rule**: A versioned document, tax, rounding, conversion, or credit-note rule that applies on the relevant effective date.
- **Calculation Trace**: The rate, taxable values, displayed values, rounding policy, intermediate arithmetic, conversion inputs, and expected result used for a verification decision.
- **Original Invoice Reference**: Documentary or cross-reference evidence connecting a credit note to the invoice it adjusts.
- **Review Item**: A failed or unavailable check that remains visible for AP human review.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: At least 95% of verification decisions in representative business testing match independently prepared expected outcomes for supported document types, calculations, and rule variants.
- **SC-002**: 100% of required-field failures identify the field, evidence status, actionable reason, and preserved extracted value where one exists.
- **SC-003**: 100% of GST arithmetic test cases covering supported methods, rates, and rounding policies produce either deterministic pass, deterministic fail, or explicit unavailable outcome.
- **SC-004**: In controlled tests, no missing evidence, unsupported conversion, missing rule, unknown vendor, or ambiguous document relationship produces a pass outcome.
- **SC-005**: 100% of verification decisions retain enough rule and calculation provenance to reproduce the outcome, including any unavailable dependency, for audit review.
- **SC-006**: Reviewers identify the next required action for at least 90% of failed or unavailable checks during usability validation.
- **SC-007**: 100% of re-verifications triggered by classification, evidence, rule, vendor, currency, or original-invoice changes do not reuse a stale outcome.
- **SC-008**: No verification workflow or result in representative end-to-end testing constitutes payment approval.
- **SC-009**: At least 95% of representative simplified, regular, and credit-note documents are assigned the correct document-specific rule set without manual correction.

## Assumptions

- The reference-data feature provides approved, effective-dated vendor records, document rules, tax rates, rounding policies, currency rules, and rule provenance.
- Singapore is the initial jurisdiction and IRAS invoicing guidance is the authoritative source for supported GST document requirements.
- Candidate extracted values and source evidence are available from prior intake and evidence-extraction features.
- Human review can be initiated for failed, unavailable, and otherwise uncertain verification outcomes.
- GST is calculated at the nearest cent unless evidence and an effective approved rule support an allowed alternative.
- The feature does not perform purchase-order or receipt matching; those are handled separately.
- Payment authorization, approval, release, or execution remains outside this feature.
