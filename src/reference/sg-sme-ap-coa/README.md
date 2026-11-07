# Singapore SME AP COA Skill

This package is a **starter template** for a Singapore SME AP Document Agent / AP Agent.

### Files
- `SKILL.md`: operating instructions and JSON output contract
- `references/chart_of_accounts.csv`: 38 example GL codes; `Enabled=No` means an optional account that needs company approval before AI can recommend it
- `references/gst_tax_codes.csv`: internal GST candidates; **not** Xero native tax-code identifiers
- `references/mapping_examples.csv`: training/QA examples
- `references/posting_rules.md`: journal, tax and exception rules
- `assets/vendor_master_template.csv`: separate supplier master, blank on purpose
- `assets/sample_input.json`: fictional test input
- `assets/sample_output.json`: fictional balanced journal
- `scripts/validate_journal.py`: validate COA codes and journal balance

### Getting started
1. Review/replace starter codes with your real company's ledger; activate project accounts if needed.
2. Confirm the company GST status, tax policy, capitalization threshold and supporting document profiles.
3. Place this folder in your agent's skills directory, or give Codex/your agent access to the folder and request `$sg-sme-ap-coa`.
4. Test on the fictional sample: `python3 scripts/validate_journal.py`.
5. Process real invoices only in draft / review mode first. Do not auto-post, pay, or modify Gmail.

This is not an audited production accounting policy. Seek qualified accounting/tax review before live implementation.
