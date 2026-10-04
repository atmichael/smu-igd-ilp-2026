Extract all line items from the document table into a pipe-delimited CSV followed by overall confidence.
Rules: Output CSV header row and data rows, then `[CONFIDENCE]`. Exactly one line per item (strip internal newlines/tabs). No markdown blocks or commentary. Use "null" if missing. Numbers as plain digits (no "$" or commas). Quantities default to 1.00 if unstated. Tax rates as decimals (e.g. 0.09, 0.00). Confidence as decimal from 0.00 to 1.00 based on overall table extraction certainty.

Columns & Definitions:
sn| Sequential item index (1, 2, ...)
description| Product or service description
quantity| Billed quantity (default 1.00)
unit_price| Rate per unit before tax
amount| Net line total before tax (quantity * unit_price)
tax_rate| GST rate as decimal (e.g. 0.09 for 9%, 0.00 for ZR/EX)
tax_amount| Stated or calculated line GST amount

Output Format:
sn|description|quantity|unit_price|amount|tax_rate|tax_amount
1|Server Rack Installation & Setup Includes cable management|1.00|1000.00|1000.00|0.09|90.00
2|Overseas Cloud Hosting (US)|1.00|200.00|200.00|0.00|0.00
[CONFIDENCE]
confidence| 0.94