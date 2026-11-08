using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class SimplifiedDocumentVerifier
{
    public static VerificationCheckDto VerifyVariant(string variant, DocumentEntity document, bool evidenceAvailable)
    {
        var supported = evidenceAvailable && variant is "DebitNote" or "SalesVoucher" && document.TotalAmount <= 1000m;
        return new VerificationCheckDto(
            Guid.NewGuid().ToString("N"), "simplified-document-applicability", VerificationCheckType.Applicability,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Fail, supported ? "SUPPORTED" : "SIMPLIFIED_INVOICE_REQUIREMENTS_NOT_MET",
            supported ? "The document satisfies simplified tax-invoice applicability and required fields." : "A sales voucher or debit note can use simplified-invoice status only with evidence, required fields, and a total no greater than S$1,000.",
            document.TotalAmount.ToString("0.00"), "S$1,000 or less including GST", evidenceAvailable ? ["evidence"] : [], !supported, null);
    }
}
