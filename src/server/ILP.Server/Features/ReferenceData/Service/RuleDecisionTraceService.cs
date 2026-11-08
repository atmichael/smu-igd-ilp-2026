using ILP.Shared.ReferenceData;
using ILP.Server.Features.ReferenceData.Repository;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class RuleDecisionTraceService(IReferenceDataRepository repository, TimeProvider timeProvider)
{
    public RuleDecisionTraceDto Record(string checkIdentity, string? documentOrTransactionId, ReferenceSnapshotDto snapshot,
        ReferenceOutcome outcome, string reasonCode, IEnumerable<string>? referenceVersionIds = null,
        IEnumerable<string>? rulePackageVersionIds = null)
    {
        var trace = new RuleDecisionTraceDto(
            Guid.NewGuid().ToString("D"), checkIdentity, documentOrTransactionId, snapshot.SnapshotId,
            outcome, reasonCode,
            referenceVersionIds?.Distinct().ToArray() ?? [],
            rulePackageVersionIds?.Distinct().ToArray() ?? [],
            timeProvider.GetUtcNow());
        repository.SaveTrace(trace);
        return trace;
    }
}
