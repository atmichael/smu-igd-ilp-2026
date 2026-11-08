using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class MixedSupplyVerifier
{
    public static VerificationCheckDto Verify(DocumentEntity document, bool evidenceAvailable)
    {
        var distinctRates = document.DocumentItems.Select(item => item.TaxRate).Distinct().Count();
        if (distinctRates <= 1) return NotApplicable();
        var separated = evidenceAvailable && document.DocumentItems.Any(item => item.TaxRate != 0m) && document.DocumentItems.All(item => item.Amount.HasValue || item.UnitPrice != 0m);
        return new(Guid.NewGuid().ToString("N"), "mixed-supply-gross-amounts", VerificationCheckType.Applicability,
            separated ? VerificationOutcome.Pass : VerificationOutcome.Fail,
            separated ? "SUPPORTED" : "MIXED_SUPPLY_AMOUNT_NOT_SEPARATED",
            separated ? "Mixed supply treatments and gross amounts are separately identifiable." : "Mixed supply treatments or gross amounts are not separately identifiable.",
            distinctRates.ToString(), distinctRates.ToString(), evidenceAvailable ? ["evidence"] : [], !separated, null);
    }

    private static VerificationCheckDto NotApplicable() => new(Guid.NewGuid().ToString("N"), "mixed-supply-gross-amounts", VerificationCheckType.Applicability,
        VerificationOutcome.NotApplicable, "NOT_APPLICABLE", "The supplies do not contain multiple GST treatments.", null, null, [], false, null);
}
