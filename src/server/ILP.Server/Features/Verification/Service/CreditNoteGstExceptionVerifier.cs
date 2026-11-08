using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class CreditNoteGstExceptionVerifier
{
    public static VerificationCheckDto Verify(bool isExceptionClaimed, bool writtenAgreementAvailable, bool customerIsFullyTaxable, bool statementAvailable)
    {
        if (!isExceptionClaimed) return new(Guid.NewGuid().ToString("N"), "credit-note-gst-adjustment-exception", VerificationCheckType.Applicability,
            VerificationOutcome.NotApplicable, "NOT_APPLICABLE", "The no-GST-adjustment exception is not claimed.", null, null, [], false, null);
        var supported = writtenAgreementAvailable && customerIsFullyTaxable && statementAvailable;
        return new(Guid.NewGuid().ToString("N"), "credit-note-gst-adjustment-exception", VerificationCheckType.Adjustment,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Fail,
            supported ? "SUPPORTED" : "GST_ADJUSTMENT_EXCEPTION_UNSUPPORTED",
            supported ? "All conditions for the no-GST-adjustment exception are supported." : "Written agreement, fully taxable customer status, or required statement is missing.",
            isExceptionClaimed.ToString(), "true", supported ? ["evidence"] : [], !supported, null);
    }
}
