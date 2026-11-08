namespace ILP.Server.Features.Verification.Service;

public static class VerificationCalculationPolicy
{
    public static decimal RoundCent(decimal value) => decimal.Round(value, 2, MidpointRounding.AwayFromZero);

    public static decimal SubtotalGst(decimal taxableSubtotal, decimal rate) => RoundCent(taxableSubtotal * rate);

    public static decimal ItemByItemGst(IEnumerable<(decimal TaxableValue, decimal Rate)> items) =>
        RoundCent(items.Sum(item => RoundCent(item.TaxableValue * item.Rate)));

    public static bool MatchesCent(decimal displayed, decimal calculated) =>
        RoundCent(displayed) == RoundCent(calculated);
}
