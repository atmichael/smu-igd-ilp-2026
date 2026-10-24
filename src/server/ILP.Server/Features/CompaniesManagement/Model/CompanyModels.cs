namespace ILP.Server.Features.CompaniesManagement.Model;

public sealed record CompanySearchResponse(
    IReadOnlyList<CompanySummary> Items,
    int TotalCount);

public sealed record CompanySummary(
    long Id,
    string? Uen,
    string Name,
    decimal TotalOutstandingSgd);

public sealed record CompanyDetails(
    long Id,
    string? Uen,
    string Name,
    string Address,
    string TaxRegistrationNumber,
    decimal TotalOutstandingSgd,
    IReadOnlyList<CompanyDocumentSummary> Documents);

public sealed record CompanyDocumentSummary(
    long Id,
    string RefNumber,
    DateTime SentDate,
    decimal TotalAmount,
    DateTime CreatedAt);
