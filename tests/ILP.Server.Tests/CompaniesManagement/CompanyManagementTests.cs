using System.Net;
using System.Net.Http.Headers;
using ILP.Server.Features.CompaniesManagement.Entrypoint;
using ILP.Server.Features.CompaniesManagement.Model;
using ILP.Server.Features.CompaniesManagement.Repository;
using ILP.Server.Features.CompaniesManagement.Service;
using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ILP.Server.Tests.CompaniesManagement;

public sealed class CompanyManagementTests
{
    [Fact]
    public async Task SearchAsync_FiltersByPartialNameOrUenAndReturnsEmptyForNoMatches()
    {
        await using var database = await TestDatabase.CreateAsync();
        database.Context.Companies.AddRange(
            CreateCompany("Alpha Pte Ltd", "UEN-1"),
            CreateCompany("Beta Pte Ltd", "UEN-ABC"));
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var repository = new SqliteCompanyRepository(database.Factory);

        var byName = await repository.SearchAsync(
            "alpha", 1, 20, "name", "asc", TestContext.Current.CancellationToken);
        var byUen = await repository.SearchAsync(
            "abc", 1, 20, "name", "asc", TestContext.Current.CancellationToken);
        var noMatches = await repository.SearchAsync(
            "missing", 1, 20, "name", "asc", TestContext.Current.CancellationToken);

        Assert.Equal("Alpha Pte Ltd", Assert.Single(byName.Companies).Name);
        Assert.Equal(1, byName.TotalCount);
        Assert.Equal("Beta Pte Ltd", Assert.Single(byUen.Companies).Name);
        Assert.Empty(noMatches.Companies);
        Assert.Equal(0, noMatches.TotalCount);
    }

    [Fact]
    public async Task SearchAsync_SortsAndPaginatesByOutstandingAmountFromActiveDocuments()
    {
        await using var database = await TestDatabase.CreateAsync();
        var alpha = CreateCompany("Alpha", "UEN-1");
        var beta = CreateCompany("Beta", "UEN-2");
        var gamma = CreateCompany("Gamma", "UEN-3");
        database.Context.Companies.AddRange(alpha, beta, gamma);
        database.Context.Documents.AddRange(
            CreateDocument(alpha, "A-1", 5m, new DateTime(2026, 10, 1)),
            CreateDocument(alpha, "A-2", 6m, new DateTime(2026, 10, 2)),
            CreateDocument(beta, "B-1", 20m, new DateTime(2026, 10, 3)),
            CreateDocument(gamma, "G-1", 100m, new DateTime(2026, 10, 4), active: false));
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new CompanyService(new SqliteCompanyRepository(database.Factory));

        var result = await service.SearchAsync(
            null, 1, 2, "totalOutstandingSgd", "desc", TestContext.Current.CancellationToken);

        Assert.Equal(3, result.TotalCount);
        Assert.Equal(["Beta", "Alpha"], result.Items.Select(company => company.Name));
        Assert.Equal(20m, result.Items[0].TotalOutstandingSgd);
        Assert.Equal(11m, result.Items[1].TotalOutstandingSgd);
    }

    [Fact]
    public async Task GetByIdAsync_ReturnsCompanyAndActiveDocumentsInRecentOrder()
    {
        await using var database = await TestDatabase.CreateAsync();
        var company = CreateCompany("Alpha", "UEN-1");
        var older = CreateDocument(company, "INV-1", 10m, new DateTime(2026, 10, 1));
        var newer = CreateDocument(company, "INV-2", 20m, new DateTime(2026, 10, 2));
        var inactive = CreateDocument(company, "INV-3", 100m, new DateTime(2026, 10, 3), active: false);
        database.Context.Companies.Add(company);
        database.Context.Documents.AddRange(older, newer, inactive);
        await database.Context.SaveChangesAsync(TestContext.Current.CancellationToken);
        var service = new CompanyService(new SqliteCompanyRepository(database.Factory));

        var result = await service.GetByIdAsync(company.Id, TestContext.Current.CancellationToken);

        Assert.NotNull(result);
        Assert.Equal(30m, result.TotalOutstandingSgd);
        Assert.Equal(["INV-2", "INV-1"], result.Documents.Select(document => document.RefNumber));
    }

    [Fact]
    public async Task GetById_ReturnsBadRequestForInvalidIdAndNotFoundForMissingCompany()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(CompanyEndpoints.AuthorizationPolicy, policy => policy.RequireAuthenticatedUser());
        });
        var service = new MissingCompanyService();
        builder.Services.AddSingleton<ICompanyService>(service);

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapCompanyEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);

        var invalidId = await client.GetAsync("/api/companies/0", TestContext.Current.CancellationToken);
        var missingId = await client.GetAsync("/api/companies/42", TestContext.Current.CancellationToken);
        var search = await client.GetAsync(
            "/api/companies?search=acme&page=2&pageSize=5&sortBy=name&sortDirection=desc",
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingId.StatusCode);
        Assert.Equal(HttpStatusCode.OK, search.StatusCode);
        Assert.Equal("acme", service.Search);
        Assert.Equal(2, service.Page);
        Assert.Equal(5, service.PageSize);
        Assert.Equal("name", service.SortBy);
        Assert.Equal("desc", service.SortDirection);
    }

    private static CompanyEntity CreateCompany(string name, string uen) => new()
    {
        Name = name,
        Uen = uen,
        CreatedTime = new DateTime(2026, 10, 1),
        ModifiedTime = new DateTime(2026, 10, 1)
    };

    private static DocumentEntity CreateDocument(
        CompanyEntity company,
        string reference,
        decimal amount,
        DateTime createdAt,
        bool active = true) => new()
    {
        Company = company,
        RefNumber = reference,
        TypeCode = "INV",
        SentDate = createdAt,
        TotalAmount = amount,
        CreatedTime = createdAt,
        ModifiedTime = createdAt,
        ActiveStatus = active
    };

    private sealed class MissingCompanyService : ICompanyService
    {
        public string? Search { get; private set; }
        public int Page { get; private set; }
        public int PageSize { get; private set; }
        public string? SortBy { get; private set; }
        public string? SortDirection { get; private set; }

        public Task<CompanySearchResponse> SearchAsync(
            string? search,
            int page,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default)
        {
            Search = search;
            Page = page;
            PageSize = pageSize;
            SortBy = sortBy;
            SortDirection = sortDirection;
            return Task.FromResult(new CompanySearchResponse([], 0));
        }

        public Task<CompanyDetails?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<CompanyDetails?>(null);
    }

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
        public InboxExtractionDbContext CreateDbContext() => new(
            new DbContextOptionsBuilder<InboxExtractionDbContext>()
                .UseSqlite((SqliteConnection)context.Database.GetDbConnection())
                .Options);

        public Task<InboxExtractionDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(CreateDbContext());
    }
}
