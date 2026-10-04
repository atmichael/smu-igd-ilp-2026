# Document Type Classification

**Priority:** Must-have

As an accounts-payable user, I want each processed document identified by type so that invoice data and matching evidence follow the appropriate workflow.

Classify supported documents as a supplier invoice, buyer purchase order, service order, goods receipt, delivery evidence, service acceptance, or supplier statement. A service order may provide the authorized commitment for a service purchase; service acceptance is the corresponding receipt evidence. A supplier statement is for reconciliation, not three-way matching. A sales order is seller-side and MUST NOT be treated as a buyer purchase order or AP match input.

When classification is uncertain or unsupported, show that status for user review rather than silently assigning a type. Preserve the classification with the source document and extracted text. Clarify how users correct a classification and whether classification applies to a whole document or individual pages. Do not include type-specific field extraction, matching, or payment approval.

This feature owns the document-type vocabulary and builds on the labels the shared extraction prompt (`src/shared/ILP.Shared/InfoExtraction/Prompts/ExtractDocumentHeaderInfo.md`) already returns: Invoice, Purchase Order, Delivery Order, Service Order, Sales Order, and Statement of Account. In code and APIs use their kebab-case form without abbreviations: `invoice`, `purchase-order`, `delivery-order`, `service-order`, `sales-order`, `statement-of-account`. Add `goods-receipt` and `service-acceptance` (needed for three-way matching) and `unclassified` (uncertain or unsupported documents) to the prompt and this list. `ExtractedDocumentMapper.ToDocumentType` maps them to the coarser evidence-storage `documentType` (Feature 08) until that is aligned:

| Classification | Evidence storage today | Record categories (besides `document-header`) |
|---|---|---|
| `invoice` | `invoice` | `invoice-line` |
| `purchase-order`, `service-order` | `purchase-order` | `purchase-order-commitment` |
| `delivery-order`, `goods-receipt` | `receipt` | `goods-receipt` |
| `service-acceptance` | `receipt` | `service-acceptance` |
| `sales-order`, `statement-of-account` | `other-evidence` (not a match input) | none |

A seller-side sales order is not a supported match document; flag it for review rather than mapping it to `purchase-order`.