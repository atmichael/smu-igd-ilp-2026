using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.TransactionDocuments.Repository;

public interface ITransactionDocumentRepository
{
    Task<(IReadOnlyList<DocumentEntity> Documents, int TotalCount)> SearchAsync(
        string? search,
        decimal? confidenceThreshold,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);

    Task<DocumentEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<bool> SoftDeleteAsync(long id, CancellationToken cancellationToken = default);
}
