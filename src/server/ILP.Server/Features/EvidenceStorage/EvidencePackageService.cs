using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>
/// Orchestrates evidence package lifecycle. Writes are serialized so duplicate checks and saves are atomic within this process;
/// a multi-instance deployment needs a store-level uniqueness constraint instead.
/// </summary>
public sealed class EvidencePackageService
{
    private static readonly ReviewStatus[] FinalizableStatuses = [ReviewStatus.Draft, ReviewStatus.PendingReview, ReviewStatus.Reviewed];

    private readonly IEvidenceRepository _repository;
    private readonly IDocumentContentStore _contentStore;
    private readonly AuditTrailService _audit;
    private readonly RecordChangeService _recordChanges;
    private readonly MatchOutcomeLinkService _matchLinks;
    private readonly RetentionRules _retention;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EvidencePackageService> _logger;
    private readonly object _writeLock = new();

    public EvidencePackageService(
        IEvidenceRepository repository,
        IDocumentContentStore contentStore,
        AuditTrailService audit,
        RecordChangeService recordChanges,
        MatchOutcomeLinkService matchLinks,
        RetentionRules retention,
        TimeProvider timeProvider,
        ILogger<EvidencePackageService> logger)
    {
        _repository = repository;
        _contentStore = contentStore;
        _audit = audit;
        _recordChanges = recordChanges;
        _matchLinks = matchLinks;
        _retention = retention;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<EvidencePackage> CreateAsync(CreateEvidencePackageRequest request, string? actorId, CancellationToken cancellationToken = default)
    {
        foreach (var document in request.Documents ?? [])
        {
            if (string.IsNullOrWhiteSpace(document.StorageLocation)
                && DocumentContentLocations.ParseId(document.SourceDocumentId ?? string.Empty) is { } intakeId
                && await _contentStore.ExistsAsync(intakeId.ToString("D"), cancellationToken))
            {
                document.StorageLocation = DocumentContentLocations.For(intakeId.ToString("D"));
            }
        }

        var validated = EvidencePackageValidator.ValidateCreate(request, _timeProvider.GetUtcNow());
        var package = validated.Package;
        _audit.AppendCreationEvents(package, ActorType.User, actorId);
        _audit.AppendClientEvents(package, validated.AuditEvents, actorId);

        lock (_writeLock)
        {
            if (package.ReplacesEvidencePackageId is { } replacedId)
            {
                var replaced = _repository.Get(replacedId)
                    ?? throw new EvidenceNotFoundException($"Evidence package {replacedId} to replace was not found.");
                if (!string.Equals(replaced.CaseId, package.CaseId, StringComparison.OrdinalIgnoreCase))
                {
                    throw new EvidenceValidationException(new Dictionary<string, string[]>
                    {
                        ["replacesEvidencePackageId"] = ["A replacement must belong to the same case as the package it replaces."]
                    });
                }

                if (replaced.ReviewStatus != ReviewStatus.Confirmed)
                {
                    throw new EvidencePreconditionException("Only the current confirmed package can be replaced.");
                }

                package.Version = replaced.Version + 1;
            }
            else
            {
                EnsureNoConfirmedDuplicate(package, excludedIds: [package.EvidencePackageId]);
            }

            try
            {
                _repository.Save(package);
            }
            catch (Exception ex)
            {
                RecordFailedSave(package, ex);
                throw new EvidencePersistenceException(package.EvidencePackageId, WireEnum.ToWire(ReviewStatus.Failed), ex);
            }
        }

        return package;
    }

    public EvidencePackage Get(string evidencePackageId) =>
        RetentionRules.ApplyAccessRestrictions(Load(evidencePackageId));

    public IReadOnlyList<EvidencePackage> Query(string? caseId, string? documentId, string? sourceReference, string? reviewStatus)
    {
        ReviewStatus? status = string.IsNullOrWhiteSpace(reviewStatus)
            ? null
            : EvidencePackageValidator.ParseRequired<ReviewStatus>("reviewStatus", reviewStatus);

        var matches = _repository.List().AsEnumerable();

        if (!string.IsNullOrWhiteSpace(caseId))
        {
            matches = matches.Where(package => string.Equals(package.CaseId, caseId, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(documentId))
        {
            matches = matches.Where(package => package.Documents.Any(document =>
                string.Equals(document.DocumentId, documentId, StringComparison.OrdinalIgnoreCase)
                || string.Equals(document.SourceDocumentId, documentId, StringComparison.OrdinalIgnoreCase)));
        }

        if (!string.IsNullOrWhiteSpace(sourceReference))
        {
            matches = matches.Where(package => package.Documents.Any(document =>
                string.Equals(document.SourceReference, sourceReference, StringComparison.OrdinalIgnoreCase)));
        }

        if (status is { } wanted)
        {
            matches = matches.Where(package => package.ReviewStatus == wanted
                || package.Documents.Any(document => document.ReviewStatus == wanted
                    || document.Records.Any(record => record.ReviewStatus == wanted)));
        }

        return matches
            .OrderBy(package => package.CreatedAt)
            .Select(RetentionRules.ApplyAccessRestrictions)
            .ToList();
    }

    public EvidencePackage Finalize(string evidencePackageId, string? actorId)
    {
        lock (_writeLock)
        {
            var package = Load(evidencePackageId);
            var original = Load(evidencePackageId);
            if (!FinalizableStatuses.Contains(package.ReviewStatus))
            {
                throw new EvidencePreconditionException(
                    $"Package cannot be finalized from '{WireEnum.ToWire(package.ReviewStatus)}'; expected draft, pending-review, or reviewed.");
            }

            var activeDocuments = package.Documents.Where(document => document.ReviewStatus != ReviewStatus.Rejected).ToList();
            if (activeDocuments.Count == 0)
            {
                throw new EvidencePreconditionException("Package cannot be finalized without at least one non-rejected source document.");
            }

            if (activeDocuments.Any(document => string.IsNullOrWhiteSpace(document.StorageLocation) || string.IsNullOrWhiteSpace(document.SourceReference)))
            {
                throw new EvidencePreconditionException("Every source document must have a source reference and protected storage location before finalization.");
            }

            EvidencePackage? replaced = null;
            if (package.ReplacesEvidencePackageId is { } replacedId)
            {
                replaced = _repository.Get(replacedId);
                if (replaced is null || replaced.ReviewStatus != ReviewStatus.Confirmed)
                {
                    throw new EvidenceDuplicateException("The package being replaced is no longer the current confirmed version.");
                }
            }

            EnsureNoConfirmedDuplicate(package, excludedIds: [package.EvidencePackageId, package.ReplacesEvidencePackageId]);

            var now = _timeProvider.GetUtcNow();
            package.ReviewStatus = ReviewStatus.Confirmed;
            package.FinalizedAt = now;
            package.UpdatedAt = now;
            package.RetentionPolicy = RetentionRules.PilotPolicyName;
            package.RetentionUntil = _retention.RetentionUntil(now);
            foreach (var document in activeDocuments)
            {
                document.ReviewStatus = ReviewStatus.Confirmed;
                foreach (var record in document.Records.Where(record => record.ReviewStatus != ReviewStatus.Rejected))
                {
                    record.ReviewStatus = ReviewStatus.Confirmed;
                    AppendSystemProvenanceEntry(package, document, record, ProvenanceEventType.Finalized, actorId, "Package finalized", now);
                }
            }

            _audit.Append(package, AuditEventType.Finalized, ActorType.Reviewer, actorId, "Evidence package finalized.", metadata: new Dictionary<string, string>
            {
                ["finalizedAt"] = now.ToString("O"),
                ["retentionUntil"] = package.RetentionUntil.Value.ToString("O"),
                ["version"] = package.Version.ToString()
            });

            if (replaced is not null)
            {
                MarkSuperseded(replaced, package, actorId, now);
            }

            Persist(package, WireEnum.ToWire(original.ReviewStatus));
            if (replaced is not null)
            {
                try
                {
                    _repository.Save(replaced);
                }
                catch (Exception ex)
                {
                    // Roll the new package back so two confirmed versions never coexist.
                    Persist(original, WireEnum.ToWire(original.ReviewStatus));
                    LogSaveFailure(replaced.EvidencePackageId, ex);
                    throw new EvidencePersistenceException(package.EvidencePackageId, WireEnum.ToWire(original.ReviewStatus), ex);
                }
            }

            return package;
        }
    }

    public EvidencePackage CorrectRecord(string evidencePackageId, string recordId, RecordCorrectionRequest request, string? actorId) =>
        Mutate(evidencePackageId, package => _recordChanges.ApplyCorrection(package, recordId, request, actorId));

    public EvidencePackage VerifyRecord(string evidencePackageId, string recordId, RecordVerificationRequest request, string? actorId) =>
        Mutate(evidencePackageId, package => _recordChanges.ApplyVerification(package, recordId, request, actorId));

    public EvidencePackage ChangeStatus(string evidencePackageId, StatusChangeRequest request, string? actorId) =>
        Mutate(evidencePackageId, package => _recordChanges.ApplyStatusChange(package, request, actorId));

    public EvidencePackage LinkMatchOutcome(string evidencePackageId, MatchOutcomeRequest request, string? actorId) =>
        Mutate(evidencePackageId, package => _matchLinks.RecordOutcome(package, request, actorId));

    public EvidencePackage Archive(string evidencePackageId, string? actorId) =>
        RetentionRules.ApplyAccessRestrictions(Mutate(evidencePackageId, package =>
        {
            var now = _timeProvider.GetUtcNow();
            _retention.EnsureCanArchive(package, now);
            package.ReviewStatus = ReviewStatus.Archived;
            package.ArchivedAt = now;
            package.UpdatedAt = now;
            foreach (var document in package.Documents)
            {
                document.ReviewStatus = ReviewStatus.Archived;
                foreach (var record in document.Records)
                {
                    record.ReviewStatus = ReviewStatus.Archived;
                }
            }

            _audit.Append(package, AuditEventType.Archived, ActorType.System, actorId, "Evidence package archived after retention period.", metadata: new Dictionary<string, string>
            {
                ["retentionPolicy"] = package.RetentionPolicy,
                ["archivedAt"] = now.ToString("O")
            });
        }));

    private EvidencePackage Mutate(string evidencePackageId, Action<EvidencePackage> change)
    {
        lock (_writeLock)
        {
            var package = Load(evidencePackageId);
            var previousStatus = WireEnum.ToWire(package.ReviewStatus);
            change(package);
            Persist(package, previousStatus);
            return package;
        }
    }

    private EvidencePackage Load(string evidencePackageId) =>
        _repository.Get(evidencePackageId)
            ?? throw new EvidenceNotFoundException($"No evidence package was found for {evidencePackageId}.");

    private void Persist(EvidencePackage package, string visibleStatusOnFailure)
    {
        try
        {
            _repository.Save(package);
        }
        catch (Exception ex)
        {
            LogSaveFailure(package.EvidencePackageId, ex);
            throw new EvidencePersistenceException(package.EvidencePackageId, visibleStatusOnFailure, ex);
        }
    }

    private void EnsureNoConfirmedDuplicate(EvidencePackage candidate, IReadOnlyCollection<string?> excludedIds)
    {
        var references = candidate.Documents
            .Where(document => document.ReviewStatus != ReviewStatus.Rejected)
            .Select(document => document.SourceReference)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var duplicate = _repository.List().Any(existing =>
            existing.ReviewStatus == ReviewStatus.Confirmed
            && !excludedIds.Contains(existing.EvidencePackageId, StringComparer.OrdinalIgnoreCase)
            && string.Equals(existing.CaseId, candidate.CaseId, StringComparison.OrdinalIgnoreCase)
            && existing.Documents.Any(document => references.Contains(document.SourceReference)));

        if (duplicate)
        {
            throw new EvidenceDuplicateException(
                "Confirmed evidence already exists for this source and case; submit with replacesEvidencePackageId to create a new version.");
        }
    }

    private void MarkSuperseded(EvidencePackage replaced, EvidencePackage replacement, string? actorId, DateTimeOffset now)
    {
        replaced.ReviewStatus = ReviewStatus.Superseded;
        replaced.SupersededByEvidencePackageId = replacement.EvidencePackageId;
        replaced.UpdatedAt = now;
        foreach (var document in replaced.Documents)
        {
            document.ReviewStatus = ReviewStatus.Superseded;
            foreach (var record in document.Records)
            {
                record.ReviewStatus = ReviewStatus.Superseded;
                AppendSystemProvenanceEntry(replaced, document, record, ProvenanceEventType.Superseded, actorId, replacement.ReplacementReason, now);
            }
        }

        var metadata = new Dictionary<string, string>
        {
            ["supersededBy"] = replacement.EvidencePackageId,
            ["replacedPackage"] = replaced.EvidencePackageId,
            ["version"] = replacement.Version.ToString()
        };
        _audit.Append(replaced, AuditEventType.Superseded, ActorType.Reviewer, actorId, "Evidence package superseded by an explicit replacement.", metadata: metadata);
        _audit.Append(replacement, AuditEventType.Superseded, ActorType.Reviewer, actorId, "Evidence package replaced a prior confirmed version.", metadata: new Dictionary<string, string>(metadata));
    }

    private static void AppendSystemProvenanceEntry(EvidencePackage package, EvidenceDocument document, StructuredRecord record, ProvenanceEventType eventType, string? actorId, string? reason, DateTimeOffset now) =>
        record.Provenance.Add(new ProvenanceEntry
        {
            RecordId = record.RecordId,
            EvidencePackageId = package.EvidencePackageId,
            EventType = eventType,
            ActorType = ActorType.System,
            ActorId = actorId,
            SourceReference = document.SourceReference,
            Timestamp = now,
            Reason = reason
        });

    private void RecordFailedSave(EvidencePackage package, Exception error)
    {
        LogSaveFailure(package.EvidencePackageId, error);
        package.ReviewStatus = ReviewStatus.Failed;
        _audit.Append(package, AuditEventType.SaveFailed, ActorType.System, actorId: null, "Evidence package save failed; package was not committed.");

        try
        {
            _repository.Save(package);
        }
        catch (Exception markerError)
        {
            LogSaveFailure(package.EvidencePackageId, markerError);
        }
    }

    // Only identifiers and exception types are logged; exception messages may echo invoice content.
    private void LogSaveFailure(string evidencePackageId, Exception error) =>
        _logger.LogError("Evidence package {EvidencePackageId} save failed ({ExceptionType}).", evidencePackageId, error.GetType().Name);
}
