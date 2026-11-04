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
    IReadOnlyList<UpdateTransactionDocumentItem>? DocumentItems,
    bool? ManualOverrideFlag = null,
    DateTime? DueDate = null,
    UpdateTransactionDocumentAPJournal? ApJournal = null);

public sealed record UpdateTransactionDocumentAPJournal(
    string? GstCode,
    IReadOnlyList<UpdateTransactionDocumentAPSuggestion>? CoaSuggestions);

public sealed record UpdateTransactionDocumentAPSuggestion(
    int LineIndex,
    string? AccountCode,
    string? Reason);

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
