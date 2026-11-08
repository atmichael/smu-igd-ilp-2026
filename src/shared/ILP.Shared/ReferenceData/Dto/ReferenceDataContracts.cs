namespace ILP.Shared.ReferenceData;

public enum ReferenceOutcome
{
    Passed,
    Failed,
    Unverified,
    RequiresReview
}

public sealed record ReferenceDataRecordDto(
    string ReferenceDataRecordId,
    string Category,
    string Identity,
    string SourceSystem,
    string? SourceRecordId,
    string? SourceLink,
    IReadOnlyDictionary<string, string> Values,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    string Version,
    string Status);

public sealed record ReferenceAliasDto(string AliasValue, string CanonicalIdentity, string SourceSystem, string SourceRecordId);

public sealed record ReferenceSnapshotDto(
    string SnapshotId,
    string Status,
    string Source,
    DateTimeOffset PublishedAt,
    string Version,
    IReadOnlyList<ReferenceDataRecordDto> Records,
    IReadOnlyList<ReferenceAliasDto> Aliases);

public sealed record SynchronizationRunDto(
    string RunId,
    string Source,
    string TriggerType,
    string Status,
    DateTimeOffset StartedAt,
    DateTimeOffset? CompletedAt,
    string? Error,
    string? PublishedSnapshotId);

public sealed record PolicySourceDto(
    string Source,
    string Authority,
    string OfficialLink,
    string ReviewStatus,
    string ReviewOwner,
    DateOnly? LastReviewedAt,
    DateOnly? NextReviewAt);

public sealed record RuleDecisionTraceDto(
    string TraceId,
    string CheckIdentity,
    string? DocumentOrTransactionId,
    string SnapshotId,
    ReferenceOutcome Outcome,
    string ReasonCode,
    IReadOnlyList<string> ReferenceVersionIds,
    IReadOnlyList<string> RulePackageVersionIds,
    DateTimeOffset CreatedAt);
