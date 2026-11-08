namespace ILP.Shared.ReferenceData;

public sealed record BusinessRulePackageDto(
    string BusinessRulePackageId,
    string Jurisdiction,
    string DocumentVariant,
    DateOnly EffectiveStart,
    DateOnly? EffectiveEnd,
    string SourceAuthority,
    string SourceLink,
    IReadOnlyDictionary<string, string> RuleValues,
    string ReviewStatus,
    string ReviewOwner,
    DateOnly? ReviewedAt,
    DateOnly? NextReviewAt,
    string Version);
