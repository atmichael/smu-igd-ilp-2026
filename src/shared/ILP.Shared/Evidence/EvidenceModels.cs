namespace ILP.Shared.Evidence;

public enum EvidencePackageSourceType
{
    Invoice,
    PurchaseOrder,
    Receipt,
    Mixed
}

public enum ReviewStatus
{
    Draft,
    PendingReview,
    Reviewed,
    Confirmed,
    Rejected,
    Superseded,
    Archived
}

public sealed class EvidencePackage
{
    public string EvidencePackageId { get; set; } = Guid.NewGuid().ToString();
    public string CaseId { get; set; } = string.Empty;
    public string SourceType { get; set; } = "mixed";
    public string Status { get; set; } = "draft";
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
    public string RetentionPolicy { get; set; } = "pilot-3-year";
    public string? RelatedMatchReviewId { get; set; }
    public List<SourceDocument> Documents { get; set; } = new();
    public List<AuditEvent> AuditEvents { get; set; } = new();
}

public sealed class SourceDocument
{
    public string SourceDocumentId { get; set; } = Guid.NewGuid().ToString();
    public string EvidencePackageId { get; set; } = string.Empty;
    public string DocumentType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "draft";
    public string ContentHash { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
    public List<StructuredRecord> Records { get; set; } = new();
}

public sealed class StructuredRecord
{
    public string RecordId { get; set; } = Guid.NewGuid().ToString();
    public string SourceDocumentId { get; set; } = string.Empty;
    public string RecordCategory { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? CurrentValue { get; set; }
    public string ReviewStatus { get; set; } = "draft";
    public int ProvenanceVersion { get; set; } = 1;
    public string SourceReference { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string? SchemaVersion { get; set; }
    public string? VerificationStatus { get; set; }
    public string? MatchedEvidenceId { get; set; }
    public List<ProvenanceRecord> Provenance { get; set; } = new();
}

public sealed class ProvenanceRecord
{
    public string ProvenanceId { get; set; } = Guid.NewGuid().ToString();
    public string RecordId { get; set; } = string.Empty;
    public string EvidencePackageId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
    public string? Reason { get; set; }
}

public sealed class AuditEvent
{
    public string AuditEventId { get; set; } = Guid.NewGuid().ToString();
    public string EvidencePackageId { get; set; } = string.Empty;
    public string? RecordId { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Metadata { get; set; }
    public DateTimeOffset Timestamp { get; set; } = DateTimeOffset.UtcNow;
}

public sealed class CreateEvidencePackageRequest
{
    public string CaseId { get; set; } = string.Empty;
    public string SourceType { get; set; } = "mixed";
    public string ReviewStatus { get; set; } = "draft";
    public List<SourceDocumentRequest>? Documents { get; set; }
    public List<AuditEventRequest>? AuditEvents { get; set; }
}

public sealed class SourceDocumentRequest
{
    public string DocumentType { get; set; } = string.Empty;
    public string SourceReference { get; set; } = string.Empty;
    public string ReviewStatus { get; set; } = "draft";
    public string StorageLocation { get; set; } = string.Empty;
    public string Checksum { get; set; } = string.Empty;
    public List<StructuredRecordRequest>? Records { get; set; }
}

public sealed class StructuredRecordRequest
{
    public string RecordCategory { get; set; } = string.Empty;
    public string RecordType { get; set; } = string.Empty;
    public string? RawValue { get; set; }
    public string? CurrentValue { get; set; }
    public string ReviewStatus { get; set; } = "draft";
    public string SourceReference { get; set; } = string.Empty;
    public string? ModelVersion { get; set; }
    public string? SchemaVersion { get; set; }
    public string? VerificationStatus { get; set; }
    public string? MatchedEvidenceId { get; set; }
    public List<ProvenanceRecordRequest>? Provenance { get; set; }
}

public sealed class ProvenanceRecordRequest
{
    public string EventType { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string? PreviousValue { get; set; }
    public string? NewValue { get; set; }
    public string SourceReference { get; set; } = string.Empty;
    public DateTimeOffset? Timestamp { get; set; }
    public string? Reason { get; set; }
}

public sealed class AuditEventRequest
{
    public string EventType { get; set; } = string.Empty;
    public string ActorType { get; set; } = string.Empty;
    public string? ActorId { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Metadata { get; set; }
    public DateTimeOffset? Timestamp { get; set; }
}
