# Contracts: Reference Data and Business Rules

These contracts are technology-neutral behavior specifications for planning. The implementation may expose equivalent server endpoints or application commands.

## Publish synchronization intent

**Purpose:** Start an approved source refresh using an update notification, reconciliation trigger, or manual request.

Required request data:

- Source identifier
- Trigger type
- Requested scope
- Requesting actor

Required response states:

- Synchronization run identifier
- Current run state
- Validation and conflict summaries
- Published snapshot identifier when publication succeeds
- Explicit failure reason when publication does not succeed

## Query reference data

**Purpose:** Read only approved reference records needed by a check.

Required query data:

- Reference category
- Canonical identity or alias
- Applicable transaction or effective date
- Required source scope

Required response data:

- Record or explicit unverified reason
- Canonical identity
- Record and snapshot versions
- Source authority and source reference
- Effective period
- Last successful synchronization
- Review status

## Query applicable rule package

**Purpose:** Select the deterministic policy package for a jurisdiction and document variant.

Required query data:

- Jurisdiction
- Document variant
- Transaction or effective date

Required response data:

- Rule package identifier and immutable version
- Source authority, link, review metadata, and effective period
- Applicable GST, required-field, rounding, conversion, adjustment, and tolerance values
- Explicit result when no complete package applies

## Submit rule change

**Purpose:** Request a governed change to reference values, policy, or tolerances.

Required request data:

- Affected object and version
- Proposed effective period
- Changed values
- Reason
- Requesting actor

Required response data:

- Proposed change identifier
- Approval state
- Required approver roles
- Conflict or effective-period validation result

## Approve rule change

Only the tax-policy owner or AP operations lead may approve the governed scope assigned to that role. Requester and approver must differ. Approval records the approver, reason, timestamp, and resulting snapshot or package version.

## Reconcile source

**Purpose:** Compare an authoritative source with the current approved snapshot and stage an atomic candidate snapshot if differences exist.

Required response data:

- Compared source and scope
- Added, changed, unchanged, and removed records
- Validation errors and conflicts
- Candidate snapshot identifier
- Publication state

## Read synchronization status

Required status fields:

- Source
- Latest run
- Current run state
- Trigger type
- Last successful synchronization
- Publication status
- Error summary and recovery guidance

## Read decision trace

Required data:

- Exact reference records and rule package versions
- Source authority for each dependency
- Snapshot identifier
- Effective period selection
- Outcome and unverified reason codes
