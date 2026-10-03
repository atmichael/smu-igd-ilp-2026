# Three-Way Match Review and Resolution

**Priority:** Should-have

As an accounts-payable user, I want to inspect a three-way match result and resolve its exceptions so that valid invoice records can proceed and unresolved discrepancies remain visible.

Show the overall and line-level match status, the reason for each unmatched or discrepant item, and links to the invoice, buyer order, and receipt or service-acceptance evidence used in the comparison. Let the user correct invoice or evidence data through the appropriate review workflow, link the correct source records, or add missing evidence through the supported intake flow. After a correction, allow the user to rerun the deterministic match and view the updated result.

Preserve the history of match results and user changes. A matched status is not payment approval. Do not allow users to silently override matching tolerances or mark a discrepancy as matched without an explicitly authorized and auditable exception process. Clarify user roles, status names, and actions for missing or disputed evidence. Keep payment approval and payment execution out of scope.