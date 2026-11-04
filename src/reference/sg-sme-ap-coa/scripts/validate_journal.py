#!/usr/bin/env python3
"""Validate draft AP journal JSON: exact balance and permitted ledger codes."""
from __future__ import annotations
import csv
import json
from decimal import Decimal, InvalidOperation
from pathlib import Path
import sys

ROOT = Path(__file__).resolve().parents[1]
COA_PATH = ROOT / "references" / "chart_of_accounts.csv"

def validate(data: dict) -> list[str]:
    with COA_PATH.open(encoding="utf-8-sig", newline="") as handle:
        coa = {r["Code"]: r for r in csv.DictReader(handle)}
    errors = []
    journal = data.get("journal")
    if not isinstance(journal, list) or not journal:
        return ["Missing journal lines"]
    debit_total = Decimal("0")
    credit_total = Decimal("0")
    for i, line in enumerate(journal, start=1):
        code = str(line.get("account_code", ""))
        if code not in coa:
            errors.append(f"Line {i}: unknown account {code}")
        elif coa[code]["Enabled"] != "Yes":
            errors.append(f"Line {i}: account {code} is disabled")
        try:
            debit = Decimal(str(line.get("debit", "0")))
            credit = Decimal(str(line.get("credit", "0")))
        except InvalidOperation:
            errors.append(f"Line {i}: nonnumeric amount")
            continue
        if not debit.is_finite() or not credit.is_finite():
            errors.append(f"Line {i}: nonfinite amount")
            continue
        if debit < 0 or credit < 0:
            errors.append(f"Line {i}: negative debit or credit")
        if (debit > 0 and credit > 0) or (debit == 0 and credit == 0):
            errors.append(f"Line {i}: enter debit or credit, but not both/neither")
        if debit.as_tuple().exponent < -2 or credit.as_tuple().exponent < -2:
            errors.append(f"Line {i}: amount has more than two decimals")
        debit_total += debit
        credit_total += credit
    if debit_total != credit_total:
        errors.append(f"Unbalanced journal: Dr {debit_total}, Cr {credit_total}")
    if data.get("status") == "POSTED" and not data.get("accounting_system_posting_id"):
        errors.append("POSTED requires accounting system posting ID")
    if data.get("posted") is True and not data.get("accounting_system_posting_id"):
        errors.append("posted=true requires accounting system posting ID")
    return errors

if __name__ == "__main__":
    input_file = Path(sys.argv[1]) if len(sys.argv) > 1 else ROOT / "assets" / "sample_output.json"
    output = validate(json.loads(input_file.read_text(encoding="utf-8")))
    print("PASS: Valid and balanced draft journal" if not output else "FAIL:\n" + "\n".join(output))
    sys.exit(0 if not output else 1)
