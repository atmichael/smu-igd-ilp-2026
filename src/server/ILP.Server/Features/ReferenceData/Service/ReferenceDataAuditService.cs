using ILP.Server.Features.ReferenceData.Repository;
using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class ReferenceDataAuditService(IReferenceDataRepository repository, TimeProvider timeProvider)
{
    public SynchronizationRunDto RecordRun(SynchronizationRunDto run) => repository.SaveRun(run);

    public RuleDecisionTraceDto RecordDecision(string checkIdentity, string? documentId, string snapshotId, ReferenceOutcome outcome, string reasonCode)
    {
        var trace = new RuleDecisionTraceDto(Guid.NewGuid().ToString("D"), checkIdentity, documentId, snapshotId,
            outcome, reasonCode, [], [], timeProvider.GetUtcNow());
        repository.SaveTrace(trace);
        return trace;
    }
}
