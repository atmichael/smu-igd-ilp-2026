using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Applies append-only value and status changes to records; prior values remain in the record's provenance.</summary>
public sealed class RecordChangeService
{
    private static readonly ReviewStatus[] EditableStatuses = [ReviewStatus.Draft, ReviewStatus.PendingReview, ReviewStatus.Reviewed];
    private static readonly ReviewStatus[] ManualStatusTargets = [ReviewStatus.Draft, ReviewStatus.PendingReview, ReviewStatus.Reviewed, ReviewStatus.Rejected];
    private static readonly ActorType[] CorrectionActors = [ActorType.User, ActorType.Reviewer];

    private readonly AuditTrailService _audit;
    private readonly TimeProvider _timeProvider;

    public RecordChangeService(AuditTrailService audit, TimeProvider timeProvider)
    {
        _audit = audit;
        _timeProvider = timeProvider;
    }

    public StructuredRecord ApplyCorrection(EvidencePackage package, string recordId, RecordCorrectionRequest request, string? actorId)
    {
        EnsureEditable(package, "corrected");
        EvidencePackageValidator.Require("newValue", request.NewValue, "A corrected value is required.");
        EvidencePackageValidator.Require("reason", request.Reason, "A correction reason is required.");
        var actorType = string.IsNullOrWhiteSpace(request.ActorType)
            ? ActorType.User
            : EvidencePackageValidator.ParseRequired<ActorType>("actorType", request.ActorType);
        if (!CorrectionActors.Contains(actorType))
        {
            throw new EvidenceValidationException(new Dictionary<string, string[]> { ["actorType"] = ["Corrections must be made by a user or reviewer."] });
        }

        var (document, record) = FindRecord(package, recordId);
        var now = _timeProvider.GetUtcNow();
        record.Provenance.Add(new ProvenanceEntry
        {
            RecordId = record.RecordId,
            EvidencePackageId = package.EvidencePackageId,
            EventType = ProvenanceEventType.Corrected,
            ActorType = actorType,
            ActorId = actorId,
            PreviousValue = record.CurrentValue,
            NewValue = request.NewValue,
            SourceReference = document.SourceReference,
            Timestamp = now,
            Reason = request.Reason
        });
        record.CurrentValue = request.NewValue;
        record.ValueVersion++;
        package.UpdatedAt = now;

        _audit.Append(package, AuditEventType.UserCorrection, actorType, actorId, "Record value corrected; prior value retained in provenance.", record.RecordId,
            new Dictionary<string, string> { ["valueVersion"] = record.ValueVersion.ToString() });
        return record;
    }

    public StructuredRecord ApplyVerification(EvidencePackage package, string recordId, RecordVerificationRequest request, string? actorId)
    {
        if (package.ReviewStatus is ReviewStatus.Superseded or ReviewStatus.Archived or ReviewStatus.Failed)
        {
            throw new EvidencePreconditionException($"Verification cannot be recorded on '{WireEnum.ToWire(package.ReviewStatus)}' evidence.");
        }

        var verification = EvidencePackageValidator.ParseRequired<VerificationStatus>("verificationStatus", request.VerificationStatus);
        var (document, record) = FindRecord(package, recordId);
        var now = _timeProvider.GetUtcNow();
        var previous = record.VerificationStatus;

        record.Provenance.Add(new ProvenanceEntry
        {
            RecordId = record.RecordId,
            EvidencePackageId = package.EvidencePackageId,
            EventType = ProvenanceEventType.Verified,
            ActorType = ActorType.System,
            ActorId = actorId,
            PreviousValue = previous is null ? null : WireEnum.ToWire(previous.Value),
            NewValue = WireEnum.ToWire(verification),
            SourceReference = document.SourceReference,
            Timestamp = now,
            Reason = request.Reason
        });
        record.VerificationStatus = verification;
        package.UpdatedAt = now;

        var metadata = new Dictionary<string, string> { ["verificationStatus"] = WireEnum.ToWire(verification) };
        if (!string.IsNullOrWhiteSpace(request.ModelVersion))
        {
            metadata["modelVersion"] = request.ModelVersion;
        }

        if (!string.IsNullOrWhiteSpace(request.SchemaVersion))
        {
            metadata["schemaVersion"] = request.SchemaVersion;
        }

        _audit.Append(package, AuditEventType.VerificationResult, ActorType.System, actorId, "Verification result recorded without altering the evidence value.", record.RecordId, metadata);
        return record;
    }

    public void ApplyStatusChange(EvidencePackage package, StatusChangeRequest request, string? actorId)
    {
        EnsureEditable(package, "re-statused");
        var target = EvidencePackageValidator.ParseRequired<ReviewStatus>("reviewStatus", request.ReviewStatus);
        if (!ManualStatusTargets.Contains(target))
        {
            throw new EvidenceValidationException(new Dictionary<string, string[]>
            {
                ["reviewStatus"] = [$"Manual status changes allow {string.Join(", ", ManualStatusTargets.Select(WireEnum.ToWire))}; use finalize, replacement, or archive for other states."]
            });
        }

        string previous;
        string? recordId = null;
        if (string.IsNullOrWhiteSpace(request.RecordId))
        {
            previous = WireEnum.ToWire(package.ReviewStatus);
            package.ReviewStatus = target;
        }
        else
        {
            var (_, record) = FindRecord(package, request.RecordId);
            previous = WireEnum.ToWire(record.ReviewStatus);
            record.ReviewStatus = target;
            recordId = record.RecordId;
        }

        package.UpdatedAt = _timeProvider.GetUtcNow();
        var eventType = target == ReviewStatus.Rejected ? AuditEventType.Rejected : AuditEventType.StatusChanged;
        var metadata = new Dictionary<string, string> { ["previousStatus"] = previous, ["newStatus"] = WireEnum.ToWire(target) };
        if (!string.IsNullOrWhiteSpace(request.Reason))
        {
            metadata["reason"] = SensitiveDataGuard.SanitizeText(request.Reason, SensitiveDataGuard.ProtectedValues(package));
        }

        _audit.Append(package, eventType, ActorType.Reviewer, actorId, "Review status changed.", recordId, metadata);
    }

    private static void EnsureEditable(EvidencePackage package, string action)
    {
        if (!EditableStatuses.Contains(package.ReviewStatus))
        {
            throw new EvidencePreconditionException(
                $"'{WireEnum.ToWire(package.ReviewStatus)}' evidence cannot be {action}; submit a replacement package to change final evidence.");
        }
    }

    private static (EvidenceDocument Document, StructuredRecord Record) FindRecord(EvidencePackage package, string recordId)
    {
        foreach (var document in package.Documents)
        {
            var record = document.Records.FirstOrDefault(candidate => string.Equals(candidate.RecordId, recordId, StringComparison.OrdinalIgnoreCase));
            if (record is not null)
            {
                return (document, record);
            }
        }

        throw new EvidenceNotFoundException($"Record {recordId} was not found in evidence package {package.EvidencePackageId}.");
    }
}
