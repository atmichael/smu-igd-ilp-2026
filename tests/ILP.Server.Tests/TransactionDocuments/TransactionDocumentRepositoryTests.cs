using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.TransactionDocuments.Configuration;
using ILP.Server.Features.TransactionDocuments.Model;
using ILP.Server.Features.TransactionDocuments.Repository;
using ILP.Server.Features.TransactionDocuments.Service;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Xunit;

namespace ILP.Server.Tests.TransactionDocuments;

public sealed class TransactionDocumentRepositoryTests
{
    [Fact]
    public async Task SearchAsync_FiltersByConfidenceIncludingThreshold_AndPaginatesInRequestedOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var company = CreateCompany();
        var manuallyVerified = CreateDocument("INV-VERIFIED", 0.2m, 5m, company);
        manuallyVerified.ManualOverrideFlag = true;
        manuallyVerified.ManualOverrideBy = "reviewer";
        database.Context.Documents.AddRange(
            CreateDocument("INV-1", 0.4m, 30m, company),
            CreateDocument("INV-2", 0.5m, 20m, company),
            CreateDocument("INV-3", 0.6m, 10m, company),
            manuallyVerified);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var repository = new SqliteTransactionDocumentRepository(database.Factory);
        var service = new TransactionDocumentService(
            repository,
            Options.Create(new TransactionDocumentsOptions { ConfidenceThreshold = 0.5m }));
        var belowThreshold = await service.GetBelowThresholdAsync(
            1, 10, "totalAmount", "asc", TestContext.Current.CancellationToken);
        var (documents, totalCount) = await repository.SearchAsync(
            search: null,
            confidenceThreshold: 0.5m,
            page: 1,
            pageSize: 1,
            sortBy: "totalAmount",
            sortDirection: "asc",
            cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(["INV-2", "INV-1"], belowThreshold.Items.Select(document => document.RefNumber));
        Assert.Equal(2, belowThreshold.TotalCount);
        Assert.Equal(2, totalCount);
        Assert.Equal("INV-2", Assert.Single(documents).RefNumber);
    }

    [Fact]
    public async Task UpdateAsync_StoresManualOverrideOnceAndRetainsConfidence()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = CreateDocument("INV-1", 0.42m, 10m);
        database.Context.Documents.Add(document);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TransactionDocumentService(
            new SqliteTransactionDocumentRepository(database.Factory),
            Options.Create(new TransactionDocumentsOptions { ConfidenceThreshold = 0.5m }));
        var request = new UpdateTransactionDocumentRequest(
            "INV-1", document.SentDate, document.Company.Uen, document.Company.Name, 0m, 0m, 10m, null, [],
            ManualOverrideFlag: true);

        var updated = await service.UpdateAsync(
            document.Id, request, TestContext.Current.CancellationToken, "reviewer");
        var repeated = await service.UpdateAsync(
            document.Id, request, TestContext.Current.CancellationToken, "another-reviewer");
        var belowThreshold = await service.GetBelowThresholdAsync(
            1, 10, "sentDate", "desc", TestContext.Current.CancellationToken);

