# Invoice Three-Way Matching

**Priority:** Should-have

As an accounts-payable user, I want a supplier invoice compared with its buyer purchase order and goods-receipt or service-acceptance evidence so that discrepancies are found before the payable proceeds.

Compare invoice lines and amounts against the authorized order and the quantities received or services accepted. Use deterministic rules and agreed tolerances. Report matched, unmatched, and discrepant items with explanations and links to the source records. Route missing evidence and discrepancies to user review; do not let an LLM decide whether a financial difference is acceptable or approve payment.

Clarify matching identifiers, currency and tax handling, quantity and price tolerances, partial deliveries, service milestones, duplicate invoices, and what happens when evidence is missing. Supplier statements belong to a separate reconciliation workflow and are not a substitute for any of the three match inputs. Keep payment approval and payment execution out of scope.