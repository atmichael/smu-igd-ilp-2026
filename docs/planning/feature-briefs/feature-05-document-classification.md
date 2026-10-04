# Document Type Classification

**Priority:** Must-have

As an accounts-payable user, I want each processed document identified by type so that invoice data and matching evidence follow the appropriate workflow.

Classify supported documents as a supplier invoice, buyer purchase order, service order, goods receipt, delivery evidence, service acceptance, or supplier statement. A service order may provide the authorized commitment for a service purchase; service acceptance is the corresponding receipt evidence. A supplier statement is for reconciliation, not three-way matching. A sales order is seller-side and MUST NOT be treated as a buyer purchase order or AP match input.

When classification is uncertain or unsupported, show that status for user review rather than silently assigning a type. Preserve the classification with the source document and extracted text. Clarify how users correct a classification and whether classification applies to a whole document or individual pages. Do not include type-specific field extraction, matching, or payment approval.

This feature owns the document-type vocabulary. Use these kebab-case names without abbreviations in code and APIs: `supplier-invoice`, `purchase-order`, `service-order`, `goods-receipt`, `delivery-evidence`, `service-acceptance`, `supplier-statement`, plus `unclassified` for uncertain or unsupported documents. Evidence storage (Feature 08) currently stores a coarser `documentType`; align it with this list when this feature is specified:

| Classification | Evidence storage today | Record category |
|---|---|---|
| `supplier-invoice` | `invoice` | `invoice-line` |
| `purchase-order`, `service-order` | `purchase-order` | `purchase-order-commitment` |
| `goods-receipt`, `delivery-evidence` | `receipt` | `goods-receipt` |
| `service-acceptance` | `receipt` | `service-acceptance` |
| `supplier-statement` | `other-evidence` (not a match input) | none |

A seller-side sales order is not a supported match document; flag it for review rather than mapping it to `purchase-order`.