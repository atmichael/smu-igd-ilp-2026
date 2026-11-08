# Deterministic Verification Contract

## Start verification

**POST** `/api/verification-runs`

Request:

```json
{
  "documentId": "document identifier",
  "evidencePackageId": "evidence package identifier",
  "originalInvoiceReferenceId": "optional original-invoice identifier"
}
```

Response:

```json
{
  "verificationRunId": "run identifier",
  "status": "verified|blocked|superseded",
  "documentVariant": "classified document variant",
  "rulePackageVersionId": "effective rule package",
  "completedAt": "timestamp"
}
```

## Get verification run

**GET** `/api/verification-runs/{verificationRunId}`

Returns run identity, status, rule/evidence references, and ordered checks.

## Verification check shape

```json
{
  "verificationCheckId": "check identifier",
  "fieldIdentifier": "stable optional field identifier",
  "checkType": "field-presence|title|adjustment|arithmetic|currency|vendor|numeric-format|applicability",
  "outcome": "pass|fail|unavailable|not-applicable",
  "reasonCode": "stable reason",
  "message": "actionable explanation",
  "extractedValue": "original candidate value, if available",
  "normalizedValue": "normalized representation, if applicable",
  "expectedValue": "deterministic expected value, if applicable",
  "evidenceReferenceIds": ["evidence-reference-id"],
  "ruleReferenceId": "exact rule/rate/rounding reference",
  "requiresReview": true,
  "calculationTraceId": "optional calculation trace identifier"
}
```

## Get calculation trace

**GET** `/api/verification-runs/{verificationRunId}/checks/{verificationCheckId}/calculation-trace`

Returns taxable and displayed amounts, selected method, rounding policy, rate, conversion inputs, intermediate values, and expected results.

## Resolve review item

**POST** `/api/verification-review-items/{reviewItemId}/resolve`

MVP request (signed-in user):

```json
{
  "resolutionReason": "reason for review disposition"
}
```

Resolution response retains the original automated outcome and run identifiers. If evidence or inputs change, the client MUST start a new verification run instead of expecting the automated result to be edited.

## Re-verification

Whenever classification, evidence, rule version, vendor reference, exchange-rate data, or original-invoice data changes, clients call the start-verification endpoint again with the current immutable inputs. The API supersedes or otherwise excludes stale runs and never mutates a completed run.

## Outcome rules

- `pass`: supported evidence and applicable policy produce agreement.
- `fail`: evidence contradicts a determinable requirement or required evidence/value is missing where a requirement is determinable.
- `unavailable`: required reference, rule, rate, conversion, vendor, or relationship data is not determinable from approved inputs.
- `not-applicable`: the effective rule determines that the check does not apply.

A blocked run contains at least one applicable `fail` or `unavailable` check. A run does not approve payment.
