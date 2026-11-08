using ILP.Server.Features.Verification.Persistence;

namespace ILP.Server.Features.Verification.Review;

public sealed class VerificationReviewService(IVerificationRepository repository)
{
    public void Resolve(string reviewItemId, string userId, string reason) => repository.Resolve(reviewItemId, userId, reason);
}
