# Feature Specification: Reference Data and Business Rules

**Feature Branch**: `004-reference-data-and-rules`

**Created**: 2026-11-07

**Status**: Draft

**Input**: User description: "Reference Data and Business Rules"

## Clarifications

### Session 2026-11-07

- Q: Which system is authoritative for vendor master data? → A: ERP vendor master

- Vendor identities and aliases MUST be synchronized from the ERP vendor master.

- Q: Which source is authoritative for Singapore GST rules? → A: IRAS GST invoicing guidance

- Singapore GST rules MUST use IRAS GST invoicing guidance as the authoritative source, retaining the source link and review metadata.

- Q: Which system is authoritative for purchase orders and receipts used by matching? → A: ERP purchasing records

- Purchase orders and receipts used by matching MUST be synchronized from the ERP purchasing records.

- Q: How should approved reference data be refreshed from its authoritative source? → A: Source updates trigger refresh, with scheduled reconciliation

- Approved operational reference data MUST be refreshed when the authoritative source reports an update, where update notifications are supported. A daily reconciliation MUST detect changes even when notifications are unavailable, and authorized users MUST be able to request an on-demand refresh. Records are not marked stale based solely on age.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Apply approved reference data to invoice checks (Priority: P1)

As an accounts-payable reviewer, I want invoice and matching checks to use approved vendor, tax, invoice-format, and purchasing reference data, so that verification reflects current business records and policy.

**Why this priority**: This is the foundation for trustworthy invoice verification and prevents checks from relying on unsupported assumptions.

**Independent Test**: Can be tested by applying a vendor record, tax-rule set, document-format rule, and purchase-order reference to a sample invoice and confirming which checks can run and how results are reported.

**Acceptance Scenarios**:

1. **Given** an invoice classified as a full tax invoice, **When** the reviewer runs supported checks, **Then** the system applies the full-tax-invoice requirements and displays the applicable vendor, tax, and source-rule versions.
2. **Given** an invoice contains a vendor identity or purchasing reference not represented in approved reference data, **When** checks are requested, **Then** the system marks affected checks as unverified rather than pass or fail based on missing data.
3. **Given** reference data is available for the effective date of the invoice, **When** checks run, **Then** the result uses that effective version and provides enough metadata to reproduce the decision.

---

### User Story 2 - Select the correct tax-document rule (Priority: P1)

As an accounts-payable reviewer, I want rules for each supported tax-document type to be applied only to the matching classified document, so that full tax invoices, simplified tax invoices, receipts, debit notes, sales vouchers, and credit notes are evaluated correctly.

**Why this priority**: Applying requirements from the wrong document variant can produce false acceptance or rejection and has direct financial consequences.

**Independent Test**: Can be tested by presenting each supported document classification with representative valid and invalid examples and confirming the applicable requirements and outcomes.

**Acceptance Scenarios**:

1. **Given** a document is classified as a simplified tax invoice, **When** validation runs, **Then** only simplified-invoice requirements and applicable thresholds are applied.
2. **Given** a document is classified as a credit note, **When** validation runs, **Then** credit-note adjustment requirements are applied and full or simplified tax-invoice requirements are not substituted.
3. **Given** the same document could qualify as more than one variant, **When** no unambiguous classification rule resolves it, **Then** the system leaves the variant result for human review rather than silently choosing one.

---

### User Story 3 - Maintain traceable Singapore GST policy (Priority: P1)

As a tax-policy owner, I want Singapore GST requirements and calculation policy to be preserved with review and effective-date metadata, so that invoice checks remain based on authoritative requirements even when rates or methods change.

**Why this priority**: GST rate, threshold, rounding, foreign-currency conversion, and adjustment rules change policy outcomes and require auditable provenance.

**Independent Test**: Can be tested by reviewing an invoice against rule versions before and after a known effective date, including GST calculation, rounding, currency conversion, and credit-note adjustment scenarios.

**Acceptance Scenarios**:

