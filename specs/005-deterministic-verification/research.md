# Phase 0 Research: Deterministic Invoice Verification

## Decision 1: Verification inputs and evidence

- **Decision:** Verification consumes the existing `DocumentDto` candidate, its evidence package references, the effective reference-data rule package, and the original invoice when required.
- **Rationale:** These inputs already separate extraction candidates from source evidence and provide effective-date rule provenance.
- **Alternatives considered:** Re-reading source documents for every check; rejected because it duplicates evidence storage and can introduce inconsistent extracted values. Verifying extracted JSON without evidence; rejected because required fields would not be reproducibly supported.

## Decision 2: Verification outcome model

- **Decision:** Use `Pass`, `Fail`, `Unavailable`, and `NotApplicable` as check outcomes. A run is `Verified` only when every applicable check passes; any applicable fail or unavailable check blocks the run for review.
- **Rationale:** The outcome vocabulary distinguishes invalid evidence from absent policy dependencies and prevents mixed results from becoming successful verification.
- **Alternatives considered:** Boolean pass/fail; rejected because it cannot represent missing rules or non-applicable checks. “Unverified” alone for both failures and unavailable checks; rejected because reviewers need different recovery actions.

## Decision 3: GST calculation method and rounding

- **Decision:** Use `decimal`, evaluate either supplier-selected item-by-item or subtotal GST, round GST and displayed totals to cents by the approved policy, and compare only after the selected rounding policy has been applied. Report nearest-five-cent cash rounding separately and never include it in GST.
- **Rationale:** This follows IRAS guidance that total GST may be rounded to the nearest cent and both calculation methods may differ under rounding. Cash rounding may be applied consistently to the amount payable, separately from GST.
- **Alternatives considered:** Floating-point arithmetic; rejected for financial calculations. Comparing before rounding; rejected because both valid IRAS methods may legitimately differ.

## Decision 4: Recalculation and recomputation

- **Decision:** Preserve original candidate values and produce an immutable calculation trace. Never overwrite mismatched values or silently reconcile them; after changed evidence or inputs, create a new verification run.
- **Rationale:** Audit reproducibility and the non-reconciliation requirement demand additive traces rather than corrected source values.
- **Alternatives considered:** Normalizing extracted values automatically; rejected because unsupported correction could erase the extraction defect.

## Decision 5: Foreign-currency policy

- **Decision:** Use the effective approved historical or prevailing exchange-rate rule and effective date. Credit notes use the original invoice policy where required and report inconsistent or missing rates as unavailable.
- **Rationale:** IRAS guidance permits consistent historical or prevailing treatment for foreign-denominated credit notes under its stated customer conditions.
- **Alternatives considered:** Selecting a live exchange rate; rejected because verification requires approved effective-date provenance. Treating unavailable conversion as a failed calculation; rejected because policy absence is not evidence the arithmetic is wrong.

## Decision 6: Credit-note GST exception

- **Decision:** Recognize a no-GST-adjustment credit note only when evidence supports the written supplier-and-customer agreement, fully taxable customer status, and exact required statement.
- **Rationale:** The exception is conditional and must not infer GST treatment from the credit-note label or confidence.
- **Alternatives considered:** Always adjusting GST; rejected because it would override a permitted documented exception. Recognizing the statement alone; rejected because mandatory conditions would be unsatisfied.

## Decision 7: Persistence boundary

- **Decision:** Persist verification runs and review resolutions through the server’s existing persistence pattern, with immutable run snapshots and append-only review resolution metadata.
- **Rationale:** Re-runs can remain reproducible while resolution history does not overwrite automated evidence.
- **Alternatives considered:** Storing only derived UI state; rejected because replay and audit provenance would be lost.

## Decision 8: MVP review authorization

- **Decision:** Any signed-in user can resolve review items in the MVP; resolution records user identity and reason, does not alter automated outcomes, and does not approve payment. Resolution-triggering input changes require a new run.
- **Rationale:** This matches the accepted MVP scope while preserving deterministic verification integrity and avoiding premature role-management implementation.
- **Alternatives considered:** Authorized-reviewer configuration; deferred because the project does not yet provide frontend user/role administration. Automatic resolution; rejected because it undermines human review.
