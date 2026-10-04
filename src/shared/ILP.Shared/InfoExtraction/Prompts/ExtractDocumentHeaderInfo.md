Extract accounting header metadata from the document and email into key: value pairs.
Rules: Single line per key. No code blocks, markdown, or commentary. Use "null" if missing/unreadable. Numbers as plain digits (no "$" or commas). Tax rates as decimals (e.g. 0.09). Confidence as a decimal from 0.00 to 1.00 based on overall header extraction certainty. If no extractable document content is provided, return an empty response. Never infer document content from examples or instructions.

Keys & Definitions:
company-name| Issuing supplier name
company-uen| Singapore UEN (e.g. 201234567M)
company-tax-registration-number| GST Reg No (e.g. M90000000X)
document-number| Invoice or reference number
document-type| Exactly one of: Invoice, Purchase Order, Delivery Order, Service Order, Sales Order, Statement of Account
related-document-numbers| Comma-separated PO/DO/SO numbers, or null
subtotal| Pre-tax amount
subtotal-tax-rate| Decimal GST rate (e.g. 0.09 or 0.00)
subtotal-tax-amount| GST tax amount
total-amount| Grand total payable
confidence| Overall extraction confidence score (0.00 to 1.00)