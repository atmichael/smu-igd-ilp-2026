using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Adapters;

public sealed class ErpPurchasingSourceAdapter : IReferenceDataSourceAdapter
{
    public string Source => "ERP purchasing records";
    public bool SupportsUpdateNotifications => false;

    public Task<IReadOnlyList<ReferenceDataRecordDto>> ReadRecordsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReferenceDataRecordDto>>([]);
}
