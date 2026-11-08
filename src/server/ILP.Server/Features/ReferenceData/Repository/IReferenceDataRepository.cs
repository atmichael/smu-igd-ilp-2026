using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Repository;

public interface IReferenceDataRepository
{
    ReferenceSnapshotDto? GetCurrentSnapshot(string source);
    IReadOnlyList<ReferenceSnapshotDto> GetSnapshots(string source);
    void Publish(ReferenceSnapshotDto snapshot);
    SynchronizationRunDto SaveRun(SynchronizationRunDto run);
    IReadOnlyList<SynchronizationRunDto> GetRuns(string source);
    void SaveTrace(RuleDecisionTraceDto trace);
    RuleDecisionTraceDto? GetTrace(string traceId);
}
