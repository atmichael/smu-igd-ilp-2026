using System.Text.Json;
using System.Text.Json.Serialization;

namespace ILP.Shared.Evidence;

/// <summary>Serializes evidence enums using the kebab-case wire names from the data model (e.g. <c>pending-review</c>).</summary>
public sealed class KebabCaseEnumConverter<TEnum> : JsonStringEnumConverter<TEnum>
    where TEnum : struct, Enum
{
    public KebabCaseEnumConverter()
        : base(JsonNamingPolicy.KebabCaseLower, allowIntegerValues: false)
    {
    }
}

public static class WireEnum
{
    public static string ToWire<TEnum>(TEnum value) where TEnum : struct, Enum =>
        JsonNamingPolicy.KebabCaseLower.ConvertName(value.ToString());

    public static IEnumerable<string> WireNames<TEnum>() where TEnum : struct, Enum =>
        Enum.GetValues<TEnum>().Select(ToWire);

    public static bool TryParse<TEnum>(string? value, out TEnum result) where TEnum : struct, Enum
    {
        result = default;
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var normalized = value.Trim().Replace('_', '-');
        foreach (var candidate in Enum.GetValues<TEnum>())
        {
            if (string.Equals(ToWire(candidate), normalized, StringComparison.OrdinalIgnoreCase))
            {
                result = candidate;
                return true;
            }
        }

        return false;
    }
}

[JsonConverter(typeof(KebabCaseEnumConverter<ReviewStatus>))]
public enum ReviewStatus
{
    Draft,
    PendingReview,
    Reviewed,
    Confirmed,
    Rejected,
    Superseded,
    Archived,
    Failed
}

[JsonConverter(typeof(KebabCaseEnumConverter<EvidencePackageType>))]
public enum EvidencePackageType
{
    Invoice,
    PurchaseOrder,
    Receipt,
    Mixed
}

[JsonConverter(typeof(KebabCaseEnumConverter<DocumentType>))]
public enum DocumentType
{
    Invoice,
    PurchaseOrder,
    Receipt,
    OtherEvidence
}

[JsonConverter(typeof(KebabCaseEnumConverter<RecordCategory>))]
public enum RecordCategory
{
    DocumentHeader,
    InvoiceLine,
    PurchaseOrderCommitment,
    GoodsReceipt,
    ServiceAcceptance
}

[JsonConverter(typeof(KebabCaseEnumConverter<VerificationStatus>))]
public enum VerificationStatus
{
    Unverified,
    Passed,
    Failed,
    NeedsReview
}

[JsonConverter(typeof(KebabCaseEnumConverter<ProvenanceEventType>))]
public enum ProvenanceEventType
{
    Extracted,
    Corrected,
    Verified,
    Rejected,
    Superseded,
    Finalized
}

[JsonConverter(typeof(KebabCaseEnumConverter<AuditEventType>))]
public enum AuditEventType
{
    SourceAttached,
    ModelVersioned,
    SchemaVersioned,
    VerificationResult,
    UserCorrection,
    MatchOutcome,
    Finalized,
    Rejected,
    Archived,
    StatusChanged,
    Superseded,
    SaveFailed
}

[JsonConverter(typeof(KebabCaseEnumConverter<ActorType>))]
public enum ActorType
{
    Model,
    User,
    System,
    Reviewer
}

[JsonConverter(typeof(KebabCaseEnumConverter<MatchOutcomeType>))]
public enum MatchOutcomeType
{
    Matched,
    Discrepancy,
    Unmatched
}