1. **Given** an invoice is subject to a policy that is effective on its transaction date, **When** validation runs, **Then** the result identifies the selected GST rate, required fields, rounding method, and effective period.
2. **Given** the authoritative invoicing source is reviewed or updated, **When** the review metadata changes, **Then** the affected rule package records the reviewer, review date, and source link without silently changing interpretation.
3. **Given** a policy method or effective GST rate changes, **When** users compare previous and current decisions, **Then** both decisions retain their original rule version and the changed policy is treated as versioned policy data.

---

### User Story 4 - Report and govern reference-data changes (Priority: P2)

As an AP operations owner, I want unavailable reference data or failed synchronization to be visible and changes to rule data and tolerances to be controlled and audited, so that users do not mistake incomplete checks for successful checks.

**Why this priority**: Operational visibility and change control are required to keep matching reliable over time.

**Independent Test**: Can be tested by simulating synchronized, unavailable, failed, conflicting, and changed reference records and confirming status, blocking behavior, and audit history.

**Acceptance Scenarios**:

1. **Given** the most recent synchronization for a required reference failed, **When** a check depends on it, **Then** the check is shown as unverified and reports the failed synchronization and last successful update.
2. **Given** a required reference source is unavailable, **When** a check depends on it, **Then** no successful result is presented for that check and users receive the affected reference, source, and next action.
3. **Given** an authorized change is made to reference data, a rule, or a tolerance, **When** the change is recorded, **Then** the audit history identifies the previous version, changed value, effective date, actor, and reason.

---

### Edge Cases

- A document has a transaction date outside the effective period of every matching rule version; affected checks remain unverified.
- Reference data is newly synchronized but conflicts with an existing approved record.
- A source update arrives after its declared effective date or before an approved review is complete.
- A reference source becomes unavailable partway through a synchronized update; partial updates are never presented as authoritative.
- A user changes external vendor, tax, purchase-order, or receipt data outside this feature; those authoritative records remain unchanged.
- GST calculations, rounding, foreign-currency conversion, or credit-note adjustments are required but the necessary policy value is missing.
- A document classification changes after reference rules have been selected; the new classification triggers re-evaluation under the correct variant.
- Rule, rate, and tolerance changes overlap in effective dates or create conflicting versions.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The product MUST provide traceable approved reference data for vendor identities and aliases.
- **FR-002**: The product MUST provide traceable reference data for applicable tax rules, invoice formats, credit-note rules, currency rules, purchase-order references, and receipt references.
- **FR-003**: The product MUST associate each reference record or rule package with source authority, source link where applicable, effective start and end dates, review status, and review metadata.
- **FR-004**: The product MUST retain Singapore invoicing requirements from the authoritative IRAS source: https://www.iras.gov.sg/taxes/goods-services-tax-(gst)/basics-of-gst/invoicing-price-display-and-record-keeping/invoicing-customers
- **FR-005**: The product MUST record applicable GST rate, simplified-invoice S$1,000 threshold, required fields, GST rounding and calculation methods, approved foreign-currency conversion rules, and credit-note adjustments.
- **FR-006**: The product MUST distinguish full tax invoices, simplified tax invoices, receipts, debit notes, sales vouchers, and credit notes and apply requirements for the document's classified variant only.
- **FR-007**: The product MUST select reference data and rule versions by the document or transaction's applicable effective date, with an explicit outcome when no applicable version exists.
- **FR-008**: The product MUST change GST rate or calculation-method versions only as governed policy data and MUST preserve the rule version used for each decision.
- **FR-009**: The product MUST detect missing, conflicting, failed, and unavailable reference data required by a check.
- **FR-010**: A check affected by missing, conflicting, failed, or unavailable reference data MUST NOT be reported as successful; it MUST be reported as unverified with the affected reference and recovery guidance.
- **FR-011**: The product MUST report synchronization status and the last successful update of each synchronized reference or source.
- **FR-012**: The product MUST support controlled refresh when an authoritative source reports an update; daily reconciliation when notifications are unavailable; and authorized on-demand refresh.
- **FR-013**: The product MUST detect partial, failed, or conflicting synchronization and prevent partial results from becoming authoritative.
- **FR-014**: The product MUST preserve the complete history of rule, rate, tolerance, and reference-data changes, including previous and current values, effective dates, actor, reason, and approval status where required.
- **FR-015**: The product MUST allow users to trace every reference-dependent check to the exact reference or rule version used.
- **FR-016**: The product MUST provide a current review status and next-review or expected-review information for authoritative policy sources.
- **FR-017**: The product MUST prevent payment approval and edits to authoritative external vendor, tax, purchase-order, or receipt systems within this feature.
- **FR-018**: The product SHOULD support aliases and equivalent vendor, currency, and reference representations without merging records that have different identities.
- **FR-019**: The product MUST identify ambiguous classification-to-rule matches and require human review when the applicable document variant cannot be determined.
- **FR-020**: The product MUST define and test GST rounding and foreign-currency conversion outcomes for supported tax-document scenarios.
- **FR-021**: Reference-dependent checks MUST use an approved synchronized snapshot and MUST record the source and last successful synchronization.

