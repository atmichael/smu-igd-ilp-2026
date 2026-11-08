using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.Verification.Service;

public static class SubtotalGstVerifier
{
    public static decimal Calculate(DocumentEntity document) => VerificationCalculationPolicy.SubtotalGst(document.Subtotal, document.SubtotalTaxRate);
}
