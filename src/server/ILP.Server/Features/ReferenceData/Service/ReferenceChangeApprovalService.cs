namespace ILP.Server.Features.ReferenceData.Service;

public sealed record ReferenceChangeRequest(string ObjectId, string ChangedBy, string Reason, DateOnly EffectiveDate);
public sealed record ReferenceChangeDecision(string ChangeId, string Status, string? Approver, string? ApprovalReason);

public sealed class ReferenceChangeApprovalService
{
    public ReferenceChangeDecision Submit(ReferenceChangeRequest request) =>
        new(Guid.NewGuid().ToString("D"), "PendingApproval", null, null);

    public ReferenceChangeDecision Approve(ReferenceChangeDecision decision, string approver, string approvalReason)
    {
        if (string.Equals(decision.Approver, approver, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("The requester and approver must be different people.");
        return decision with { Status = "Approved", Approver = approver, ApprovalReason = approvalReason };
    }
}
