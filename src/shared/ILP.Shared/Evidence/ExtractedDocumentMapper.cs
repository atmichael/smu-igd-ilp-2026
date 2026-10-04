using ILP.Shared.InfoExtraction;

namespace ILP.Shared.Evidence;

/// <summary>Turns the shared extraction output (DocumentHeaderFields keys) into an evidence document request.</summary>
public static class ExtractedDocumentMapper
{
    /// <summary>Maps the prompt's document-type labels; sales orders and statements are kept but are never match inputs.</summary>
    public static DocumentType ToDocumentType(string? extractedType) =>
        extractedType?.Trim().ToLowerInvariant() switch
        {
            "invoice" => DocumentType.Invoice,
            "purchase order" or "service order" => DocumentType.PurchaseOrder,
            "delivery order" => DocumentType.Receipt,
            _ => DocumentType.OtherEvidence
        };

    public static EvidenceDocumentRequest ToEvidenceDocument(
        IReadOnlyDictionary<string, string?> headerFields,
        string storageLocation,
        string checksum,
        string? sourceDocumentId = null,
        string? modelVersion = null,
        string? schemaVersion = null)
    {
        headerFields.TryGetValue(DocumentHeaderFields.DocumentNumber, out var documentNumber);
        headerFields.TryGetValue(DocumentHeaderFields.DocumentType, out var documentType);

        return new EvidenceDocumentRequest
        {
            SourceDocumentId = sourceDocumentId,
            DocumentType = WireEnum.ToWire(ToDocumentType(documentType)),
            SourceReference = documentNumber ?? string.Empty,
            StorageLocation = storageLocation,
            Checksum = checksum,
            Records = headerFields
                .Where(field => field.Value is not null)
                .Select(field => new StructuredRecordRequest
                {
                    RecordCategory = WireEnum.ToWire(RecordCategory.DocumentHeader),
                    RecordType = field.Key,
                    RawValue = field.Value,
                    CurrentValue = field.Value,
                    ModelVersion = modelVersion,
                    SchemaVersion = schemaVersion,
                    Provenance =
                    [
                        new ProvenanceEntryRequest
                        {
                            EventType = WireEnum.ToWire(ProvenanceEventType.Extracted),
                            ActorType = WireEnum.ToWire(ActorType.Model),
                            NewValue = field.Value
                        }
                    ]
                })
                .ToList()
        };
    }
}
