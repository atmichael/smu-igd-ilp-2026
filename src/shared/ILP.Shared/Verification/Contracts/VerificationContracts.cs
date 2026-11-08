using ILP.Shared.Verification.Enums;

namespace ILP.Shared.Verification.Contracts;

public sealed record VerificationCalculationTraceDto(
    decimal? TaxableValue,
    decimal? EffectiveRate,
    decimal? CalculatedGstAmount,
    decimal? DisplayedGstAmount,
    decimal? CalculatedTotal,
    decimal? DisplayedTotal,
    string RoundingPolicy,
    decimal? CashRoundingAmount,
    string CurrencyCode,
    string? ExchangeRateBasis,
    decimal? ExchangeRate,
    DateOnly? ExchangeRateDate,
    decimal? ConvertedAmount);

public sealed record VerificationCheckDto(
    string VerificationCheckId,
    string? FieldIdentifier,
    VerificationCheckType CheckType,
    VerificationOutcome Outcome,
    string ReasonCode,
    string Message,
    string? ExtractedValue,
    string? ExpectedValue,
    string[] EvidenceReferenceIds,
    bool RequiresReview,
    VerificationCalculationTraceDto? CalculationTrace);

public sealed record VerificationRunDto(
    string VerificationRunId,
    string DocumentId,
    string DocumentVariant,
    VerificationRunStatus Status,
    string RulePackageVersionId,
    string? EvidencePackageId,
    IReadOnlyList<VerificationCheckDto> Checks,
    DateTimeOffset CompletedAt);

public sealed record StartVerificationRequest(string DocumentId, string? EvidencePackageId, string? OriginalInvoiceReferenceId);
public sealed record ResolveReviewItemRequest(string ResolutionReason);

public sealed record VerificationReviewItemDto(
    string ReviewItemId,
    string VerificationRunId,
    string VerificationCheckId,
    string Status,
    string? ResolutionReason,
    string? ResolvedByUserId,
    DateTimeOffset? ResolvedAt);
