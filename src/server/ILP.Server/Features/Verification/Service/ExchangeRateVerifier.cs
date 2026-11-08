namespace ILP.Server.Features.Verification.Service;

public sealed record ApprovedExchangeRate(decimal Rate, DateOnly EffectiveDate, string Basis);

public static class ExchangeRateVerifier
{
    public static VerificationDependencyResult Verify(DateOnly documentDate, ApprovedExchangeRate? rate)
    {
        if (rate is null) return new(false, "APPROVED_EXCHANGE_RATE_UNAVAILABLE");
        return rate.EffectiveDate == documentDate ? new(true, null) : new(false, "EXCHANGE_RATE_EFFECTIVE_DATE_MISMATCH");
    }
}
