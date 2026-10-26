using ILP.Server.Features.TransactionDocuments.Model;

namespace ILP.Server.Features.TransactionDocuments.Service;

public interface ITransactionDocumentService
{
    Task<DocumentSearchResponse> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);

    Task<DocumentSearchResponse> GetBelowThresholdAsync(
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);

    Task<TransactionDocumentRecord?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<TransactionDocumentRecord?> UpdateAsync(
        long id,
        UpdateTransactionDocumentRequest request,
        CancellationToken cancellationToken = default);

    Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default);
}
