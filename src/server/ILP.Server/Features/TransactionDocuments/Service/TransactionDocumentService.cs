using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.TransactionDocuments.Configuration;
using ILP.Server.Features.TransactionDocuments.Model;
using ILP.Server.Features.TransactionDocuments.Repository;
using Microsoft.Extensions.Options;

namespace ILP.Server.Features.TransactionDocuments.Service;

public sealed class TransactionDocumentService(
    ITransactionDocumentRepository repository,
    IOptions<TransactionDocumentsOptions> options) : ITransactionDocumentService
{
    public Task<DocumentSearchResponse> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default) =>
        SearchCoreAsync(search, null, page, pageSize, sortBy, sortDirection, cancellationToken);

    public Task<DocumentSearchResponse> GetBelowThresholdAsync(
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default) =>
        SearchCoreAsync(null, options.Value.ConfidenceThreshold, page, pageSize, sortBy, sortDirection, cancellationToken);

    public async Task<TransactionDocumentRecord?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken);
        return document is null ? null : MapDocument(document);
    }

    private async Task<DocumentSearchResponse> SearchCoreAsync(
        string? search,
        decimal? confidenceThreshold,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken)
    {
        var (documents, totalCount) = await repository.SearchAsync(
            search, confidenceThreshold, page, pageSize, sortBy, sortDirection, cancellationToken);
        return new DocumentSearchResponse(documents.Select(MapDocument).ToArray(), totalCount);
    }

    private static TransactionDocumentRecord MapDocument(DocumentEntity document) =>
        new(
            document.Id,
            document.RefNumber,
            document.TypeCode,
            new TransactionDocumentCompany(document.Company.Id, document.Company.Uen, document.Company.Name),
            document.SentDate,
            document.TotalAmount,
            document.Confidence,
            document.RelatedDocumentNumbers,
            document.Subtotal,
            document.SubtotalTaxRate,
            document.SubtotalTaxAmount,
            document.CreatedTime,
            document.DocumentItems.Select(item => new TransactionDocumentItem(
                item.Id,
                item.Description,
                item.Quantity,
                item.CurrencyCode,
                item.UnitPrice,
                item.TaxName,
                item.TaxRate,
                item.SerialNumber,
                item.Amount,
                item.TaxAmount)).ToArray(),
            string.IsNullOrWhiteSpace(document.InboxMessageMapping?.SourceAttachment?.WebViewLink)
                ? null
                : document.InboxMessageMapping.SourceAttachment.WebViewLink);
}
