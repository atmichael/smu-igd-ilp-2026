using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class OriginalInvoiceAdjustmentVerifier
{
    public static VerificationCheckDto Verify(DocumentEntity document, string? originalInvoiceReferenceId, bool evidenceAvailable)
    {
        var supported = evidenceAvailable && !string.IsNullOrWhiteSpace(originalInvoiceReferenceId);
        return new(Guid.NewGuid().ToString("N"), "original-invoice-adjustment", VerificationCheckType.Adjustment,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Fail,
            supported ? "SUPPORTED" : "ORIGINAL_INVOICE_REFERENCE_UNSUPPORTED",
            supported ? "The credit note has supported original-invoice evidence." : "The credit note adjustment cannot be linked to the original invoice.",
            document.RelatedDocumentNumbers, originalInvoiceReferenceId, evidenceAvailable ? ["evidence"] : [], !supported, null);
    }
}
