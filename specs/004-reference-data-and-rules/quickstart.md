# Quickstart Validation: Reference Data and Business Rules

## Prerequisites

- A test ERP vendor source and test ERP purchasing source are available, or deterministic source fixtures are loaded.
- IRAS review metadata and GST policy fixtures are approved.
- The test environment records synchronization runs and reference versions.
- Authorization distinguishes rule/request changes from approval.

## Scenario 1: Vendor alias check

1. Refresh vendors from the ERP fixture.
2. Run a check for a known vendor alias.
3. Confirm the result resolves to the canonical vendor record and reports its source record and snapshot version.
4. Run the same check with the alias removed from the next source fixture.
5. Confirm the result remains unverified until the changed approved snapshot is published.

## Scenario 2: Atomic synchronization

1. Start a refresh containing valid records and one invalid record.
2. Confirm no records from the run become part of the approved snapshot.
3. Confirm the synchronization run is failed and the prior complete snapshot remains available.
4. Correct the fixture and rerun.
5. Confirm publication changes the snapshot atomically.

## Scenario 3: Document-variant policy

1. Run representative full tax invoice, simplified tax invoice, receipt, debit note, sales voucher, and credit note cases.
2. For each case, confirm only requirements for that classified variant are evaluated.
3. Change the classification and rerun without changing the extracted document values.
4. Confirm the decision trace changes to the corresponding rule package.

## Scenario 4: Singapore GST effective periods

1. Run a case before an approved GST-effective date.
2. Run a case on and after the effective date.
3. Confirm the selected rate, fields, rounding method, currency conversion method, and adjustment rules match the effective rule package.
4. Run a case with missing policy metadata.
5. Confirm the affected checks are unverified.

## Scenario 5: Source update and reconciliation

1. Add a source update and trigger the update notification path.
2. Confirm the candidate snapshot is staged and published after validation and required approval.
3. Repeat without a notification and confirm daily reconciliation detects the same change.
4. Request an authorized manual refresh.
5. Confirm source and synchronization metadata are recorded.

## Scenario 6: Failed approval and conflict

1. Create two approved source values for overlapping effective periods.
2. Confirm publication is rejected and checks remain unverified.
3. Resolve the overlap through the governed approval workflow.
4. Confirm only the resolved snapshot can be published.

## Expected validation evidence

- Unit tests cover effective periods, version selection, aliases, atomic publication, audit fields, and document variants.
- Integration tests cover source adapters and synchronization failure/recovery.
- Contract tests cover reference query, synchronization, and decision-trace responses.
- Validation records show no successful check can omit or infer required reference provenance.
