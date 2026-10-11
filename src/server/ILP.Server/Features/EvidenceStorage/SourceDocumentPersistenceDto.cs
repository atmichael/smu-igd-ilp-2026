using ILP.Shared.SourceDocuments.Dto;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Repository envelope for source-document metadata and intake idempotency data.</summary>
public sealed record SourceDocumentPersistenceDto(SourceDocumentMetadataDto Document, string IdempotencyKey, string PayloadHash);
