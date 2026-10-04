# Quickstart: Document Evidence Storage Validation

This guide validates the persistence, retrieval, provenance, audit, and status-handling behavior for the document evidence package feature. For terminology and how the entities connect, see the [data model overview](data-model.md#overview).

## Prerequisites

- .NET 10 SDK installed.
- Evidence store configured under `EvidenceStorage` in `src/server/ILP.Server/appsettings.json`: `Provider` (`File` by default, `InMemory` for tests), `RootPath` for evidence packages (default `App_Data/evidence-store`), `ContentProvider` for original files received through intake (`MySql` in Development, using connection string `IlpDatabase`; `File` stores them under `ContentRootPath`, default `App_Data/source-documents`), and `RetentionYears` (default `3`). Folders under `App_Data` are git-ignored.
- For `MySql`, a running database: `setup/Start-DevDependencies.ps1` (Docker) or `setup/Start-LocalDatabase.ps1` (MariaDB, no Docker or admin rights); see [local setup](../../docs/setup/setup-instruction.md#1-start-the-database).
- Access to the existing .NET test project at `tests/ILP.Server.Tests`.
- Optional: a test environment that can exercise a real invoice, purchase order, and receipt evidence set without exposing credentials or raw invoice contents in logs.

## Start the API

From the repository root:

```powershell
dotnet run --project .\src\server\ILP.Server\ILP.Server.csproj
```

The API exposes `/api/evidence-packages` using the shared contract layer and does not call ingestion, extraction, or matching services. All routes require authentication; in the Development environment the test scheme accepts an `Authorization: Test <value>` header (the API does not start in other environments until Feature 17).

## Automated checks

From the repository root:

```powershell
dotnet test .\tests\ILP.Server.Tests\ILP.Server.Tests.csproj
```

`EvidenceStorageTests.cs` covers:

- create, retrieval by package ID, and query by case ID, document ID (evidence or intake), source reference, or review status
- deduplication of final evidence on create and finalize, and explicit replacement that supersedes the prior version
- draft/final status transitions, finalize preconditions, and status changes
- provenance across extraction, correction, verification, and finalization
- failed saves shown as `failed` (create) or unchanged prior state (finalize), never success
- match-outcome linkage, redaction of sensitive audit text, authorization, enum validation, retention/archive gating, retrieval time budget, and file-store durability

## Manual validation scenarios

1. Create a draft package containing an invoice, purchase order, and receipt for the same case, then finalize it.
   - Expected result: the package is persisted with source documents and structured records linked by package ID and case ID, and becomes `confirmed` only after finalize.
2. Retrieve the package by case ID and then by invoice or document ID.
   - Expected result: the same package and linked records are returned without losing the original source document metadata.
3. Create a record where an extracted value is later corrected by a user or reviewer.
   - Expected result: the raw value and approved value are both retained, and the provenance shows the change and timestamp.
4. Save a draft or pending package and finalize it.
   - Expected result: the draft remains separate and the final package is only created after explicit finalization.
5. Submit the same source document for the same case again after the first package was finalized, without explicit replacement.
   - Expected result: the second submission returns `409`; with `replacesEvidencePackageId` and a reason it becomes a new version that supersedes the first on finalize.
6. Trigger a storage failure after evidence is assembled but before final commit.
   - Expected result: the package remains failed or pending and is never shown as successful.
7. Record a match outcome against the evidence package.
   - Expected result: the match outcome remains linked to the evidence package and supporting records used in the decision, flagged incomplete if any record is missing.
8. Validate retention metadata and archive lifecycle on a finalized evidence package.
   - Expected result: `retentionUntil` is 3 years after `finalizedAt`, archive returns `412` before then, and an archived package withholds content and values.

## Expected outcomes

- Finalized evidence packages are stored with provenance, review status, and audit history.
- Retrieval is available by package identifier, case, source document, source reference, or review status.
- Draft and final evidence remain separate lifecycle states.
- Duplicate final records are prevented unless a legitimate replacement workflow is explicit.
- Failed save attempts do not appear as successful completion states.
