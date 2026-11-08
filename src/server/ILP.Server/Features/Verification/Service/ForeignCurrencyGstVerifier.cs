using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class ForeignCurrencyGstVerifier
{
    public static VerificationCheckDto Verify(decimal foreignAmount, ApprovedExchangeRate? rate)
    {
        var dependency = ExchangeRateVerifier.Verify(DateOnly.FromDateTime(DateTime.UtcNow), rate);
        if (!dependency.IsAvailable) return new(Guid.NewGuid().ToString("N"), "foreign-currency-gst", VerificationCheckType.Currency,
            VerificationOutcome.Unavailable, dependency.ReasonCode!, "Approved exchange-rate data is unavailable or inconsistent.", foreignAmount.ToString("0.00"), null, [], true, null);
        var converted = VerificationCalculationPolicy.RoundCent(foreignAmount * rate!.Rate);
        return new(Guid.NewGuid().ToString("N"), "foreign-currency-gst", VerificationCheckType.Currency, VerificationOutcome.Pass,
            "SUPPORTED", "Foreign-currency amount was converted using the approved effective exchange rate.", foreignAmount.ToString("0.00"), converted.ToString("0.00"), ["exchange-rate-reference"], false, null);
    }
}
