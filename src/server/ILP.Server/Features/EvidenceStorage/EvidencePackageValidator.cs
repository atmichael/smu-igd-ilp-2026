using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

public sealed record ValidatedAuditEvent(
    AuditEventType EventType,
    ActorType ActorType,
    string? ActorId,
    string Message,
    IReadOnlyDictionary<string, string>? Metadata,
    DateTimeOffset? Timestamp);

public sealed record ValidatedCreate(EvidencePackage Package, IReadOnlyList<ValidatedAuditEvent> AuditEvents);

/// <summary>Validates inbound evidence requests against the data-model enums and lifecycle rules.</summary>
public static class EvidencePackageValidator
{
    private static readonly ReviewStatus[] CreatableStatuses = [ReviewStatus.Draft, ReviewStatus.PendingReview];

    private static readonly ProvenanceEventType[] ClientProvenanceEvents =
        [ProvenanceEventType.Extracted, ProvenanceEventType.Corrected, ProvenanceEventType.Verified, ProvenanceEventType.Rejected];

    private static readonly AuditEventType[] ClientAuditEvents =
    [
        AuditEventType.SourceAttached,
        AuditEventType.ModelVersioned,
        AuditEventType.SchemaVersioned,
        AuditEventType.VerificationResult,
        AuditEventType.UserCorrection
    ];

    private static readonly Dictionary<DocumentType, RecordCategory[]> AllowedCategories = new()
    {
        [DocumentType.Invoice] = [RecordCategory.InvoiceLine],
        [DocumentType.PurchaseOrder] = [RecordCategory.PurchaseOrderCommitment],
        [DocumentType.Receipt] = [RecordCategory.GoodsReceipt, RecordCategory.ServiceAcceptance],
        [DocumentType.OtherEvidence] = Enum.GetValues<RecordCategory>()
    };

    public static ValidatedCreate ValidateCreate(CreateEvidencePackageRequest request, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(request.CaseId) || request.Documents is null || request.Documents.Count == 0)
        {
            var missing = new Dictionary<string, string[]>();
            if (string.IsNullOrWhiteSpace(request.CaseId))
            {
                missing["caseId"] = ["A valid caseId is required."];
            }

            if (request.Documents is null || request.Documents.Count == 0)
            {
                missing["documents"] = ["At least one evidence document is required."];
            }

            throw new EvidenceValidationException(missing, StatusCodes.Status400BadRequest);
        }

        var errors = new ErrorCollector();
        var package = new EvidencePackage
        {
            CaseId = request.CaseId.Trim(),
            ReviewStatus = errors.ParseStatus("reviewStatus", request.ReviewStatus, CreatableStatuses),
            CreatedAt = now,
            UpdatedAt = now
        };

        if (!string.IsNullOrWhiteSpace(request.ReplacesEvidencePackageId))
        {
            package.ReplacesEvidencePackageId = request.ReplacesEvidencePackageId.Trim();
            package.ReplacementReason = request.ReplacementReason?.Trim();
            if (string.IsNullOrWhiteSpace(package.ReplacementReason))
            {
                errors.Add("replacementReason", "A replacement or reprocessing reason is required when replacing evidence.");
            }
        }

        for (var d = 0; d < request.Documents.Count; d++)
        {
            package.Documents.Add(BuildDocument(request.Documents[d], $"documents[{d}]", package.EvidencePackageId, now, errors));
        }

        package.PackageType = PackageTypeOf(package.Documents);

        var auditEvents = new List<ValidatedAuditEvent>();
        for (var a = 0; a < (request.AuditEvents?.Count ?? 0); a++)
        {
            var path = $"auditEvents[{a}]";
            var source = request.AuditEvents![a];
            var eventType = errors.ParseRequired<AuditEventType>($"{path}.eventType", source.EventType);
            if (!ClientAuditEvents.Contains(eventType))
            {
                errors.Add($"{path}.eventType", $"Allowed client audit events: {string.Join(", ", ClientAuditEvents.Select(WireEnum.ToWire))}.");
            }

            auditEvents.Add(new ValidatedAuditEvent(
                eventType,
                errors.ParseRequired<ActorType>($"{path}.actorType", source.ActorType),
                source.ActorId,
                source.Message,
                source.Metadata,
                source.Timestamp));
        }

