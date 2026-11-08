# Data Model: Reference Data and Business Rules

## Reference Data Record

A locally approved copy of a source record required for invoice or matching checks.

| Field | Meaning | Validation |
|---|---|---|
| `referenceDataRecordId` | Stable local identifier | Required; immutable |
| `category` | Vendor, tax, currency, format, purchase order, receipt, credit note, or another governed category | Required |
| `identity` | Canonical key used to match the record | Required; unique within category and source record |
| `sourceSystem` | Authoritative system or publication | Required |
| `sourceRecordId` | Identifier assigned by the source, when available | Optional for document sources with a stable URL |
| `sourceLink` | Source URL or integration reference | Required for published policy sources; source-system reference otherwise |
| `values` | Category-specific approved values | Must be valid for category |
| `effectiveStart` | First date on which values apply | Required |
| `effectiveEnd` | Last date on which values apply, if bounded | Optional |
| `status` | Draft, pending approval, approved, superseded, or rejected | Required |
| `version` | Immutable local version | Required |
| `createdAt`, `createdBy` | Creation audit | Required |

Relationships: belongs to a source registration; may have aliases and one current approved snapshot.

## Reference Alias

A non-canonical value that identifies the same underlying reference record.

- `aliasValue` must not cause two different canonical identities to merge.
- Conflicting aliases require human resolution.
- Alias provenance records the source and synchronization run.

## Business Rule Package

A versioned collection of deterministic checks for a jurisdiction, document variant, and effective period.

| Field | Meaning |
|---|---|
| `businessRulePackageId` | Stable package identifier |
| `jurisdiction` | Jurisdiction whose rules are defined |
| `documentVariant` | Classified document type |
| `effectiveStart`, `effectiveEnd` | Applicability period |
| `sourceAuthority` | Source publication or internal authority |
| `sourceLink` | Authoritative source link |
| `ruleValues` | Required fields, thresholds, rounding, conversions, adjustments, and tolerances |
| `reviewStatus` | Current review state |
| `reviewOwner`, `reviewedAt`, `nextReviewAt` | Review metadata |
| `version` | Immutable rule version |

The package must be reproducible; a GST rate or calculation method change creates a new policy version rather than mutating an existing decision package.

## Policy Source

Represents IRAS or another authorized policy authority.

Required metadata includes authority name, official link, category, responsible reviewer, review status, last reviewed date, next review date, and synchronization registration.

## Reference Synchronization Run

Records one refresh attempt against an authoritative source.

States: `Started`, `Staged`, `Validated`, `Approved`, `Published`, `Failed`, and `Rejected`.

Required outcome fields: source, trigger type, start and end time, requested scope, record counts, validation errors, conflicts, last successful synchronization, and whether an atomic snapshot was published. A failed run never replaces the prior approved snapshot.

## Reference Snapshot

An atomic, approved set of reference records and rule packages used by checks.

- A snapshot is immutable once published.
- A snapshot is selected only for complete categories required by the check.
- Reconciliation creates a candidate snapshot rather than modifying the current snapshot.
- Publishing records source refresh timestamp and reviewer approval when approval is required.

## Rule Decision Trace

Records exactly which reference records and rule package versions produced a check outcome.

Required fields: check identity, document or transaction identity, snapshot identity, reference record identities and versions, rule package identities and versions, selected effective dates, outcome, and timestamp. The outcome is `Passed`, `Failed`, `Unverified`, or `RequiresReview` with an explicit reason code.

## Change Audit Entry

Records governed changes to records, rules, tolerances, or publication approval.

Required fields: changed object, previous version, new version, changed values, reason, actor, effective date, approval status, approver, and timestamp.

## State model

### Synchronization run

`Started → Staged → Validated → Approved → Published`

Failure transitions: `Started`, `Staged`, or `Validated` may transition to `Failed`; `Validated` may transition to `Rejected` when governance approval is denied.

### Reference record

`Draft → Pending approval → Approved → Superseded` or `Rejected`. Only an approved record can be published in an authoritative snapshot.