        Assert.True(updated!.ManualOverrideFlag);
        Assert.Equal("reviewer", updated.ManualOverrideBy);
        Assert.Equal(0.42m, updated.Confidence);
        Assert.True(repeated!.ManualOverrideFlag);
        Assert.Equal("reviewer", repeated.ManualOverrideBy);
        Assert.Empty(belowThreshold.Items);
    }

    [Fact]
    public async Task SearchAsync_SearchesReferenceCompanyNameAndUen()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Documents.Add(CreateDocument("INV-ABC", 0.8m, 10m));
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new SqliteTransactionDocumentRepository(database.Factory);

        var (byReference, _) = await repository.SearchAsync(
            "abc", null, 1, 20, "refNumber", "asc", TestContext.Current.CancellationToken);
        var (byCompanyName, _) = await repository.SearchAsync(
            "supplier", null, 1, 20, "refNumber", "asc", TestContext.Current.CancellationToken);
        var (byUen, _) = await repository.SearchAsync(
            "uen-1", null, 1, 20, "refNumber", "asc", TestContext.Current.CancellationToken);
        var (noMatches, noMatchCount) = await repository.SearchAsync(
            "missing", null, 1, 20, "refNumber", "asc", TestContext.Current.CancellationToken);

        Assert.Single(byReference);
        Assert.Single(byCompanyName);
        Assert.Single(byUen);
        Assert.Empty(noMatches);
        Assert.Equal(0, noMatchCount);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsOnlyActiveDocuments()
    {
        await using var database = await TestDatabase.CreateAsync();
        var company = CreateCompany();
        var active = CreateDocument("INV-1", 0.8m, 10m, company);
        var inactive = CreateDocument("INV-2", 0.8m, 10m, company);
        inactive.ActiveStatus = false;
        database.Context.Documents.AddRange(active, inactive);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new SqliteTransactionDocumentRepository(database.Factory);
        var service = new TransactionDocumentService(
            repository,
            Options.Create(new TransactionDocumentsOptions()));

        Assert.NotNull(await repository.GetByIdAsync(active.Id, TestContext.Current.CancellationToken));
        Assert.Null(await repository.GetByIdAsync(inactive.Id, TestContext.Current.CancellationToken));
        Assert.Null(await repository.GetByIdAsync(long.MaxValue, TestContext.Current.CancellationToken));
        Assert.Null(await service.GetByIdAsync(long.MaxValue, TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsDriveUrlOfSourceAttachmentAsOriginalFileUrl()
    {
        await using var database = await TestDatabase.CreateAsync();
        var company = CreateCompany();
        var document = CreateDocument("INV-1", 0.8m, 10m, company);
        var withoutAttachment = CreateDocument("INV-2", 0.8m, 10m, company);
        var message = new InboxMessageEntity { UniqueId = 1, Subject = "s", Sender = "a@b.c", Body = "b" };
        var attachment = new InboxMessageAttachmentEntity
        {
            Message = message,
            FileName = "inv.pdf",
            DriveFileId = "drive-1",
            WebViewLink = "https://drive.google.com/file/d/drive-1/view"
        };
        document.InboxMessageMapping = new DocumentInboxMessageEntity { Message = message, SourceAttachment = attachment };
        database.Context.Documents.AddRange(document, withoutAttachment);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TransactionDocumentService(
            new SqliteTransactionDocumentRepository(database.Factory),
            Options.Create(new TransactionDocumentsOptions()));

        var result = await service.GetByIdAsync(document.Id, TestContext.Current.CancellationToken);
        var none = await service.GetByIdAsync(withoutAttachment.Id, TestContext.Current.CancellationToken);

        Assert.Equal("https://drive.google.com/file/d/drive-1/view", result!.OriginalFileUrl);
        Assert.Null(none!.OriginalFileUrl);
    }

    [Fact]
    public async Task DeleteAsync_SoftDeletesDocumentAndRelatedRecords_AndHidesThemFromQueries()
    {
        await using var database = await TestDatabase.CreateAsync();
        var document = CreateDocument("INV-1", 0.8m, 10m);
        var other = CreateDocument("INV-2", 0.8m, 10m);
        document.DocumentItems.Add(new DocumentItemEntity
        {
            Description = "Item",
            CreatedTime = new DateTime(2026, 10, 2),
            ModifiedTime = new DateTime(2026, 10, 2)
        });
        var message = new InboxMessageEntity { UniqueId = 1, Subject = "s", Sender = "a@b.c", Body = "b" };
        document.InboxMessageMapping = new DocumentInboxMessageEntity { Message = message };
        database.Context.Documents.AddRange(document, other);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TransactionDocumentService(
            new SqliteTransactionDocumentRepository(database.Factory),
            Options.Create(new TransactionDocumentsOptions()));

        var first = await service.DeleteAsync(document.Id, TestContext.Current.CancellationToken);
        var second = await service.DeleteAsync(document.Id, TestContext.Current.CancellationToken);
        var missing = await service.DeleteAsync(long.MaxValue, TestContext.Current.CancellationToken);

        Assert.True(first);
        Assert.False(second);
        Assert.False(missing);
        await using var verify = await database.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        Assert.False((await verify.Documents.SingleAsync(d => d.Id == document.Id, TestContext.Current.CancellationToken)).ActiveStatus);
        Assert.All(verify.DocumentItems.Where(i => i.DocumentId == document.Id), i => Assert.False(i.ActiveStatus));
        Assert.All(verify.Set<DocumentInboxMessageEntity>().Where(m => m.DocumentId == document.Id), m => Assert.False(m.ActiveStatus));
        Assert.True((await verify.Documents.SingleAsync(d => d.Id == other.Id, TestContext.Current.CancellationToken)).ActiveStatus);
        Assert.Null(await service.GetByIdAsync(document.Id, TestContext.Current.CancellationToken));
        var search = await service.SearchAsync(null, 1, 10, "refNumber", "asc", TestContext.Current.CancellationToken);
        Assert.Equal(["INV-2"], search.Items.Select(d => d.RefNumber));
    }

    [Fact]
    public async Task UpdateAsync_PersistsChanges_ReusesExistingCompanyByUen_AndUpsertsLineItems()
    {
        await using var database = await TestDatabase.CreateAsync();
        var other = new CompanyEntity { Uen = "UEN-2", Name = "Existing Co", CreatedTime = new DateTime(2026, 10, 1), ModifiedTime = new DateTime(2026, 10, 1) };
        var document = CreateDocument("INV-1", 0.8m, 10m);
        var kept = NewItem("Kept", 1m, 5m);
        var removed = NewItem("Removed", 1m, 5m);
        document.DocumentItems.Add(kept);
        document.DocumentItems.Add(removed);
        database.Context.Companies.Add(other);
        database.Context.Documents.Add(document);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TransactionDocumentService(
            new SqliteTransactionDocumentRepository(database.Factory),
            Options.Create(new TransactionDocumentsOptions()));

        var result = await service.UpdateAsync(
            document.Id,
            new UpdateTransactionDocumentRequest(
                "INV-1A", new DateTime(2026, 11, 2), "UEN-2", "Ignored Name", 30m, 3m, 33m, "PO-1",
                [
                    new UpdateTransactionDocumentItem(kept.Id, "Kept edited", 2m, "SGD", 10m, "GST", 9m, 1, 20m, null),
                    new UpdateTransactionDocumentItem(null, "Added", 1m, "SGD", 10m, "GST", 9m, 2, 10m, null)
                ]),
            TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal("INV-1A", result.RefNumber);
        Assert.Equal(other.Id, result.Company.Id);
        Assert.Equal("Existing Co", result.Company.Name);
        Assert.Equal(["Added", "Kept edited"], result.DocumentItems.Select(item => item.Description).Order());
        await using var verify = await database.Factory.CreateDbContextAsync(TestContext.Current.CancellationToken);
        var saved = await verify.Documents.SingleAsync(d => d.Id == document.Id, TestContext.Current.CancellationToken);
        Assert.Equal(33m, saved.TotalAmount);
        Assert.Equal(3m, saved.SubtotalTaxAmount);
        Assert.Equal("PO-1", saved.RelatedDocumentNumbers);
        Assert.Equal(new DateTime(2026, 11, 2), saved.SentDate);
        Assert.False((await verify.DocumentItems.SingleAsync(i => i.Id == removed.Id, TestContext.Current.CancellationToken)).ActiveStatus);
        Assert.Equal(2, await verify.DocumentItems.CountAsync(i => i.DocumentId == document.Id && i.ActiveStatus, TestContext.Current.CancellationToken));
        Assert.Equal(2, await verify.Companies.CountAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task UpdateAsync_CreatesCompanyWhenUenDoesNotExist_AndReturnsNullForMissingOrInactiveDocument()
    {
        await using var database = await TestDatabase.CreateAsync();
        var company = CreateCompany();
        var document = CreateDocument("INV-1", 0.8m, 10m, company);
        var inactive = CreateDocument("INV-2", 0.8m, 10m, company);
        inactive.ActiveStatus = false;
        database.Context.Documents.AddRange(document, inactive);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new TransactionDocumentService(
            new SqliteTransactionDocumentRepository(database.Factory),
            Options.Create(new TransactionDocumentsOptions()));
        var request = new UpdateTransactionDocumentRequest(
            "INV-1", new DateTime(2026, 10, 1), "NEW-UEN", "New Co", 0m, 0m, 0m, null, []);

        var result = await service.UpdateAsync(document.Id, request, TestContext.Current.CancellationToken);

        Assert.Equal("NEW-UEN", result!.Company.Uen);
        Assert.Equal("New Co", result.Company.Name);
        Assert.Null(await service.UpdateAsync(inactive.Id, request, TestContext.Current.CancellationToken));
        Assert.Null(await service.UpdateAsync(long.MaxValue, request, TestContext.Current.CancellationToken));
    }

    [Fact]
    public void Validate_RequiresTallyingSubtotalAndTotal_AndRequiredFields()
    {
        var item = new UpdateTransactionDocumentItem(null, "Item", 2m, "SGD", 5m, "GST", 9m, 1, null, null);
        var valid = new UpdateTransactionDocumentRequest("INV-1", new DateTime(2026, 10, 1), "UEN", null, 10m, 1m, 11m, null, [item]);

        Assert.Empty(TransactionDocumentUpdateValidator.Validate(valid));
        Assert.Contains("subtotal", TransactionDocumentUpdateValidator.Validate(valid with { Subtotal = 9m }).Keys);
        Assert.Contains("totalAmount", TransactionDocumentUpdateValidator.Validate(valid with { TotalAmount = 12m }).Keys);
        Assert.Contains("refNumber", TransactionDocumentUpdateValidator.Validate(valid with { RefNumber = " " }).Keys);
        Assert.Contains("companyName", TransactionDocumentUpdateValidator.Validate(valid with { CompanyUen = null }).Keys);
        Assert.NotEmpty(TransactionDocumentUpdateValidator.Validate(null));
    }

    private static DocumentItemEntity NewItem(string description, decimal quantity, decimal unitPrice) => new()
    {
        Description = description,
        Quantity = quantity,
        UnitPrice = unitPrice,
        CreatedTime = new DateTime(2026, 10, 2),
        ModifiedTime = new DateTime(2026, 10, 2)
    };

    private static CompanyEntity CreateCompany() => new() { Uen = "UEN-1", Name = "Supplier Ltd" };

    private static DocumentEntity CreateDocument(
        string reference,
        decimal? confidence,
        decimal totalAmount,
        CompanyEntity? company = null) => new()
    {
        RefNumber = reference,
        TypeCode = "INV",
        Company = company ?? CreateCompany(),
        SentDate = new DateTime(2026, 10, 1),
        TotalAmount = totalAmount,
        Confidence = confidence,
        CreatedTime = new DateTime(2026, 10, 2),
        ModifiedTime = new DateTime(2026, 10, 2)
    };

    private sealed class TestDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection _connection;

        private TestDatabase(SqliteConnection connection, InboxExtractionDbContext context)
        {
            _connection = connection;
            Context = context;
            Factory = new TestContextFactory(context);
        }

        public InboxExtractionDbContext Context { get; }

        public IDbContextFactory<InboxExtractionDbContext> Factory { get; }

        public static async Task<TestDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var options = new DbContextOptionsBuilder<InboxExtractionDbContext>()
                .UseSqlite(connection)
                .Options;
            var context = new InboxExtractionDbContext(options);
            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            return new TestDatabase(connection, context);
        }

        public async ValueTask DisposeAsync()
        {
            await Context.DisposeAsync();
            await _connection.DisposeAsync();
        }
    }

    private sealed class TestContextFactory(InboxExtractionDbContext context)
        : IDbContextFactory<InboxExtractionDbContext>
    {
        public InboxExtractionDbContext CreateDbContext() => new(context.Database.GetDbConnection() is SqliteConnection connection
            ? new DbContextOptionsBuilder<InboxExtractionDbContext>().UseSqlite(connection).Options
            : throw new InvalidOperationException("Expected a SQLite connection."));

        public Task<InboxExtractionDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