### Key Entities *(include if feature involves data)*

- **Reference Data Record**: An approved vendor, alias, currency, purchase-order, receipt, tax, format, or credit-note reference with provenance and validity metadata.
- **Business Rule Package**: A versioned collection of document checks and policy values that applies to a defined jurisdiction, document variant, and effective period.
- **Document Variant**: A classified tax-document type, including full tax invoice, simplified tax invoice, receipt, debit note, sales voucher, or credit note.
- **Policy Source**: An authoritative source such as a government publication or approved internal system, including source authority, link, review owner, and update metadata.
- **Reference Synchronization Run**: An attempted import or update showing source, scope, outcome, last successful update, conflicts, and whether the result became authoritative.
- **Rule Decision Trace**: The recorded relationship between a check outcome and the exact reference record or rule package version used.
- **Change Audit Entry**: A historical record of a change, approval, actor, reason, effective date, and previous and current values.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: In validation of representative invoices, 100% of checks that depend on reference data identify the exact source record or rule version used, or show an explicit unverified status.
- **SC-002**: During validation with controlled reference-data scenarios, 100% of missing, conflicting, failed, unavailable, or out-of-coverage records prevent false success for affected checks.
- **SC-003**: Across supported document variants, 100% of test cases apply only the requirements associated with the classified variant.
- **SC-004**: For GST policy, calculation, currency-conversion, and credit-note adjustment scenarios, at least 95% complete without unsupported assumptions or incorrect variant selection in representative business testing.
- **SC-005**: Users can determine why a reference-dependent check is unverified and identify the required recovery action in 90% of tested failure scenarios.
- **SC-006**: 100% of governed reference, rule, rate, and tolerance changes contain traceable actor, reason, effective date, previous value, current value, and approval status when approval is required.
- **SC-007**: Reviewers can reproduce the rule and reference versions used for a completed invoice check within 2 minutes for 90% of audited decisions.

## Assumptions

- Singapore requirements are in scope because the feature brief identifies the IRAS invoice guidance as the authoritative product reference.
- Additional jurisdictions may be supported, but each jurisdiction requires its own identified authoritative source, effective rules, and review ownership.
- Controlled synchronization may include scheduled, triggered, or manually initiated updates; each reference-data category has its own update schedule.
- External vendor, tax, purchase-order, and receipt systems remain authoritative for their respective records.
- The tax-policy owner and AP operations lead approve governed rule and tolerance changes; the requester and approver must be separate roles.
- Conflicting, overlapping, retroactive, unavailable, or partially synchronized updates are not published as authoritative; affected checks remain unverified until a complete and approved resolution is available.
- Payment approval remains outside this feature.
- Editing of authoritative external systems remains outside this feature.

## Resolved Product Decisions

- **Reference-data governance**: Vendor, tax, purchasing, and policy data MUST use a source-system-per-category model. Each category has a named authoritative source, responsible data owner, and update schedule.
- **Rule governance**: The tax-policy owner and AP operations lead are the authorized business approvers for GST rules, other policy rules, and tolerances. Requesters and approvers MUST be separate roles.
- **Conflict handling**: Conflicting, overlapping, retroactive, unavailable, or partially synchronized updates MUST fail closed. The affected data or checks remain unverified and under human review until a complete and approved resolution is published.
