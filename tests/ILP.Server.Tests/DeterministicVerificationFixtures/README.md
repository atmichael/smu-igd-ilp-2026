# Deterministic Verification Fixtures

Use these fixture rules when adding representative verification cases:

- `simplified-valid.json`: all eight required fields, evidence package, total 850 SGD, passing subtotal GST.
- `simplified-over-threshold.json`: total 1,000.01 SGD; threshold check fails.
- `regular-mixed-supply.json`: standard-rated and zero-rated lines with separately identified gross amounts.
- `credit-note-valid.json`: supported original-invoice reference and matching adjustment values.
- `credit-note-currency.json`: historical exchange policy consistent with original invoice.
- `cash-rounding.json`: GST remains nearest-cent rounded; nearest-five-cent adjustment is separate.

Fixtures should preserve source values and evidence references. They must not represent recalculated values as original evidence.
