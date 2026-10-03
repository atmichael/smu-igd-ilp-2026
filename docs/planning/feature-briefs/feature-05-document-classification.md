# Document Type Classification

**Priority:** Must-have

As an accounts-payable user, I want each processed document identified by type so that invoice data and matching evidence follow the appropriate workflow.

Classify supported documents as a supplier invoice, buyer purchase order, service order, goods receipt, delivery evidence, service acceptance, or supplier statement. A service order may provide the authorized commitment for a service purchase; service acceptance is the corresponding receipt evidence. A supplier statement is for reconciliation, not three-way matching. A sales order is seller-side and MUST NOT be treated as a buyer purchase order or AP match input.

When classification is uncertain or unsupported, show that status for user review rather than silently assigning a type. Preserve the classification with the source document and extracted text. Clarify how users correct a classification and whether classification applies to a whole document or individual pages. Do not include type-specific field extraction, matching, or payment approval.