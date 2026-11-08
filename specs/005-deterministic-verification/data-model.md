# Data Model: Deterministic Invoice Verification

## Verification Run

A verification run is one immutable deterministic evaluation of a document and its supplied inputs.

- `verificationRunId`: unique identifier.
- `documentId`: transaction document or source document identifier.
- `documentVariant`: classified full, simplified, credit-note, or other supported variant.
- `effectiveDate`: date used to select rules.
- `rulePackageVersionId`: exact effective reference-data package.
- `evidencePackageId`: evidence snapshot used.
- `originalInvoiceReferenceId?`: related invoice evidence when required.
- `status`: verified, blocked, or superseded.
  - `verified`: every applicable check passed.
  - `blocked`: one or more applicable checks failed or were unavailable.
  - `superseded`: inputs changed and a new verification run was created.
- `startedAt`, `completedAt`: execution timestamps.
- `inputVersion`: content fingerprint or immutable input snapshot identifier.

## Verification Check

An individual required-field, document-rule, vendor, currency, format, credit-note, or arithmetic assessment.

- `verificationCheckId`: unique identifier.
- `verificationRunId`: owning run.
- `fieldIdentifier?`: stable required field or business calculation identifier.
- `checkType`: field-presence, title, adjustment, arithmetic, currency, vendor, numeric-format, or applicability.
- `outcome`: pass, fail, unavailable, not-applicable.
- `reasonCode`: stable reason identifier.
- `message`: actionable human-readable reason.
- `extractedValue?`: preserve the candidate value.
- `normalizedValue?`: normalized representation where applicable.
- `expectedValue?`: deterministic expected value.
- `evidenceReferenceIds`: source evidence supporting or contradicting the check.
- `ruleReferenceId?`: exact rule/rate/rounding item used.
- `requiresReview`: true for fail or unavailable.

## Calculation Trace

Immutable arithmetic and conversion inputs for an applicable calculation check.

- `calculationTraceId`: unique identifier.
- `verificationCheckId`: owning check.
- `calculationMethod`: subtotal or item-by-item.
- `taxableValue`: unrounded taxable amount.
- `effectiveRate?`: configured GST rate.
- `calculatedGstAmount?`: GST after the selected rounding policy.
- `displayedGstAmount?`: amount from the source evidence.
- `calculatedTotal?`: deterministic expected total.
- `displayedTotal?`: displayed total.
- `roundingPolicy`: nearest-cent or approved alternative.
- `cashRoundingAmount?`: separate nearest-five-cent adjustment.
- `currencyCode`, `exchangeRateBasis?`, `exchangeRate?`, `exchangeRateDate?`, `convertedAmount?`
- `inputs`: ordered item/subtotal calculation inputs.

## Review Item

A failed or unavailable check awaiting human resolution.

- `reviewItemId`: unique identifier.
- `verificationRunId`: owning run.
- `verificationCheckId`: affected check.
- `status`: open or resolved.
- `resolutionReason?`: reviewer-entered reason.
- `resolvedByUserId?`: signed-in user.
- `resolvedAt?`: resolution timestamp.
- `newVerificationRunId?`: run created after changed evidence or inputs.

Review resolution is a workflow decision, not a changed automated outcome. Automated outcomes remain immutable.

## Relationships and invariants

- One verification run has one or more verification checks.
- Each calculation check may have one calculation trace.
- Each failed or unavailable check creates one open review item in the MVP.
- A review item belongs to at most one active resolution record.
- A verification run has exactly one overall status derived from its applicable checks.
- Missing evidence cannot produce pass.
- Missing required policy cannot produce pass; it produces unavailable.
- Classification, evidence, rule, vendor, exchange-rate, or original-invoice changes create a new run rather than mutate an existing run.

## Canonical reuse

Verification uses existing `DocumentDto`, document-item, evidence package, evidence-document, structured-record, source evidence, and reference-data rule contracts. Verification-owned entities are introduced only for run, check, calculation trace, outcome, and review resolution concepts.
