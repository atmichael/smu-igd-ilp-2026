using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Adapters;

public sealed class ErpVendorSourceAdapter : IReferenceDataSourceAdapter
{
    public string Source => "ERP vendor master";
    public bool SupportsUpdateNotifications => false;

    public Task<IReadOnlyList<ReferenceDataRecordDto>> ReadRecordsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ReferenceDataRecordDto>>([]);
}
