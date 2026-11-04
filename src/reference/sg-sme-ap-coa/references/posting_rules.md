# AP Posting Rules and Decision Controls

## Authority and source of truth
1. Actual company-approved COA, tax policy, capitalization policy and vendor master outrank this starter template.
2. A company accountant must approve COA account activation and any tax logic before live posting.
3. Use bank, project, fixed-asset and tax-control accounts only for their stated purposes. No arbitrary account creation.

## Invoice posting (standard local purchase)
If supplier tax invoice is valid, buyer is GST-registered, and input tax is claimable:
- Debit eligible expense / direct cost / asset for net amount
- Debit 1200 GST Input Tax Recoverable for eligible GST
- Credit 2000 Accounts Payable - Trade for gross payable

Example SGD 1,000 office stationery + SGD 90 GST:
- Dr 6000 Office Supplies 1,000
- Dr 1200 GST Input Tax Recoverable 90
- Cr 2000 Accounts Payable - Trade 1,090

No GST claimed:
- Debit relevant expense/asset including non-recoverable tax if appropriate
- Credit 2000 AP Trade for full amount
No fabricated input GST.

Payment of previously recorded bill:
- Dr 2000 AP Trade
- Cr 1000 Operating Bank
Payment must have separate approval and bank confirmation; bank account is **not** selected as the invoice expense.

Credit note linked to approved original invoice:
- Dr 2000 AP Trade
- Cr original eligible expense/asset and (if appropriate) input GST
Amounts and account codes reflect original tax/coding; do not assume tax can be reversed if treatment unclear.

## Exceptions requiring review
- First-time vendor, unverified UEN, conflicting vendor details, changed bank account
- Suspected duplicate invoice/credit note
- Missing PO/DO/GRN where required by supplier profile
- Material differences in PO/DO/invoice quantity or price
- Unclear invoice amount, currency, tax evidence, bad arithmetic
- Overseas supplier, reverse charge, withholding tax, import GST, foreign currency conversion
- Blocked GST or exempt / out-of-scope treatment uncertainty
- Prepayment, fixed assets, deposits, retention, GRNI, accruals
- Unapproved COA or disabled account
- Unmatched credit note
- SOA only or support document without invoice

## Account selection
- Office consumable: 6000
- Office rent: 6010; annual payment may need asset account 1300
- Electricity / water: 6020
- Telecom: 6030
- Monthly SaaS: 6040; annual SaaS may be 1300
- Repairs and maintenance: 6050; capitalize major improvement if required
- Professional services: 6060
- Operating courier: 6070
- Insurance: 6090, or 1300 if prepayment
- Project direct materials: 5010 **only if enabled**; stock acquisition might be 1500 instead
- Project subcontract: 5020 **only if enabled**
- Project equipment rental: 5030 **only if enabled**
- Capital item: 1600, human confirmation of company threshold/policy required
- Any uncertain item: `NEEDS_REVIEW`; do not auto-place in 6130.

## Tax codes
The `gst_tax_codes.csv` labels are **internal proposed labels**, not regulator-standard codes and not necessarily ERP tax codes. Singapore current GST standard rate is 9%, but GST recoverability is case-specific. Supplier GST registration alone does not prove a particular invoice's input tax can be claimed. Do not automatically assume 9% on all domestic, service or imported purchases.

## Supporting records
Store source document hash/reference, invoice date/no, supplier identity, currency, source tax evidence, chosen COA, tax code, journal, user/agent action, timestamps, review decision, missing docs, and export acknowledgement. Preserve original unmodified source. Mask sensitive information when reporting.

## Status
`READY_FOR_APPROVAL` = reviewed by rules but NOT posted.
`NEEDS_REVIEW` = cannot safely proceed without accountant/user review.
`REJECTED` = explicitly rejected by human.
`POSTED` = only after accounting system confirms transaction, outside this skill.

## Sources
- IRAS current GST rate: https://www.iras.gov.sg/taxes/goods-services-tax-%28gst%29/basics-of-gst/current-gst-rates
- IRAS GST business responsibilities: https://www.iras.gov.sg/taxes/goods-services-tax-%28gst%29/basics-of-gst/responsibilities-of-a-gst-registered-business
- IRAS GST record keeping: https://www.iras.gov.sg/taxes/goods-services-tax-%28gst%29/basics-of-gst/invoicing-price-display-and-record-keeping/keeping-records
- IRAS corporate record keeping: https://www.iras.gov.sg/taxes/corporate-income-tax/basics-of-corporate-income-tax/record-keeping-requirements
