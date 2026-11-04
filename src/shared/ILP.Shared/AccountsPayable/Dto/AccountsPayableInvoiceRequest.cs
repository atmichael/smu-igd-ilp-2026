namespace ILP.Shared.AccountsPayable.Dto;

public sealed record AccountsPayableInvoiceRequest
{
    public string DocumentType { get; init; } = "";
    public string DocumentId { get; init; } = "";
    public string VendorName { get; init; } = "";
    public string? VendorIdentifier { get; init; }
    public string InvoiceNumber { get; init; } = "";
    public DateOnly? InvoiceDate { get; init; }
    public string Currency { get; init; } = "";
    public decimal? Subtotal { get; init; }
    public decimal? TaxAmount { get; init; }
    public decimal? TotalAmount { get; init; }
    public decimal? CapitalizationThreshold { get; init; }
    public string? CompanyPolicy { get; init; }
    public IReadOnlyList<AccountsPayableInvoiceLine> Lines { get; init; } = [];
    public IReadOnlyList<string> SourceEvidence { get; init; } = [];
    public bool? CompanyGstRegistered { get; init; }
    public bool? DomesticStandardRatedConfirmed { get; init; }
    public bool? InvoiceGstEvidenceValid { get; init; }
    public bool? InputGstClaimableConfirmed { get; init; }
    public bool? VendorWhitelisted { get; init; }
    public bool? UenMasterVerified { get; init; }
    public bool? VendorBankDetailsChanged { get; init; }
    public bool? DuplicateCheckPassed { get; init; }
    public string? RequiredDocumentsProfile { get; init; }
    public bool? SupportingDocumentsComplete { get; init; }
}

public sealed record AccountsPayableInvoiceLine(
    int LineIndex,
    string Description,
    decimal? NetAmount,
    decimal? TaxAmount,
    string? EvidenceReference,
    decimal? Quantity = null,
    decimal? UnitPrice = null,
    string? PurchaseOrderCode = null,
    string? ProjectCode = null);
