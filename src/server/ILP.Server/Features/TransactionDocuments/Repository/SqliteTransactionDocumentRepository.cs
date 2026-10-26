using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using Microsoft.EntityFrameworkCore;

namespace ILP.Server.Features.TransactionDocuments.Repository;

public sealed class SqliteTransactionDocumentRepository(
    IDbContextFactory<InboxExtractionDbContext> contextFactory) : ITransactionDocumentRepository
{
    public async Task<(IReadOnlyList<DocumentEntity> Documents, int TotalCount)> SearchAsync(
        string? search,
        decimal? confidenceThreshold,
        int page,
        int pageSize,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<DocumentEntity> documents = context.Documents
            .Where(document => document.ActiveStatus)
            .Include(document => document.Company)
            .Include(document => document.DocumentItems);

        if (!string.IsNullOrWhiteSpace(search))
        {
            var searchTerm = search.Trim().ToLowerInvariant();
            documents = documents.Where(document =>
                document.RefNumber.ToLower().Contains(searchTerm)
                || document.TypeCode.ToLower().Contains(searchTerm)
                || document.Company.Name.ToLower().Contains(searchTerm)
                || (document.Company.Uen != null && document.Company.Uen.ToLower().Contains(searchTerm)));
        }

        if (confidenceThreshold.HasValue)
        {
            documents = documents.Where(document => document.Confidence <= confidenceThreshold);
        }

        var totalCount = await documents.CountAsync(cancellationToken);
        var ordered = ApplyOrdering(documents, sortBy, sortDirection);
        var results = await ordered
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (results, totalCount);
    }

    public async Task<DocumentEntity?> GetByIdAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Documents
            .Where(document => document.ActiveStatus && document.Id == id)
            .Include(document => document.Company)
            .Include(document => document.DocumentItems)
            .Include(document => document.InboxMessageMapping)
                .ThenInclude(mapping => mapping!.SourceAttachment)
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task<bool> SoftDeleteAsync(long id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var document = await context.Documents
            .Where(candidate => candidate.ActiveStatus && candidate.Id == id)
            .Include(candidate => candidate.DocumentItems)
            .Include(candidate => candidate.InboxMessageMapping)
            .SingleOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return false;
        }

        var now = DateTime.UtcNow;
        Deactivate(document, now);
        foreach (var item in document.DocumentItems.Where(item => item.ActiveStatus))
        {
            Deactivate(item, now);
        }

        if (document.InboxMessageMapping is { ActiveStatus: true } mapping)
        {
            Deactivate(mapping, now);
        }

        await context.SaveChangesAsync(cancellationToken);
        return true;
    }

    private static void Deactivate(AuditedEntity entity, DateTime now)
    {
        entity.ActiveStatus = false;
        entity.Version++;
        entity.ModifiedTime = now;
    }

    private static IOrderedQueryable<DocumentEntity> ApplyOrdering(
        IQueryable<DocumentEntity> documents,
        string sortBy,
        string sortDirection)
    {
        var descending = string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase);
        return (sortBy.ToLowerInvariant(), descending) switch
        {
            ("refnumber", false) => documents.OrderBy(document => document.RefNumber).ThenBy(document => document.Id),
            ("refnumber", true) => documents.OrderByDescending(document => document.RefNumber).ThenBy(document => document.Id),
            ("typecode", false) => documents.OrderBy(document => document.TypeCode).ThenBy(document => document.Id),
            ("typecode", true) => documents.OrderByDescending(document => document.TypeCode).ThenBy(document => document.Id),
            ("totalamount", false) => documents.OrderBy(document => document.TotalAmount).ThenBy(document => document.Id),
            ("totalamount", true) => documents.OrderByDescending(document => document.TotalAmount).ThenBy(document => document.Id),
            ("confidence", false) => documents.OrderBy(document => document.Confidence).ThenBy(document => document.Id),
            ("confidence", true) => documents.OrderByDescending(document => document.Confidence).ThenBy(document => document.Id),
            ("createdat", false) => documents.OrderBy(document => document.CreatedTime).ThenBy(document => document.Id),
            ("createdat", true) => documents.OrderByDescending(document => document.CreatedTime).ThenBy(document => document.Id),
            ("companyname", false) => documents.OrderBy(document => document.Company.Name).ThenBy(document => document.Id),
            ("companyname", true) => documents.OrderByDescending(document => document.Company.Name).ThenBy(document => document.Id),
            ("sentdate", false) => documents.OrderBy(document => document.SentDate).ThenBy(document => document.Id),
            _ => documents.OrderByDescending(document => document.SentDate).ThenBy(document => document.Id)
        };
    }
}
