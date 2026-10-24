using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.TransactionDocuments.Configuration;
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
        database.Context.Documents.AddRange(
            CreateDocument("INV-1", 0.4m, 30m, company),
            CreateDocument("INV-2", 0.5m, 20m, company),
            CreateDocument("INV-3", 0.6m, 10m, company));
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
