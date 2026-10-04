# Quickstart: Document Evidence Storage Validation

This guide validates the persistence, retrieval, provenance, audit, and status-handling behavior for the document evidence package feature.

## Prerequisites

- .NET 10 SDK installed.
- A server project path for the evidence-storage API and a durable backend store configured for the development environment.
- Access to the existing .NET test project at `tests/ILP.Server.Tests`.
- Optional: a test environment that can exercise a real invoice, purchase order, and receipt evidence set without exposing credentials or raw invoice contents in logs.

## Start the API

From the repository root:

```powershell
dotnet run --project .\src\server\ILP.Server\ILP.Server.csproj
```

The implementation should expose an evidence package API that uses the shared contract layer and does not call ingestion, extraction, or matching services.

## Automated checks

From the repository root:

```powershell
dotnet test .\tests\ILP.Server.Tests\ILP.Server.Tests.csproj
```

For the implementation phase, add contract tests that validate:

- save or create evidence package behavior
- retrieval by package ID, case ID, document ID, or review status
- deduplication of final evidence for the same source and case
- explicit draft/final status transitions
- provenance preservation across extraction and correction events
- failed save operations remain failed/pending instead of success

## Manual validation scenarios

1. Save a final evidence package containing an invoice, purchase order, and receipt for the same case.
   - Expected result: the package is persisted with source documents and structured records linked by package ID and case ID.
2. Retrieve the package by case ID and then by invoice or document ID.
   - Expected result: the same package and linked records are returned without losing the original source document metadata.
3. Create a record where an extracted value is later corrected by a user or reviewer.
   - Expected result: the raw value and approved value are both retained, and the provenance timeline shows the change and timestamp.
4. Save a draft or pending package and finalize it.
   - Expected result: the draft remains separate and the final package is only created after explicit finalization.
5. Submit the same source document twice for the same case without explicit replacement.
   - Expected result: the system deduplicates the final evidence and prevents a duplicate final record.
6. Trigger a storage failure after evidence is assembled but before final commit.
   - Expected result: the package remains failed or pending and is never shown as successful.
7. Record a match outcome against the evidence package.
   - Expected result: the match outcome remains linked to the evidence package and supporting records used in the decision.
8. Validate retention metadata and archive lifecycle on a finalized evidence package.
   - Expected result: the system records the 3-year pilot retention default and is ready for the production policy review before go-live.

## Expected outcomes

- Finalized evidence packages are stored with provenance, review status, and audit history.
- Retrieval is available by case, source document, record, or package identifier.
- Draft and final evidence remain separate lifecycle states.
- Duplicate final records are prevented unless a legitimate replacement workflow is explicit.
- Failed save attempts do not appear as successful completion states.
