using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.AccountsPayable;
using ILP.Server.Features.TransactionDocuments.Configuration;
using ILP.Server.Features.TransactionDocuments.Model;
using ILP.Server.Features.TransactionDocuments.Repository;
using ILP.Shared.AccountsPayable.Abstraction;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace ILP.Server.Features.TransactionDocuments.Service;

public sealed class TransactionDocumentService(
    ITransactionDocumentRepository repository,
    IOptions<TransactionDocumentsOptions> options,
    IAccountsPayableCodingProcessor? accountsPayableCodingProcessor = null) : ITransactionDocumentService
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

    public async Task<DocumentSearchResponse> ExportAsync(
        DateOnly? createdFrom,
        DateOnly? createdTo,
        CancellationToken cancellationToken = default)
    {
        var documents = await repository.ExportAsync(
            createdFrom?.ToDateTime(TimeOnly.MinValue),
            createdTo?.AddDays(1).ToDateTime(TimeOnly.MinValue),
            cancellationToken);
        return new DocumentSearchResponse(documents.Select(MapDocument).ToArray(), documents.Count);
    }

    public async Task<TransactionDocumentRecord?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var document = await repository.GetByIdAsync(id, cancellationToken);
        return document is null ? null : MapDocument(document);
    }

    public async Task<TransactionDocumentRecord?> UpdateAsync(
        long id,
        UpdateTransactionDocumentRequest request,
        CancellationToken cancellationToken = default,
        string? overrideBy = null)
    {
        if (request.TypeCode is not null)
        {
            var existing = await repository.GetByIdAsync(id, cancellationToken);
            if (existing is null)
            {
                return null;
            }

            if (!AccountsPayableDocumentType.IsInvoiceOrCreditNote(existing.TypeCode)
                && AccountsPayableDocumentType.IsInvoiceOrCreditNote(request.TypeCode))
            {
                if (accountsPayableCodingProcessor is null)
                {
                    throw new InvalidOperationException("The accounts payable coding processor is not configured.");
                }

                var apJournal = await accountsPayableCodingProcessor.ProcessAsync(
                    CreateDocumentForCoding(existing, request),
                    cancellationToken);
                request = request with
                {
                    ApJournal = new UpdateTransactionDocumentAPJournal(
                        apJournal.GstCode,
                        apJournal.CoaSuggestions.Select(suggestion => new UpdateTransactionDocumentAPSuggestion(
                            suggestion.LineIndex,
                            suggestion.AccountCode,
                            suggestion.Reason,
                            suggestion.RequiresReview)).ToArray(),
                        apJournal.Confidence,
                        apJournal.VerificationNotes)
                };
            }
        }

        var updated = await repository.UpdateAsync(id, request, cancellationToken, overrideBy);
        return updated is null ? null : await GetByIdAsync(id, cancellationToken);
    }

    public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
        repository.SoftDeleteAsync(id, cancellationToken);

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
            new TransactionDocumentCompany(
                document.Company.Id,
                document.Company.Uen,
                document.Company.Name,
                document.Company.Email,
                document.Company.Address),
            document.SentDate,
            document.TotalAmount,
            document.Confidence,
            document.RelatedDocumentNumbers,
            document.Subtotal,
            document.SubtotalTaxRate,
            document.SubtotalTaxAmount,
            document.CreatedTime,
            document.DocumentItems.Where(item => item.ActiveStatus).Select(item => new TransactionDocumentItem(
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
                : document.InboxMessageMapping.SourceAttachment.WebViewLink,
            document.ManualOverrideFlag,
            document.ManualOverrideBy,
            document.DueDate,
            MapApJournal(document.ApJournal));

    private static DocumentDto CreateDocumentForCoding(
        DocumentEntity existing,
        UpdateTransactionDocumentRequest request)
    {
        var items = request.DocumentItems?.Select(item => new DocumentItemDto(
            item.Quantity,
            item.UnitPrice,
            item.Id,
            item.Description ?? string.Empty,
            item.CurrencyCode ?? "SGD",
            item.TaxName ?? "GST",
            item.TaxRate,
            item.SerialNumber,
            item.Amount,
            item.TaxAmount))
            ?? existing.DocumentItems.Where(item => item.ActiveStatus).Select(item => new DocumentItemDto(
                item.Quantity,
                item.UnitPrice,
                item.Id,
                item.Description,
                item.CurrencyCode,
                item.TaxName,
                item.TaxRate,
                item.SerialNumber,
                item.Amount,
                item.TaxAmount));
        var company = new CompanyDto(
            request.CompanyName?.Trim() ?? existing.Company.Name,
            request.CompanyUen?.Trim() ?? string.Empty,
            existing.Company.Address,
            existing.Company.TaxRegistrationNumber,
            existing.Company.Id,
            existing.Company.Email);

        return new DocumentDto(
            request.RefNumber?.Trim() ?? existing.RefNumber,
            request.TypeCode!.Trim(),
            request.SentDate,
            request.TotalAmount,
            items,
            existing.Id,
            request.RelatedDocumentNumbers,
            request.Subtotal,
            existing.SubtotalTaxRate,
            request.SubtotalTaxAmount,
            company,
            existing.Confidence,
            request.DueDate);
    }

    private static DocumentAPJournalDto? MapApJournal(ILP.Server.Features.AccountsPayable.Persistence.Entity.DocumentAPJournalEntity? apJournal)
    {
        if (apJournal is null || !apJournal.ActiveStatus)
        {
            return null;
        }

        IReadOnlyList<AccountsPayableLineSuggestion> suggestions;
        try
        {
            suggestions = JsonSerializer.Deserialize<List<AccountsPayableLineSuggestion>>(
                apJournal.CoaSuggestionsJson) ?? [];
        }
        catch (JsonException)
        {
            suggestions = [];
        }

        return new DocumentAPJournalDto(
            apJournal.Confidence,
            apJournal.GstCode,
            suggestions,
            apJournal.VerificationNotes.Split('|', StringSplitOptions.RemoveEmptyEntries));
    }
}
