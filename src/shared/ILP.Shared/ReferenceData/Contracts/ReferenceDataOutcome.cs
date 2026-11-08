namespace ILP.Shared.ReferenceData;

public static class ReferenceDataOutcomeReason
{
    public const string MissingReference = "REFERENCE_MISSING";
    public const string SourceUnavailable = "SOURCE_UNAVAILABLE";
    public const string FailedSynchronization = "SYNCHRONIZATION_FAILED";
    public const string ConflictingReference = "REFERENCE_CONFLICT";
    public const string EffectiveCoverageMissing = "EFFECTIVE_COVERAGE_MISSING";
    public const string AmbiguousVariant = "DOCUMENT_VARIANT_AMBIGUOUS";

    public static string RecoveryGuidance(string reason) => reason switch
    {
        MissingReference => "Add or synchronize the required approved reference record, then retry.",
        SourceUnavailable => "Restore source connectivity or retry reconciliation, then publish a complete snapshot.",
        FailedSynchronization => "Review the synchronization run; repair the source payload and retry.",
        ConflictingReference => "Resolve the conflicting effective periods or source records through governance approval.",
        EffectiveCoverageMissing => "Add an approved rule package covering the document effective date.",
        AmbiguousVariant => "Review and confirm the tax-document classification before retrying.",
        _ => "Review the reference-data decision trace and resolve the recorded dependency."
    };
}
