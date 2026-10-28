using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.TransactionDocument.Dto;

namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IInboxExtractionPersistence
{
    Task SaveAsync(IReadOnlyList<DocumentInboxMessageMappingDto> mappings, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(DocumentDto document, CancellationToken cancellationToken = default);
}
