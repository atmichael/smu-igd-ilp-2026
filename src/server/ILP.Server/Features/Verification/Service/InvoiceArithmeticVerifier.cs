namespace ILP.Server.Features.Verification.Service;

public static class InvoiceArithmeticVerifier
{
    public static bool ExtensionMatches(decimal quantity, decimal unitPrice, decimal displayed, decimal? amount = null)
    {
        var expected = amount ?? quantity * unitPrice;
        return VerificationCalculationPolicy.MatchesCent(displayed, expected);
    }

    public static bool TotalMatches(decimal taxable, decimal gst, decimal displayed) =>
        VerificationCalculationPolicy.MatchesCent(displayed, taxable + gst);
}