        errors.ThrowIfAny();
        return new ValidatedCreate(package, auditEvents);
    }

    public static TEnum ParseRequired<TEnum>(string field, string? value) where TEnum : struct, Enum
    {
        var errors = new ErrorCollector();
        var result = errors.ParseRequired<TEnum>(field, value);
        errors.ThrowIfAny();
        return result;
    }

    public static void Require(string field, string? value, string message)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new EvidenceValidationException(new Dictionary<string, string[]> { [field] = [message] });
        }
    }

    private static EvidencePackageType PackageTypeOf(IReadOnlyCollection<EvidenceDocument> documents)
    {
        var types = documents.Select(document => document.DocumentType).Distinct().ToList();
        if (types.Count != 1)
        {
            return EvidencePackageType.Mixed;
        }

        return types[0] switch
        {
            DocumentType.Invoice => EvidencePackageType.Invoice,
            DocumentType.PurchaseOrder => EvidencePackageType.PurchaseOrder,
            DocumentType.Receipt => EvidencePackageType.Receipt,
            _ => EvidencePackageType.Mixed
        };
    }

    private static EvidenceDocument BuildDocument(EvidenceDocumentRequest source, string path, string packageId, DateTimeOffset now, ErrorCollector errors)
    {
        var documentType = errors.ParseRequired<DocumentType>($"{path}.documentType", source.DocumentType);
        errors.RequireText($"{path}.sourceReference", source.SourceReference);
        errors.RequireText($"{path}.storageLocation", source.StorageLocation);

        var document = new EvidenceDocument
        {
            EvidencePackageId = packageId,
            SourceDocumentId = string.IsNullOrWhiteSpace(source.SourceDocumentId) ? null : source.SourceDocumentId.Trim(),
            DocumentType = documentType,
            SourceReference = source.SourceReference?.Trim() ?? string.Empty,
            StorageLocation = source.StorageLocation?.Trim(),
            Checksum = source.Checksum ?? string.Empty,
            ReviewStatus = errors.ParseStatus($"{path}.reviewStatus", source.ReviewStatus, CreatableStatuses),
            CreatedAt = now
        };

        for (var r = 0; r < (source.Records?.Count ?? 0); r++)
        {
            document.Records.Add(BuildRecord(source.Records![r], $"{path}.records[{r}]", errors.IsValid($"{path}.documentType"), document, packageId, now, errors));
        }

        return document;
    }

    private static StructuredRecord BuildRecord(StructuredRecordRequest source, string path, bool documentTypeValid, EvidenceDocument document, string packageId, DateTimeOffset now, ErrorCollector errors)
    {
        var category = errors.ParseRequired<RecordCategory>($"{path}.recordCategory", source.RecordCategory);
        if (documentTypeValid && errors.IsValid($"{path}.recordCategory") && !AllowedCategories[document.DocumentType].Contains(category))
        {
            errors.Add($"{path}.recordCategory",
                $"Category '{WireEnum.ToWire(category)}' cannot be stored on a '{WireEnum.ToWire(document.DocumentType)}' document.");
        }

        errors.RequireText($"{path}.recordType", source.RecordType);

        var record = new StructuredRecord
        {
            DocumentId = document.DocumentId,
            RecordCategory = category,
            RecordType = source.RecordType?.Trim() ?? string.Empty,
            RawValue = source.RawValue,
            CurrentValue = source.CurrentValue ?? source.RawValue,
            ReviewStatus = errors.ParseStatus($"{path}.reviewStatus", source.ReviewStatus, CreatableStatuses),
            SourceReference = string.IsNullOrWhiteSpace(source.SourceReference) ? document.SourceReference : source.SourceReference.Trim(),
            ModelVersion = source.ModelVersion,
            SchemaVersion = source.SchemaVersion,
            VerificationStatus = string.IsNullOrWhiteSpace(source.VerificationStatus)
                ? null
                : errors.ParseRequired<VerificationStatus>($"{path}.verificationStatus", source.VerificationStatus),
            MatchedRecordId = source.MatchedRecordId
        };

        for (var p = 0; p < (source.Provenance?.Count ?? 0); p++)
        {
            var entryPath = $"{path}.provenance[{p}]";
            var entry = source.Provenance![p];
            var eventType = errors.ParseRequired<ProvenanceEventType>($"{entryPath}.eventType", entry.EventType);
            if (errors.IsValid($"{entryPath}.eventType") && !ClientProvenanceEvents.Contains(eventType))
            {
                errors.Add($"{entryPath}.eventType", $"'{WireEnum.ToWire(eventType)}' provenance entries are recorded by the server only.");
            }

            record.Provenance.Add(new ProvenanceEntry
            {
                RecordId = record.RecordId,
                EvidencePackageId = packageId,
                EventType = eventType,
                ActorType = errors.ParseRequired<ActorType>($"{entryPath}.actorType", entry.ActorType),
                ActorId = entry.ActorId,
                PreviousValue = entry.PreviousValue,
                NewValue = entry.NewValue,
                SourceReference = string.IsNullOrWhiteSpace(entry.SourceReference) ? record.SourceReference : entry.SourceReference.Trim(),
                Timestamp = entry.Timestamp ?? now,
                Reason = entry.Reason
            });
        }

        record.ValueVersion = Math.Max(1, record.Provenance.Count(entry =>
            entry.EventType is ProvenanceEventType.Extracted or ProvenanceEventType.Corrected));

        return record;
    }

    private sealed class ErrorCollector
    {
        private readonly Dictionary<string, List<string>> _errors = new();

        public void Add(string field, string message)
        {
            if (!_errors.TryGetValue(field, out var messages))
            {
                _errors[field] = messages = new List<string>();
            }

            messages.Add(message);
        }

        public bool IsValid(string field) => !_errors.ContainsKey(field);

        public void RequireText(string field, string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                Add(field, "A value is required.");
            }
        }

        public TEnum ParseRequired<TEnum>(string field, string? value) where TEnum : struct, Enum
        {
            if (WireEnum.TryParse<TEnum>(value, out var result))
            {
                return result;
            }

            Add(field, $"Expected one of: {string.Join(", ", WireEnum.WireNames<TEnum>())}.");
            return default;
        }

        public TEnum ParseOrDefault<TEnum>(string field, string? value, TEnum fallback) where TEnum : struct, Enum =>
            string.IsNullOrWhiteSpace(value) ? fallback : ParseRequired<TEnum>(field, value);

        public ReviewStatus ParseStatus(string field, string? value, ReviewStatus[] allowed)
        {
            var status = ParseOrDefault(field, value, ReviewStatus.Draft);
            if (IsValid(field) && !allowed.Contains(status))
            {
                Add(field, $"New evidence must be {string.Join(" or ", allowed.Select(WireEnum.ToWire))}; confirmed evidence requires POST /api/evidence-packages/{{id}}/finalize.");
            }

            return status;
        }

        public void ThrowIfAny()
        {
            if (_errors.Count > 0)
            {
                throw new EvidenceValidationException(_errors.ToDictionary(entry => entry.Key, entry => entry.Value.ToArray()));
            }
        }
    }
}
