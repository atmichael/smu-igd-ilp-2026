using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class VerificationRunStatusService
{
    public static VerificationRunStatus Derive(IEnumerable<VerificationCheckDto> checks)
    {
        var values = checks.ToArray();
        return values.Any(check => check.Outcome is VerificationOutcome.Fail or VerificationOutcome.Unavailable)
            ? VerificationRunStatus.Blocked
            : VerificationRunStatus.Verified;
    }
}
