using ILP.Server.Features.CompaniesManagement.Model;
using ILP.Server.Features.CompaniesManagement.Repository;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.CompaniesManagement.Service;

public sealed class CompanyService(ICompanyRepository repository) : ICompanyService
{
    public async Task<CompanySearchResponse> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        var (companies, totalCount) = await repository.SearchAsync(
            search, page, pageSize, sortBy, sortDirection, cancellationToken);
        return new CompanySearchResponse(companies.Select(MapSummary).ToArray(), totalCount);
    }

    public async Task<CompanyDetails?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        var company = await repository.GetByIdAsync(id, cancellationToken);
        return company is null ? null : MapDetails(company);
    }

    private static CompanySummary MapSummary(CompanyEntity company) =>
        new(company.Id, company.Uen, company.Name, GetTotalOutstanding(company));

    private static CompanyDetails MapDetails(CompanyEntity company) =>
        new(
            company.Id,
            company.Uen,
            company.Name,
            company.Address,
            company.TaxRegistrationNumber,
            GetTotalOutstanding(company),
            company.Documents
                .Where(document => document.ActiveStatus)
                .Select(document => new CompanyDocumentSummary(
                    document.Id,
                    document.RefNumber,
                    document.SentDate,
                    document.TotalAmount,
                    document.CreatedTime))
                .ToArray());

    private static decimal GetTotalOutstanding(CompanyEntity company) =>
        company.Documents
            .Where(document => document.ActiveStatus)
            .Sum(document => document.TotalAmount);
}
