# Quickstart: Deterministic Invoice Verification

## Prerequisites

- .NET SDK from `global.json`.
- Existing backend and reference-data dependencies configured.
- Representative source documents with evidence packages and extracted `DocumentDto` values.
- Effective Singapore GST rules and vendor reference data available for the selected document effective date.

## Validation scenarios

### 1. Simplified tax invoice

1. Create a simplified tax invoice with all eight required fields and a total no greater than S$1,000 including GST.
2. Run verification.
3. Confirm every applicable check passes and the run is verified only if source evidence supports each required value.
4. Repeat with one missing or unsupported value and confirm exactly one field check fails and the run is blocked.

### 2. Regular tax invoice arithmetic

1. Create lines under the subtotal method and item-by-item method.
2. Run valid examples and confirm GST totals match the selected method and cent rounding.
3. Change a displayed GST value by one cent after rounding and confirm the calculation check fails without changing the displayed value.
4. Repeat nearest-five-cent cash rounding as a separately documented adjustment.

### 3. Mixed supplies and credit notes

1. Verify a regular invoice containing standard-rated and zero-rated supplies.
2. Confirm zero-rated supplies identify 0% GST and gross amounts are separated where required.
3. Verify a credit note referencing an original invoice.
4. Repeat with unsupported original-invoice evidence and confirm the adjustment check fails.
5. Verify a supported no-GST-adjustment credit note containing the required statement and complete written-agreement evidence.

### 4. Foreign currency

1. Verify an invoice using an approved historical or prevailing exchange-rate rule.
2. Confirm the run records exchange source, rate, basis, effective date, and converted amounts.
3. Remove the applicable rate or introduce conflicting rates and confirm conversion checks are unavailable rather than passed.
4. Verify a credit note against an original foreign-currency invoice and confirm policy consistency.

### 5. Review resolution

1. Sign in as an MVP test user.
2. Open a blocked verification run containing a failed or unavailable check.
3. Confirm the reason, evidence, and recovery action are visible.
4. Record a resolution reason and resolve the review item.
5. Confirm the automated outcomes remain unchanged; if inputs or evidence change, create and run a new verification run.
6. Confirm no resolution creates payment approval.

## Expected test commands

```powershell
 dotnet test tests/ILP.Server.Tests/ILP.Server.Tests.csproj
 npm test --prefix src/client/web
```

Run focused server tests with the feature test filters after implementation creates the verification test classes.
