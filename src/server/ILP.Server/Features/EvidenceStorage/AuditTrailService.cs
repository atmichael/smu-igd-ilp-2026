using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Appends server-generated audit events; events are never edited or removed once added.</summary>
public sealed class AuditTrailService
{
    private readonly TimeProvider _timeProvider;

    public AuditTrailService(TimeProvider timeProvider)
    {
        _timeProvider = timeProvider;
    }

    public AuditEvent Append(
        EvidencePackage package,
        AuditEventType eventType,
        ActorType actorType,
        string? actorId,
        string message,
        string? recordId = null,
        Dictionary<string, string>? metadata = null)
    {
        var auditEvent = new AuditEvent
        {
            EvidencePackageId = package.EvidencePackageId,
            RecordId = recordId,
            EventType = eventType,
            ActorType = actorType,
            ActorId = actorId,
            Message = message,
            Metadata = metadata,
            Timestamp = _timeProvider.GetUtcNow()
        };

        package.AuditEvents.Add(auditEvent);
        return auditEvent;
    }

    /// <summary>Records source attachment and model/schema versions for a newly created package.</summary>
    public void AppendCreationEvents(EvidencePackage package, ActorType actorType, string? actorId)
    {
        foreach (var document in package.Documents)
        {
            var metadata = new Dictionary<string, string>
            {
                ["documentId"] = document.DocumentId,
                ["documentType"] = WireEnum.ToWire(document.DocumentType),
                ["sourceReference"] = document.SourceReference
            };

            if (document.SourceDocumentId is not null)
            {
                metadata["sourceDocumentId"] = document.SourceDocumentId;
            }

            Append(package, AuditEventType.SourceAttached, actorType, actorId, "Evidence document attached to evidence package.", metadata: metadata);

            foreach (var record in document.Records)
            {
                if (!string.IsNullOrWhiteSpace(record.ModelVersion))
                {
                    Append(package, AuditEventType.ModelVersioned, actorType, actorId, "Model version recorded for structured record.", record.RecordId,
                        new Dictionary<string, string> { ["modelVersion"] = record.ModelVersion });
                }

                if (!string.IsNullOrWhiteSpace(record.SchemaVersion))
                {
                    Append(package, AuditEventType.SchemaVersioned, actorType, actorId, "Schema version recorded for structured record.", record.RecordId,
                        new Dictionary<string, string> { ["schemaVersion"] = record.SchemaVersion });
                }

                if (record.VerificationStatus is { } verification)
                {
                    Append(package, AuditEventType.VerificationResult, actorType, actorId, "Verification result recorded for structured record.", record.RecordId,
                        new Dictionary<string, string> { ["verificationStatus"] = WireEnum.ToWire(verification) });
                }

                if (record.Provenance.Any(entry => entry.EventType == ProvenanceEventType.Corrected))
                {
                    Append(package, AuditEventType.UserCorrection, actorType, actorId, "User correction captured with structured record.", record.RecordId,
                        new Dictionary<string, string> { ["valueVersion"] = record.ValueVersion.ToString() });
                }
            }
        }
    }

    /// <summary>Stores client-supplied audit narrative after stripping credentials and raw invoice values.</summary>
    public void AppendClientEvents(EvidencePackage package, IEnumerable<ValidatedAuditEvent> events, string? submittedBy)
    {
        var protectedValues = SensitiveDataGuard.ProtectedValues(package);
        foreach (var clientEvent in events)
        {
            var metadata = SensitiveDataGuard.SanitizeMetadata(clientEvent.Metadata, protectedValues);
            if (!string.IsNullOrWhiteSpace(clientEvent.ActorId) && clientEvent.ActorId != submittedBy)
            {
                // The audit actor is always the authenticated caller; a different client-claimed actor is kept for context only.
                metadata ??= new Dictionary<string, string>();
                metadata["claimedActorId"] = clientEvent.ActorId;
            }

            package.AuditEvents.Add(new AuditEvent
            {
                EvidencePackageId = package.EvidencePackageId,
                EventType = clientEvent.EventType,
                ActorType = clientEvent.ActorType,
                ActorId = submittedBy,
                Message = SensitiveDataGuard.SanitizeText(clientEvent.Message, protectedValues),
                Metadata = metadata,
                Timestamp = clientEvent.Timestamp ?? _timeProvider.GetUtcNow()
            });
        }
    }
}
