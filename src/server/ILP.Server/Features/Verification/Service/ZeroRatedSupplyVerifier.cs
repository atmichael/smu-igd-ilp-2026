using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class ZeroRatedSupplyVerifier
{
    public static VerificationCheckDto Verify(DocumentEntity document, bool evidenceAvailable)
    {
        var hasZeroRated = document.DocumentItems.Any(item => item.TaxRate == 0m);
        if (!hasZeroRated) return NotApplicable();
        var supported = evidenceAvailable;
        return new(Guid.NewGuid().ToString("N"), "zero-rated-gst-0", VerificationCheckType.FieldPresence,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Unavailable,
            supported ? "SUPPORTED" : "ZERO_RATED_EVIDENCE_UNAVAILABLE",
            supported ? "Zero-rated supply is supported with GST stated at 0%." : "Zero-rated supply evidence is unavailable.",
            "0%", "0%", evidenceAvailable ? ["evidence"] : [], !supported, null);
    }

    private static VerificationCheckDto NotApplicable() => new(Guid.NewGuid().ToString("N"), "zero-rated-gst-0", VerificationCheckType.Applicability,
        VerificationOutcome.NotApplicable, "NOT_APPLICABLE", "No zero-rated supply is present.", null, null, [], false, null);
}
