using ILP.Server.Features.CompaniesManagement.Model;

namespace ILP.Server.Features.CompaniesManagement.Service;

public interface ICompanyService
{
    Task<CompanySearchResponse> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default,
        string? uen = null);

    Task<CompanyDetails?> GetByIdAsync(long id, CancellationToken cancellationToken = default);
}
