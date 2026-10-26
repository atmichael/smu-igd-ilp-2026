using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.CompaniesManagement.Repository;

public interface ICompanyRepository
{
    Task<(IReadOnlyList<CompanyEntity> Companies, int TotalCount)> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);

    Task<CompanyEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default);

    Task<CompanyEntity?> GetByUenAsync(string uen, CancellationToken cancellationToken = default);
}
