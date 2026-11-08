using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class SimplifiedTaxInvoiceVerifier
{
    public static VerificationCheckDto VerifyThreshold(DocumentEntity document, bool evidenceAvailable)
    {
        var supported = evidenceAvailable && document.TotalAmount <= 1000m;
        return new VerificationCheckDto(
            Guid.NewGuid().ToString("N"), "simplified-invoice-threshold", VerificationCheckType.Applicability,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Fail, supported ? "SUPPORTED" : "SIMPLIFIED_INVOICE_THRESHOLD_EXCEEDED_OR_UNSUPPORTED",
            supported ? "The GST-inclusive total does not exceed S$1,000." : "The GST-inclusive total exceeds S$1,000 or source evidence is unavailable.",
            document.TotalAmount.ToString("0.00"), "S$1,000 or less including GST", evidenceAvailable ? ["evidence"] : [], !supported, null);
    }
}
