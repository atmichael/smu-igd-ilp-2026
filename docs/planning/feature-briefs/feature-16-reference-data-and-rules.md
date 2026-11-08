# Reference Data and Business Rules

**Priority:** Should-have

As an accounts-payable user, I want invoice and matching checks to use approved vendor, tax, and purchasing reference data so that verification reflects current business records and policies.

Provide controlled, traceable reference data for vendor identity and aliases, applicable tax, invoice-format, credit-note, and currency rules, and purchase-order or receipt references used by matching. Include jurisdiction-specific requirements for regular tax invoices, simplified tax invoices, receipts, and credit notes, with effective dates, source authority, and official-source links. For Singapore IRAS rules, retain the applicable GST rate, simplified-invoice S$1,000 threshold, required fields, GST rounding and calculation methods, approved foreign-currency conversion rules, and credit-note adjustments.

Define how data is imported or synchronized, how stale or unavailable data is reported, and how rule changes are versioned and audited. A missing, stale, or unavailable reference must not be treated as a successful check. Rules must distinguish full tax invoices, simplified tax invoices, receipts, debit notes, sales vouchers, and credit notes, and must apply the required variant only to the classified document. GST and rounding policy must be explicit and consistently versioned; treat a change in method or effective rate as policy data rather than silent document interpretation.

The authoritative product reference for the invoicing requirements is the IRAS page `https://www.iras.gov.sg/taxes/goods-services-tax-(gst)/basics-of-gst/invoicing-price-display-and-record-keeping/invoicing-customers`. Maintain its review and effective-date metadata rather than relying only on an annually assumed GST rate.

Clarify authoritative source systems, update frequency, effective dates, data ownership, and who may change rules or tolerances. Keep payment approval and editing the authoritative external systems out of scope.