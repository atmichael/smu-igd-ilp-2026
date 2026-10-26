namespace ILP.Server.Features.TransactionDocuments.Model;

public sealed record UpdateTransactionDocumentRequest(
    string? RefNumber,
    DateTime SentDate,
    string? CompanyUen,
    string? CompanyName,
    decimal Subtotal,
    decimal SubtotalTaxAmount,
    decimal TotalAmount,
    string? RelatedDocumentNumbers,
    IReadOnlyList<UpdateTransactionDocumentItem>? DocumentItems);

public sealed record UpdateTransactionDocumentItem(
    long? Id,
    string? Description,
    decimal Quantity,
    string? CurrencyCode,
    decimal UnitPrice,
    string? TaxName,
    decimal TaxRate,
    int? SerialNumber,
    decimal? Amount,
    decimal? TaxAmount);
