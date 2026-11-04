# Singapore SME AP Coding Skill Translation

| Skill workflow | Classification | Application implementation |
|---|---|---|
| 1. Accept only invoice or credit-note documents | Deterministic | `AccountsPayableRuleEvaluator` rejects other document types and suppresses journal generation. |
| 2. Extract invoice fields and retain source evidence | LLM-assisted, then deterministic completeness checks | Existing `EmailInfoExtractionProvider` and `ExtractedDocumentParser` provide structured fields; AP input carries field/line evidence references and the evaluator rejects missing required evidence. The current extractor does not itself generate citations, so this remains an upstream input responsibility. |
| 3. Verify vendor identity and approved status | Deterministic gate over verified business evidence | Request carries explicit vendor-whitelist and UEN-verification results; missing or failed checks require review. The current repository has no supplier-master approval or bank-change data. |
| 4. Detect duplicate invoices | Deterministic | Check vendor identifier, document type, and invoice number against stored documents; matching candidates require review and include amount/date/currency comparison. |
| 5. Restrict coding to enabled COA accounts | Deterministic | Persist the full COA reference in SQLite; reject disabled and non-coding control/payment/tax/liability accounts as expense suggestions. |
| 6. Classify each invoice line | LLM-assisted | `AccountsPayableCodingProvider` requests a separate suggestion per line; server validates line coverage and account eligibility, and uncertain suggestions require review. |
| 7. Propose GST treatment | LLM-assisted proposal plus deterministic evidence gate | Provider proposes a persisted GST code; evaluator permits `SG_STD_9` only when registration, invoice evidence, and claimability are confirmed, otherwise routes to `SG_UNKNOWN`/review. |
| 8. Reconcile amounts and journal balance | Deterministic | Decimal arithmetic checks line totals, tax, gross amount, and debit-credit equality; mismatches prevent journal generation. |
| 9. Apply supplier document requirements | Deterministic gate over supplied evidence | Required-document profile and completeness are explicit request inputs; missing matching documents require review. Supplier-specific requirements are not yet maintained in a supplier master. |
| 10. Prepare journal and audit details | Deterministic | Build an unposted draft using eligible suggestions and selected tax treatment; never invoke a live accounting integration. Credit notes remain review-only until the original invoice coding/tax treatment is available to reverse. |
| 11. Choose final status | Deterministic | Return `READY_FOR_APPROVAL` only when every required validation passes; otherwise return `NEEDS_REVIEW`. `POSTED` is never emitted. |

## Feature mapping

- Shared request/recommendation DTOs, AP-specific provider, prompt, and JSON parser: `src/shared/ILP.Shared/AccountsPayable/`.
- Deterministic validation, reference-data entities and query service, duplicate lookup, and authenticated endpoints: `src/server/ILP.Server/Features/AccountsPayable/`.
- COA and GST dropdown values: `InboxExtractionDbContext` EF migration and migration seed data. The CSV files under `src/reference/sg-sme-ap-coa/references/` remain the editable source used to maintain the seed.
- TDD coverage: parser/provider request tests in `ILP.Shared.Test`; evaluator and SQLite migration/reference-data tests in `ILP.Server.Tests`.

The feature prepares recommendations only. Vendor whitelist, UEN verification, tax-claimability, and supporting-document completeness are explicit upstream evidence inputs because the current application has no authoritative vendor master, company tax-policy, or matching-profile tables.
