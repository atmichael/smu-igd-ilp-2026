namespace ILP.Shared.Evidence;

public sealed class EvidencePackage
{
    public string EvidencePackageId { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public EvidencePackageType PackageType { get; set; } = EvidencePackageType.Mixed;
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Draft;
    public int Version { get; set; } = 1;
    public string? ReplacesEvidencePackageId { get; set; }
    public string? ReplacementReason { get; set; }
    public string? SupersededByEvidencePackageId { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
    public DateTimeOffset? FinalizedAt { get; set; }
    public string RetentionPolicy { get; set; } = "pilot-3-year";
    public DateTimeOffset? RetentionUntil { get; set; }
    public DateTimeOffset? ArchivedAt { get; set; }
    public string? RelatedMatchReviewId { get; set; }
    public List<EvidenceDocument> Documents { get; set; } = new();
    public List<MatchOutcome> MatchOutcomes { get; set; } = new();
    public List<AuditEvent> AuditEvents { get; set; } = new();

    /// <summary>True when retention rules restrict access to document content and values.</summary>
    public bool ContentRestricted { get; set; }
}

public sealed class EvidenceDocument
{
    public string DocumentId { get; set; } = Guid.NewGuid().ToString();
    public string EvidencePackageId { get; set; } = string.Empty;
    public string? SourceDocumentId { get; set; }
    public DocumentType DocumentType { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string? StorageLocation { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Draft;
    public DateTimeOffset CreatedAt { get; set; }
    public List<StructuredRecord> Records { get; set; } = new();
}

public sealed class StructuredRecord
{
    public string RecordId { get; set; } = Guid.NewGuid().ToString();
    public string DocumentId { get; set; } = string.Empty;
    public RecordCategory RecordCategory { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? CurrentValue { get; set; }
    public ReviewStatus ReviewStatus { get; set; } = ReviewStatus.Draft;
    public int ValueVersion { get; set; } = 1;
    public string SourceReference { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string? SchemaVersion { get; set; }
    public VerificationStatus? VerificationStatus { get; set; }
    public string? MatchedRecordId { get; set; }
    public List<ProvenanceEntry> Provenance { get; set; } = new();
}

public sealed class ProvenanceEntry
{
    public string EntryId { get; set; } = Guid.NewGuid().ToString();
    public string RecordId { get; set; } = string.Empty;
    public string EvidencePackageId { get; set; } = string.Empty;
    public ProvenanceEventType EventType { get; set; }
    public ActorType ActorType { get; set; }
    public string? ActorId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }
    public string? Reason { get; set; }
}

public sealed class AuditEvent
{
    public string AuditEventId { get; set; } = Guid.NewGuid().ToString();
    public string EvidencePackageId { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public AuditEventType EventType { get; set; }
    public ActorType ActorType { get; set; }
    public string? ActorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public sealed class MatchOutcome
{
    public string MatchReviewId { get; set; } = string.Empty;
    public MatchOutcomeType Outcome { get; set; }
    public List<string> SupportingRecordIds { get; set; } = new();
    public List<string> MissingRecordIds { get; set; } = new();
    public bool EvidenceComplete { get; set; }
    public string? Reason { get; set; }
    public ActorType ActorType { get; set; }
    public string? ActorId { get; set; }
    public DateTimeOffset RecordedAt { get; set; }
}

// Requests keep enum fields as strings so invalid values surface as 422 validation errors instead of JSON binding failures.

public sealed class CreateEvidencePackageRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string? ReviewStatus { get; set; }
    public string? ReplacesEvidencePackageId { get; set; }
    public string? ReplacementReason { get; set; }
    public List<EvidenceDocumentRequest>? Documents { get; set; }
    public List<AuditEventRequest>? AuditEvents { get; set; }
}

public sealed class EvidenceDocumentRequest
{
    public string? SourceDocumentId { get; set; }
    public string? DocumentType { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string? ReviewStatus { get; set; }
    public string? StorageLocation { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public List<StructuredRecordRequest>? Records { get; set; }
}

public sealed class StructuredRecordRequest
{
    public string? RecordCategory { get; set; }
    public string RecordType { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? CurrentValue { get; set; }
    public string? ReviewStatus { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string? SchemaVersion { get; set; }
    public string? VerificationStatus { get; set; }
    public string? MatchedRecordId { get; set; }
    public List<ProvenanceEntryRequest>? Provenance { get; set; }
}

public sealed class ProvenanceEntryRequest
{
    public string? EventType { get; set; }
    public string? ActorType { get; set; }
    public string? ActorId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public string? Reason { get; set; }
}

public sealed class AuditEventRequest
{
    public string? EventType { get; set; }
    public string? ActorType { get; set; }
    public string? ActorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public Dictionary<string, string>? Metadata { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}

public sealed class RecordCorrectionRequest
{
    public string? NewValue { get; set; }
    public string? Reason { get; set; }
    public string? ActorType { get; set; }
}

public sealed class RecordVerificationRequest
{
    public string? VerificationStatus { get; set; }
    public string? Reason { get; set; }
    public string? ModelVersion { get; set; }
    public string? SchemaVersion { get; set; }
}

public sealed class StatusChangeRequest
{
    public string? ReviewStatus { get; set; }
    public string? RecordId { get; set; }
    public string? Reason { get; set; }
}

public sealed class MatchOutcomeRequest
{
    public string MatchReviewId { get; set; } = string.Empty;
    public string? Outcome { get; set; }
    public List<string>? SupportingRecordIds { get; set; }
    public string? Reason { get; set; }
}
