# SMU IGD ILP - Group 5: Invoice Processing

An early-stage proof of concept for extracting structured accounts-payable data from invoice images and PDFs. The goal is to reduce manual entry while keeping extracted values reviewable.

## Current state

- React camera-capture client that submits through the shared source-document intake API
- ASP.NET Core API with source-document intake and a file-backed evidence-storage pilot (`/api/evidence-packages`)
- Mailbox collector and .NET console prototypes for invoice extraction
- Sign-in is a development-only test scheme; access control (Feature 17), upload (Feature 01), and the review workflow are not yet implemented

## Intended workflow

1. Upload or capture an invoice.
2. Validate and process it with OCR or a vision model.
3. Convert the result to a validated invoice model.
4. Review and correct extracted values before downstream use.

The project favors local testing first, with cloud providers used for comparison or when higher accuracy is needed. See [the architecture](docs/architecture/invoice-ocr-architecture.md) for the proposed design and [setup instructions](docs/setup/setup-instruction.md) for local tooling.

## Repository map

- `src/server/ILP.Server` - ASP.NET Core API
- `src/server/ILP.Collector` - mailbox collector prototype
- `src/client/web` - React client (camera capture)
- `src/client/windows-console/ILP.Console` - console prototype
- `src/shared/ILP.Shared` - shared models and contracts
- `tests` - API and shared-model tests
- `specs` - Spec Kit feature specifications, plans, and tasks
- `docs` - architecture, planning (feature briefs and owned terms), setup, and sample invoices
- `setup` - local development dependencies
- `deploy` - deployment configuration and secrets location

## Evidence storage boundary

The durable evidence-storage feature is intentionally scoped to persistence, provenance, retrieval, and auditability. It stores original documents, related structured records, and review history without performing ingestion, extraction, classification, matching rules, or payment approval.

## Next steps

- Define the invoice contract and API behavior
- Validate local OCR and extraction
- Build the upload/review client and connect it to the API
- Improve validation and edge-case handling