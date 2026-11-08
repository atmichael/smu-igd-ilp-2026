namespace ILP.Server.Features.Verification.Service;

public static class GstRoundingVerifier
{
    public static bool MatchesAfterPolicy(decimal displayed, decimal calculated) => VerificationCalculationPolicy.MatchesCent(displayed, calculated);
}
