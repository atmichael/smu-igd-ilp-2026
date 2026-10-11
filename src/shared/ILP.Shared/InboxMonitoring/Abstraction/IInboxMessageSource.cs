using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IInboxMessageSource
{
    Task<IReadOnlyList<InboxMessageDto>> FetchUnreadAsync(string traceId, CancellationToken cancellationToken);
}
