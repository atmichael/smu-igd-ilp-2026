# Processing Reliability and Deduplication

**Priority:** Should-have

As an accounts-payable user, I want interrupted or temporarily failed document processing to recover safely so that retries do not lose documents or create duplicate invoices and records.

Track processing state across ingestion, text extraction, LLM extraction, verification, and persistence. Retry transient failures safely, allow authorized reprocessing, and make permanent failures visible with an actionable reason. Detect likely duplicate documents using source identifiers and document data; flag possible duplicates for review rather than silently discarding them. Reprocessing the same source must not create duplicate records or duplicate audit events that appear to be separate documents.

Clarify retry limits, failure categories, idempotency keys, duplicate-matching criteria, and who can retry or discard failed work. Do not automatically mark duplicate invoices as valid or payable.