# Schema-Constrained Invoice Extraction

**Priority:** Must-have

As an accounts-payable user, I want a classified supplier invoice converted into consistent AP invoice fields and line items so that the payable can be reviewed and matched.

Use a schema-constrained LLM to convert classified supplier-invoice text into candidate header fields and payable line items. Return data in the agreed invoice JSON contract, represent absent values explicitly, and never fabricate values. Preserve traceability from candidate values to source pages or text. Purchase orders and receipt documents use their own evidence schemas and never become invoice lines.

Define required fields, malformed-output behavior, and provider-independent contract expectations during specification. Treat model output as untrusted. Do not include deterministic financial verification, PO/receipt extraction, matching, payment approval, or persistence in this feature.