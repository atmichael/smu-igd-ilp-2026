namespace ILP.Server.Features.TransactionDocuments.Model;

public sealed record DocumentSearchResponse(
    IReadOnlyList<TransactionDocumentRecord> Items,
    int TotalCount);

public sealed record TransactionDocumentRecord(
    long Id,
    string RefNumber,
    string TypeCode,
    TransactionDocumentCompany Company,
    DateTime SentDate,
    decimal TotalAmount,
    decimal? Confidence,
    string? RelatedDocumentNumbers,
    decimal Subtotal,
    decimal SubtotalTaxRate,
    decimal SubtotalTaxAmount,
    DateTime CreatedAt,
    IReadOnlyList<TransactionDocumentItem> DocumentItems,
    string? OriginalFileUrl = null);

public sealed record TransactionDocumentCompany(long Id, string? Uen, string Name);

public sealed record TransactionDocumentItem(
    long Id,
    string Description,
    decimal Quantity,
    string CurrencyCode,
    decimal UnitPrice,
    string TaxName,
    decimal TaxRate,
    int? SerialNumber,
    decimal? Amount,
    decimal? TaxAmount);
