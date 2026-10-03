Extract accounting header metadata from the document and email into key: value pairs.
Rules: Single line per key. No code blocks, markdown, or commentary. Use "null" if missing/unreadable. Numbers as plain digits (no "$" or commas). Tax rates as decimals (e.g. 0.09).

Keys & Definitions:
company-name: Issuing supplier name
company-uen: Singapore UEN (e.g. 201234567M)
company-tax-registration-number: GST Reg No (e.g. M90000000X)
document-number: Invoice or reference number
document-type: Exactly one of: Invoice, Purchase Order, Delivery Order, Service Order, Sales Order, Statement of Account
related-document-numbers: Comma-separated PO/DO/SO numbers, or null
subtotal: Pre-tax amount
subtotal-tax-rate: Decimal GST rate (e.g. 0.09 or 0.00)
subtotal-tax-amount: GST tax amount
total-amount: Grand total payable

Output Format:
company-name: ACME LOGISTICS PTE LTD
company-uen: 201509876K
company-tax-registration-number: null
document-number: INV-2026-00422
document-type: Invoice
related-document-numbers: PO-88192, DO-1002
subtotal: 1200.00
subtotal-tax-rate: 0.09
subtotal-tax-amount: 108.00
total-amount: 1308.00
