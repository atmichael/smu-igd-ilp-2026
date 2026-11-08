using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class ForeignCurrencyCreditNoteVerifier
{
    public static VerificationCheckDto Verify(string? originalBasis, string creditBasis, bool evidenceAvailable)
    {
        var consistent = evidenceAvailable && string.Equals(originalBasis, creditBasis, StringComparison.OrdinalIgnoreCase);
        return new(Guid.NewGuid().ToString("N"), "credit-note-currency-policy", VerificationCheckType.Currency,
            consistent ? VerificationOutcome.Pass : VerificationOutcome.Fail,
            consistent ? "SUPPORTED" : "CREDIT_NOTE_CURRENCY_POLICY_INCONSISTENT",
            consistent ? "Credit-note currency policy matches the original invoice." : "Credit-note currency policy is inconsistent with the original invoice or unsupported.",
            creditBasis, originalBasis, evidenceAvailable ? ["evidence"] : [], !consistent, null);
    }
}
