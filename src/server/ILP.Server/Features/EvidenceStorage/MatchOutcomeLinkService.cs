using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Links a match decision made elsewhere to the evidence it relied on; it does not evaluate matching rules.</summary>
public sealed class MatchOutcomeLinkService
{
    private static readonly ReviewStatus[] LinkableStatuses = [ReviewStatus.PendingReview, ReviewStatus.Reviewed, ReviewStatus.Confirmed];

    private readonly AuditTrailService _audit;
    private readonly TimeProvider _timeProvider;

    public MatchOutcomeLinkService(AuditTrailService audit, TimeProvider timeProvider)
    {
        _audit = audit;
        _timeProvider = timeProvider;
    }

    public MatchOutcome RecordOutcome(EvidencePackage package, MatchOutcomeRequest request, string? actorId)
    {
        if (!LinkableStatuses.Contains(package.ReviewStatus))
        {
            throw new EvidencePreconditionException(
                $"Match outcomes cannot be linked to '{WireEnum.ToWire(package.ReviewStatus)}' evidence.");
        }

        EvidencePackageValidator.Require("matchReviewId", request.MatchReviewId, "A match review identifier is required.");
        var outcomeType = EvidencePackageValidator.ParseRequired<MatchOutcomeType>("outcome", request.Outcome);

        var knownRecordIds = package.Documents
            .SelectMany(document => document.Records)
            .Where(record => record.ReviewStatus != ReviewStatus.Rejected)
            .Select(record => record.RecordId)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requested = (request.SupportingRecordIds ?? new List<string>())
            .Where(id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var missing = requested.Where(id => !knownRecordIds.Contains(id)).ToList();

        var outcome = new MatchOutcome
        {
            MatchReviewId = request.MatchReviewId.Trim(),
            Outcome = outcomeType,
            SupportingRecordIds = requested.Except(missing, StringComparer.OrdinalIgnoreCase).ToList(),
            MissingRecordIds = missing,
            // A match with no supporting records, or with records not in this package, is stored but flagged as partial.
            EvidenceComplete = requested.Count > 0 && missing.Count == 0,
            Reason = request.Reason is null ? null : SensitiveDataGuard.SanitizeText(request.Reason, SensitiveDataGuard.ProtectedValues(package)),
            ActorType = ActorType.System,
            ActorId = actorId,
            RecordedAt = _timeProvider.GetUtcNow()
        };

        package.MatchOutcomes.Add(outcome);
        package.RelatedMatchReviewId = outcome.MatchReviewId;
        package.UpdatedAt = outcome.RecordedAt;

        _audit.Append(package, AuditEventType.MatchOutcome, ActorType.System, actorId, "Match outcome linked to evidence package.", metadata: new Dictionary<string, string>
        {
            ["matchReviewId"] = outcome.MatchReviewId,
            ["outcome"] = WireEnum.ToWire(outcome.Outcome),
            ["evidenceComplete"] = outcome.EvidenceComplete ? "true" : "false",
            ["supportingRecordCount"] = outcome.SupportingRecordIds.Count.ToString(),
            ["missingRecordCount"] = outcome.MissingRecordIds.Count.ToString()
        });

        return outcome;
    }
}
