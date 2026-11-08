namespace ILP.Server.Features.Verification.Service;

public static class CashRoundingVerifier
{
    public static decimal CalculateAdjustment(decimal gstInclusiveAmount)
    {
        var nearestFiveCents = Math.Round(gstInclusiveAmount * 20m, MidpointRounding.AwayFromZero) / 20m;
        return VerificationCalculationPolicy.RoundCent(nearestFiveCents - gstInclusiveAmount);
    }
}
