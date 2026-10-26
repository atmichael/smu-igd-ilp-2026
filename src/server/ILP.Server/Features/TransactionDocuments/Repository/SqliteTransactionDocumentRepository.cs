using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.TransactionDocuments.Model;
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

    public async Task<DocumentEntity?> UpdateAsync(
        long id,
        UpdateTransactionDocumentRequest request,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var document = await context.Documents
            .Where(candidate => candidate.ActiveStatus && candidate.Id == id)
            .Include(candidate => candidate.DocumentItems)
            .SingleOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        var now = DateTime.UtcNow;
        var company = await FindOrCreateCompanyAsync(context, request, now, cancellationToken);
        document.Company = company;
        document.RefNumber = request.RefNumber!.Trim();
        document.SentDate = request.SentDate;
        document.Subtotal = request.Subtotal;
        document.SubtotalTaxAmount = request.SubtotalTaxAmount;
        document.TotalAmount = request.TotalAmount;
        document.RelatedDocumentNumbers = string.IsNullOrWhiteSpace(request.RelatedDocumentNumbers)
            ? null
            : request.RelatedDocumentNumbers.Trim();
        document.Version++;
        document.ModifiedTime = now;

        var requestedItems = request.DocumentItems ?? [];
        var existingItems = document.DocumentItems
            .Where(item => item.ActiveStatus)
            .ToDictionary(item => item.Id);
        var keptItemIds = new HashSet<long>();
        foreach (var requested in requestedItems)
        {
            if (requested.Id is { } itemId && existingItems.TryGetValue(itemId, out var existing) && keptItemIds.Add(itemId))
            {
                ApplyItem(existing, requested);
                existing.Version++;
                existing.ModifiedTime = now;
            }
            else
            {
                var created = new DocumentItemEntity { CreatedTime = now, ModifiedTime = now };
                ApplyItem(created, requested);
                document.DocumentItems.Add(created);
            }
        }

        foreach (var removed in existingItems.Values.Where(item => !keptItemIds.Contains(item.Id)))
        {
            Deactivate(removed, now);
        }

        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return document;
    }

    private static async Task<CompanyEntity> FindOrCreateCompanyAsync(
        InboxExtractionDbContext context,
        UpdateTransactionDocumentRequest request,
        DateTime now,
        CancellationToken cancellationToken)
    {
        var uen = string.IsNullOrWhiteSpace(request.CompanyUen) ? null : request.CompanyUen.Trim();
        var name = request.CompanyName?.Trim() ?? "";
        CompanyEntity? company;
        if (uen is not null)
        {
            company = await context.Companies.SingleOrDefaultAsync(candidate => candidate.Uen == uen, cancellationToken);
        }
        else
        {
            var lowerName = name.ToLower();
            company = await context.Companies.FirstOrDefaultAsync(
                candidate => candidate.ActiveStatus && candidate.Uen == null && candidate.Name.ToLower() == lowerName,
                cancellationToken);
        }

        if (company is null)
        {
            company = new CompanyEntity { Uen = uen, Name = name, CreatedTime = now, ModifiedTime = now };
            context.Companies.Add(company);
        }
        else if (!company.ActiveStatus)
        {
            company.ActiveStatus = true;
            company.Version++;
            company.ModifiedTime = now;
        }

        return company;
    }

    private static void ApplyItem(DocumentItemEntity entity, UpdateTransactionDocumentItem item)
    {
        entity.Description = item.Description!.Trim();
        entity.Quantity = item.Quantity;
        entity.CurrencyCode = string.IsNullOrWhiteSpace(item.CurrencyCode) ? "SGD" : item.CurrencyCode.Trim();
        entity.UnitPrice = item.UnitPrice;
        entity.TaxName = string.IsNullOrWhiteSpace(item.TaxName) ? "GST" : item.TaxName.Trim();
        entity.TaxRate = item.TaxRate;
        entity.SerialNumber = item.SerialNumber;
        entity.Amount = item.Amount;
        entity.TaxAmount = item.TaxAmount;
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
