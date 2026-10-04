Extract accounting header metadata and itemized line items from the document and email.
Rules:
- Output ONLY the sections demarcated by `[HEADER]`, `[LINE_ITEMS]`, and `[CONFIDENCE]`.
- No markdown code blocks, backticks, or conversational commentary.
- Use "null" for missing or unreadable values.
- Numbers as plain digits (no "$" or commas). GST rates as decimals (e.g. 0.09, 0.00). Quantities default to 1.00 if unstated.
- Exactly one line per entry (clean line breaks and tabs in text).
- [CONFIDENCE] must be a single decimal score from 0.00 to 1.00 representing overall extraction certainty and document legibility.

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

Output Format:
[HEADER]
company-name| ACME LOGISTICS PTE LTD
company-uen| 201509876K
company-tax-registration-number| null
document-number| INV-2026-00422
document-type| Invoice
related-document-numbers| PO-88192, DO-1002
subtotal| 1200.00
subtotal-tax-rate| 0.09
subtotal-tax-amount| 108.00
total-amount| 1308.00
[LINE_ITEMS]
sn|description|quantity|unit_price|amount|tax_rate|tax_amount
1|Server Rack Installation & Setup Includes cable management|1.00|1000.00|1000.00|0.09|90.00
2|Overseas Cloud Hosting (US)|1.00|200.00|200.00|0.00|0.00
[CONFIDENCE]
confidence| 0.96