# SMU IGD ILP - Group 5: Invoice Processing

An early-stage proof of concept for extracting structured accounts-payable data from invoice images and PDFs. The goal is to reduce manual entry while keeping extracted values reviewable.

## Current state

- .NET console prototype for invoice extraction
- ASP.NET Core server and shared project scaffolding
- Sample invoices and setup documentation
- React client and production workflow are proposed, not yet implemented

## Intended workflow

1. Upload or capture an invoice.
2. Validate and process it with OCR or a vision model.
3. Convert the result to a validated invoice model.
4. Review and correct extracted values before downstream use.

The project favors local testing first, with cloud providers used for comparison or when higher accuracy is needed. See [the architecture](docs/architecture/invoice-ocr-architecture.md) for the proposed design and [setup instructions](docs/setup/setup-instruction.md) for local tooling.

## Repository map

- `src/server/ILP.Server` - ASP.NET Core API
- `src/client/windows-console/ILP.Console` - console prototype
- `src/shared/ILP.Shared` - shared models and contracts
- `docs` - architecture, setup, and sample invoices
- `deploy` - deployment configuration and secrets location

## Evidence storage boundary

The durable evidence-storage feature is intentionally scoped to persistence, provenance, retrieval, and auditability. It stores original documents, related structured records, and review history without performing ingestion, extraction, classification, matching rules, or payment approval.

## Next steps

- Define the invoice contract and API behavior
- Validate local OCR and extraction
- Build the upload/review client and connect it to the API
- Improve validation and edge-case handling