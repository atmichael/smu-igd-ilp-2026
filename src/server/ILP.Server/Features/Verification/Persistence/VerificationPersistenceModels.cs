namespace ILP.Server.Features.Verification.Persistence;

public sealed class VerificationRunEntity
{
    public long Id { get; set; }
    public string VerificationRunId { get; set; } = string.Empty;
    public string DocumentId { get; set; } = string.Empty;
    public string RunJson { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTimeOffset CreatedAt { get; set; }
}

public sealed class VerificationReviewResolutionEntity
{
    public long Id { get; set; }
    public string ReviewItemId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string ResolutionReason { get; set; } = string.Empty;
    public DateTimeOffset ResolvedAt { get; set; }
}
