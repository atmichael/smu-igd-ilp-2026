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
        CancellationToken cancellationToken = default);

    Task<CompanyDetails?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CompanyDetails?> GetByUenAsync(string uen, CancellationToken cancellationToken = default);
}
