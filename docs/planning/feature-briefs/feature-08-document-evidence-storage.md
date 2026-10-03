# AP Documents and Matching Evidence Storage

**Priority:** Must-have

As an accounts-payable user, I want confirmed invoice, purchase-order, and receipt records retained with their source and review status so that matching is traceable and documents can be retrieved later.

Store the original document and structured records with provenance that distinguishes extracted values from user corrections. Keep invoice payable lines separate from purchase-order commitments and receipt/service-acceptance evidence. Maintain an audit event trail for source references, model and schema versions, verification results, user corrections, and match outcomes. Do not put raw invoice contents or credentials in ordinary logs. A failed save must not appear successful.

Clarify retention, duplicate handling, draft storage, and the identifiers used to associate records. Keep this feature to persistence and retrieval; do not include ingestion, extraction, classification, matching rules, or payment approval.