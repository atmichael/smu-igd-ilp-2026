using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class CreditNoteVerifier
{
    public static VerificationCheckDto Verify(DocumentEntity document, bool evidenceAvailable)
    {
        var supported = evidenceAvailable && !string.IsNullOrWhiteSpace(document.RefNumber) && document.RelatedDocumentNumbers is not null;
        return new(Guid.NewGuid().ToString("N"), "credit-note-adjustment-details", VerificationCheckType.Adjustment,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Fail,
            supported ? "SUPPORTED" : "CREDIT_NOTE_ADJUSTMENT_UNSUPPORTED",
            supported ? "Credit-note number and original-document relationship are supported by evidence." : "Credit-note details or original-document relationship lack evidence.",
            document.RefNumber, document.RelatedDocumentNumbers, evidenceAvailable ? ["evidence"] : [], !supported, null);
    }
}
