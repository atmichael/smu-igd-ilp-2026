using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Adapters;

public interface IReferenceDataSourceAdapter
{
    string Source { get; }
    bool SupportsUpdateNotifications { get; }

    Task<IReadOnlyList<ReferenceDataRecordDto>> ReadRecordsAsync(CancellationToken cancellationToken = default);
}
