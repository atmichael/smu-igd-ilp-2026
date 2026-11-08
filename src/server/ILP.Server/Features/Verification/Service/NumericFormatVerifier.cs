using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class NumericFormatVerifier
{
    public static VerificationCheckDto Verify(string fieldIdentifier, string? rawValue)
    {
        var supported = !string.IsNullOrWhiteSpace(rawValue) && decimal.TryParse(rawValue, System.Globalization.NumberStyles.Number, System.Globalization.CultureInfo.InvariantCulture, out _);
        return new(Guid.NewGuid().ToString("N"), fieldIdentifier, VerificationCheckType.NumericFormat,
            supported ? VerificationOutcome.Pass : VerificationOutcome.Fail,
            supported ? "SUPPORTED" : "NUMERIC_FORMAT_UNSUPPORTED",
            supported ? "Numeric format is supported." : "Numeric value is missing or uses an unsupported format.",
            rawValue, null, supported ? ["evidence"] : [], !supported, null);
    }
}
