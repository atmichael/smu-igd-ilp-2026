using ILP.Shared.InboxMonitoring.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IInboxExtractionPersistence
{
    Task SaveAsync(IReadOnlyList<DocumentInboxMessageMappingDto> mappings, CancellationToken cancellationToken = default);
}
