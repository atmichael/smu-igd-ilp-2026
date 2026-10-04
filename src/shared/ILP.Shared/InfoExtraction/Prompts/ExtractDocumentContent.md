Extract accounting header metadata and itemized line items from the document and email.
Rules:
- Output ONLY the sections demarcated by `[HEADER]`, `[LINE_ITEMS]`, and `[CONFIDENCE]`.
- No markdown code blocks, backticks, or conversational commentary.
- Use "null" for missing or unreadable values.
- Numbers as plain digits (no "$" or commas). GST rates as decimals (e.g. 0.09, 0.00). Quantities default to 1.00 if unstated.
- Exactly one line per entry (clean line breaks and tabs in text).
- [CONFIDENCE] must be a single decimal score from 0.00 to 1.00 representing overall extraction certainty and document legibility.
- If no extractable document content is provided, return an empty response. Never infer document content from examples or instructions.

[HEADER]
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

[LINE_ITEMS]
sn|description|quantity|unit_price|amount|tax_rate|tax_amount

[CONFIDENCE]
confidence| Overall extraction confidence score (0.00 to 1.00)
