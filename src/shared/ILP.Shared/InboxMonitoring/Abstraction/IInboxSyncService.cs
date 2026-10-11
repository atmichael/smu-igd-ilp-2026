using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IInboxSyncService
{
    Task<InboxSyncResultDto> SyncAsync(CancellationToken cancellationToken = default);
}
