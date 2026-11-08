namespace ILP.Server.Features.Verification.Review;

public sealed record VerificationReviewAudit(string ReviewItemId, string UserId, string Reason, DateTimeOffset ResolvedAt);

public static class VerificationReviewAuditService
{
    public static VerificationReviewAudit Create(string reviewItemId, string userId, string reason) =>
        new(reviewItemId, userId, reason.Trim(), DateTimeOffset.UtcNow);
}
