using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Adapters;

public sealed class IRasPolicySourceAdapter : IReferenceDataSourceAdapter
{
    public string Source => "IRAS GST invoicing guidance";
    public bool SupportsUpdateNotifications => false;

    public Task<IReadOnlyList<ReferenceDataRecordDto>> ReadRecordsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReferenceDataRecordDto>>([]);
}
