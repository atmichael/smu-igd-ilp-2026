namespace ILP.Server.Features.Verification.Service;

public sealed record VerificationInputFingerprint(string Classification, string EvidenceVersion, string RuleVersion, string? VendorVersion, string? CurrencyVersion, string? OriginalInvoiceVersion);

public static class VerificationReevaluationService
{
    public static bool RequiresNewRun(VerificationInputFingerprint current, VerificationInputFingerprint previous) =>
        !string.Equals(current.Classification, previous.Classification, StringComparison.Ordinal) ||
        !string.Equals(current.EvidenceVersion, previous.EvidenceVersion, StringComparison.Ordinal) ||
        !string.Equals(current.RuleVersion, previous.RuleVersion, StringComparison.Ordinal) ||
        !string.Equals(current.VendorVersion, previous.VendorVersion, StringComparison.Ordinal) ||
        !string.Equals(current.CurrencyVersion, previous.CurrencyVersion, StringComparison.Ordinal) ||
        !string.Equals(current.OriginalInvoiceVersion, previous.OriginalInvoiceVersion, StringComparison.Ordinal);
}
