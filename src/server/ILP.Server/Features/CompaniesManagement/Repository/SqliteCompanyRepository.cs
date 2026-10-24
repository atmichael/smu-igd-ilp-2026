using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using Microsoft.EntityFrameworkCore;

namespace ILP.Server.Features.CompaniesManagement.Repository;

public sealed class SqliteCompanyRepository(
    IDbContextFactory<InboxExtractionDbContext> contextFactory) : ICompanyRepository
{
    public async Task<(IReadOnlyList<CompanyEntity> Companies, int TotalCount)> SearchAsync(
        string? search,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<CompanyEntity> companies = context.Companies
            .Where(company => company.ActiveStatus);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            companies = companies.Where(company =>
                company.Name.ToLower().Contains(searchTerm)
                || (company.Uen != null && company.Uen.ToLower().Contains(searchTerm)));
        }

        var totalCount = await companies.CountAsync(cancellationToken);
        var results = await ApplyOrdering(companies, sortBy, sortDirection)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Include(company => company.Documents.Where(document => document.ActiveStatus))
            .ToListAsync(cancellationToken);

        return (results, totalCount);
    }

    public async Task<CompanyEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Companies
            .Where(company => company.ActiveStatus && company.Id == id)
            .Include(company => company.Documents
                .Where(document => document.ActiveStatus)
                .OrderByDescending(document => document.CreatedTime)
                .ThenByDescending(document => document.Id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    private static IOrderedQueryable<CompanyEntity> ApplyOrdering(
        IQueryable<CompanyEntity> companies,
        string sortBy,
        string sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("uen", false) => companies.OrderBy(company => company.Uen).ThenBy(company => company.Id),
            ("uen", true) => companies.OrderByDescending(company => company.Uen).ThenBy(company => company.Id),
            ("totaloutstandingsgd", false) => companies
                .OrderBy(company => company.Documents.Where(document => document.ActiveStatus)
                    .Sum(document => (decimal?)document.TotalAmount) ?? 0m)
                .ThenBy(company => company.Id),
            ("totaloutstandingsgd", true) => companies
                .OrderByDescending(company => company.Documents.Where(document => document.ActiveStatus)
                    .Sum(document => (decimal?)document.TotalAmount) ?? 0m)
                .ThenBy(company => company.Id),
            ("name", true) => companies.OrderByDescending(company => company.Name).ThenBy(company => company.Id),
            _ => companies.OrderBy(company => company.Name).ThenBy(company => company.Id)
        };
    }
}
