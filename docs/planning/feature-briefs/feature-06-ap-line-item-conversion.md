# Schema-Constrained Invoice Extraction

**Priority:** Must-have

As an accounts-payable user, I want a classified supplier invoice converted into consistent AP invoice fields and line items so that the payable can be reviewed and matched, using an inference option appropriate for our cost and data-handling needs.

Use schema-constrained extraction to convert classified supplier-invoice content into candidate header fields and payable line items. Content may include extracted text or supported source-page images. Support a configured choice between local Ollama inference and hosted OpenRouter inference; when local inference is selected, invoice content must not be sent to the hosted provider. Return data in the same provider-independent invoice JSON contract, represent absent values explicitly, and never fabricate values. Preserve traceability from candidate values to source pages or text, and record the provider and model used. Purchase orders and receipt documents use their own evidence schemas and never become invoice lines.

Define required fields, malformed-output behavior, and provider-independent contract expectations during specification. Treat model output as untrusted. Do not include deterministic financial verification, PO/receipt extraction, matching, payment approval, or persistence in this feature.