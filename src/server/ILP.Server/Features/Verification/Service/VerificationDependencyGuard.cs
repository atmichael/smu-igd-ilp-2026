namespace ILP.Server.Features.Verification.Service;

public sealed record VerificationDependencyResult(bool IsAvailable, string? ReasonCode);

public static class VerificationDependencyGuard
{
    public static VerificationDependencyResult RequireEvidence(string? evidencePackageId) =>
        string.IsNullOrWhiteSpace(evidencePackageId)
            ? new(false, "SOURCE_EVIDENCE_UNAVAILABLE")
            : new(true, null);

    public static VerificationDependencyResult RequireRule(string? rulePackageVersionId) =>
        string.IsNullOrWhiteSpace(rulePackageVersionId)
            ? new(false, "EFFECTIVE_RULE_UNAVAILABLE")
            : new(true, null);

    public static VerificationDependencyResult RequireVendor(bool found) =>
        found ? new(true, null) : new(false, "APPROVED_VENDOR_UNAVAILABLE");

    public static VerificationDependencyResult RequireExchangeRate(decimal? rate) =>
        rate is > 0 ? new(true, null) : new(false, "APPROVED_EXCHANGE_RATE_UNAVAILABLE");
}
