using ILP.Shared.SourceDocuments;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Repository envelope for source-document metadata and intake idempotency data.</summary>
public sealed record SourceDocumentPersistenceRecord(SourceDocumentMetadata Document, string IdempotencyKey, string PayloadHash);
