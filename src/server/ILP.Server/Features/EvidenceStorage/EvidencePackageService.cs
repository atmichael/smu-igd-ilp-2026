using System.Collections.Concurrent;
using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

public interface IEvidencePackageStore
{
    EvidencePackage Create(CreateEvidencePackageRequest request);
    EvidencePackage? GetById(string evidencePackageId);
    IReadOnlyList<EvidencePackage> Query(string? caseId = null, string? documentId = null, string? sourceReference = null, string? reviewStatus = null);
    EvidencePackage Finalize(string evidencePackageId);
}

public sealed class EvidencePackageStore : IEvidencePackageStore
{
    private static readonly ConcurrentDictionary<string, EvidencePackage> Packages = new(StringComparer.OrdinalIgnoreCase);

    public EvidencePackage Create(CreateEvidencePackageRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.CaseId))
        {
            throw new InvalidOperationException("A valid caseId is required.");
        }

        if (request.Documents is null || request.Documents.Count == 0)
        {
            throw new InvalidOperationException("At least one document is required.");
        }

        var normalizedStatus = NormalizeStatus(request.ReviewStatus);
        if (normalizedStatus == "confirmed")
        {
            var duplicate = Packages.Values.Any(package =>
                string.Equals(package.CaseId, request.CaseId, StringComparison.OrdinalIgnoreCase)
                && package.Status == "confirmed"
                && request.Documents.Any(document =>
                    package.Documents.Any(existingDocument =>
                        string.Equals(existingDocument.SourceReference, document.SourceReference, StringComparison.OrdinalIgnoreCase))));

            if (duplicate)
            {
                throw new DuplicateEvidenceException("Duplicate final evidence for the same source and case.");
            }
        }

        var package = new EvidencePackage
        {
            EvidencePackageId = Guid.NewGuid().ToString(),
            CaseId = request.CaseId,
            SourceType = request.SourceType,
            Status = normalizedStatus,
            RetentionPolicy = "pilot-3-year",
            Documents = new List<SourceDocument>(),
            AuditEvents = new List<AuditEvent>()
        };

        foreach (var documentRequest in request.Documents)
        {
            var document = new SourceDocument
            {
                EvidencePackageId = package.EvidencePackageId,
                DocumentType = documentRequest.DocumentType,
                SourceReference = documentRequest.SourceReference,
                ReviewStatus = NormalizeStatus(documentRequest.ReviewStatus),
                StorageLocation = documentRequest.StorageLocation,
                Checksum = documentRequest.Checksum,
                ContentHash = string.IsNullOrWhiteSpace(documentRequest.Checksum) ? Guid.NewGuid().ToString() : documentRequest.Checksum,
                CreatedAt = DateTimeOffset.UtcNow,
                Records = new List<StructuredRecord>()
            };

            foreach (var recordRequest in documentRequest.Records ?? new List<StructuredRecordRequest>())
            {
                var record = new StructuredRecord
                {
                    SourceDocumentId = document.SourceDocumentId,
                    RecordCategory = recordRequest.RecordCategory,
                    RecordType = recordRequest.RecordType,
                    RawValue = recordRequest.RawValue,
                    CurrentValue = recordRequest.CurrentValue,
                    ReviewStatus = NormalizeStatus(recordRequest.ReviewStatus),
                    SourceReference = recordRequest.SourceReference,
                    ModelVersion = recordRequest.ModelVersion,
                    SchemaVersion = recordRequest.SchemaVersion,
                    VerificationStatus = recordRequest.VerificationStatus,
                    MatchedEvidenceId = recordRequest.MatchedEvidenceId,
                    Provenance = new List<ProvenanceRecord>()
                };

                foreach (var provenanceRequest in recordRequest.Provenance ?? new List<ProvenanceRecordRequest>())
                {
                    record.Provenance.Add(new ProvenanceRecord
                    {
                        RecordId = record.RecordId,
                        EvidencePackageId = package.EvidencePackageId,
                        EventType = provenanceRequest.EventType,
                        ActorType = provenanceRequest.ActorType,
                        ActorId = provenanceRequest.ActorId,
                        PreviousValue = provenanceRequest.PreviousValue,
                        NewValue = provenanceRequest.NewValue,
                        SourceReference = provenanceRequest.SourceReference,
                        Timestamp = provenanceRequest.Timestamp ?? DateTimeOffset.UtcNow,
                        Reason = provenanceRequest.Reason
                    });
                }

                if (record.Provenance.Count > 0)
                {
                    record.ProvenanceVersion = record.Provenance.Count;
                }

                document.Records.Add(record);
            }

            package.Documents.Add(document);
        }

        foreach (var auditEventRequest in request.AuditEvents ?? new List<AuditEventRequest>())
        {
            package.AuditEvents.Add(new AuditEvent
            {
                EvidencePackageId = package.EvidencePackageId,
                EventType = auditEventRequest.EventType,
                ActorType = auditEventRequest.ActorType,
                ActorId = auditEventRequest.ActorId,
                Message = auditEventRequest.Message,
                Metadata = auditEventRequest.Metadata,
                Timestamp = auditEventRequest.Timestamp ?? DateTimeOffset.UtcNow
            });
        }

        Packages[package.EvidencePackageId] = package;
        return package;
    }

    public EvidencePackage? GetById(string evidencePackageId) =>
        Packages.TryGetValue(evidencePackageId, out var package) ? package : null;

    public IReadOnlyList<EvidencePackage> Query(string? caseId = null, string? documentId = null, string? sourceReference = null, string? reviewStatus = null)
    {
        var matches = Packages.Values.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            matches = matches.Where(x => string.Equals(x.CaseId, caseId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(documentId))
        {
            matches = matches.Where(x => x.Documents.Any(document => string.Equals(document.SourceDocumentId, documentId, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(sourceReference))
        {
            matches = matches.Where(x => x.Documents.Any(document => string.Equals(document.SourceReference, sourceReference, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(reviewStatus))
        {
            var normalized = NormalizeStatus(reviewStatus);
            matches = matches.Where(x => string.Equals(x.Status, normalized, StringComparison.OrdinalIgnoreCase)
                || x.Documents.Any(document => string.Equals(document.ReviewStatus, normalized, StringComparison.OrdinalIgnoreCase))
                || x.Documents.Any(document => document.Records.Any(record => string.Equals(record.ReviewStatus, normalized, StringComparison.OrdinalIgnoreCase))));
        }

        return matches.ToList();
    }

    public EvidencePackage Finalize(string evidencePackageId)
    {
        var package = GetById(evidencePackageId) ?? throw new KeyNotFoundException($"Evidence package {evidencePackageId} was not found.");

        package.Status = "confirmed";
        package.UpdatedAt = DateTimeOffset.UtcNow;

        foreach (var document in package.Documents)
        {
            document.ReviewStatus = "confirmed";
            foreach (var record in document.Records)
            {
                record.ReviewStatus = "confirmed";
                record.Provenance.Add(new ProvenanceRecord
                {
                    RecordId = record.RecordId,
                    EvidencePackageId = package.EvidencePackageId,
                    EventType = "finalized",
                    ActorType = "system",
                    SourceReference = document.SourceReference,
                    Timestamp = DateTimeOffset.UtcNow,
                    Reason = "Package finalized"
                });
            }
        }

        package.AuditEvents.Add(new AuditEvent
        {
            EvidencePackageId = package.EvidencePackageId,
            EventType = "finalized",
            ActorType = "system",
            Message = "Evidence package finalized",
            Metadata = new { finalizedAt = DateTimeOffset.UtcNow },
            Timestamp = DateTimeOffset.UtcNow
        });

        return package;
    }

    private static string NormalizeStatus(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return "draft";
        }

        return value.Trim().ToLowerInvariant() switch
        {
            "draft" => "draft",
            "pending-review" or "pending_review" => "pending-review",
            "reviewed" => "reviewed",
            "confirmed" => "confirmed",
            "rejected" => "rejected",
            "superseded" => "superseded",
            "archived" => "archived",
            _ => "draft"
        };
    }
}

public sealed class DuplicateEvidenceException : Exception
{
    public DuplicateEvidenceException(string message) : base(message)
    {
    }
}
