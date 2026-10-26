using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using ILP.Server.Features.TransactionDocuments.Entrypoint;
using ILP.Server.Features.TransactionDocuments.Model;
using ILP.Server.Features.TransactionDocuments.Service;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace ILP.Server.Tests.TransactionDocuments;

public sealed class TransactionDocumentEndpointTests
{
    [Fact]
    public async Task GetById_ReturnsBadRequestForInvalidIdAndNotFoundForMissingDocument()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(TransactionDocumentEndpoints.AuthorizationPolicy, policy =>
                policy.RequireAuthenticatedUser());
        });
        builder.Services.AddSingleton<ITransactionDocumentService, MissingDocumentService>();

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTransactionDocumentEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);

        var invalidId = await client.GetAsync("/api/documents/0", TestContext.Current.CancellationToken);
        var missingId = await client.GetAsync("/api/documents/42", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingId.StatusCode);
    }

    [Fact]
    public async Task Delete_ReturnsBadRequestNotFoundAndNoContent()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(TransactionDocumentEndpoints.AuthorizationPolicy, policy =>
                policy.RequireAuthenticatedUser());
        });
        builder.Services.AddSingleton<ITransactionDocumentService, MissingDocumentService>();

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTransactionDocumentEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);

        var invalidId = await client.DeleteAsync("/api/documents/0", TestContext.Current.CancellationToken);
        var missingId = await client.DeleteAsync("/api/documents/42", TestContext.Current.CancellationToken);
        var deleted = await client.DeleteAsync("/api/documents/7", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingId.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleted.StatusCode);
    }

    [Fact]
    public async Task Patch_ReturnsBadRequestNotFoundValidationProblemAndOk()
    {
        var builder = WebApplication.CreateBuilder();
        builder.WebHost.UseTestServer();
        builder.Services.AddAuthentication(TestAuthHandler.SchemeName)
            .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, _ => { });
        builder.Services.AddAuthorization(options =>
        {
            options.AddPolicy(TransactionDocumentEndpoints.AuthorizationPolicy, policy =>
                policy.RequireAuthenticatedUser());
        });
        builder.Services.AddSingleton<ITransactionDocumentService, MissingDocumentService>();

        await using var app = builder.Build();
        app.UseAuthentication();
        app.UseAuthorization();
        app.MapTransactionDocumentEndpoints();
        await app.StartAsync(TestContext.Current.CancellationToken);

        using var client = app.GetTestClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName);
        var valid = JsonContent.Create(new
        {
            refNumber = "INV-1",
            sentDate = "2026-10-01",
            companyUen = "UEN-1",
            companyName = "Supplier",
            subtotal = 10,
            subtotalTaxAmount = 1,
            totalAmount = 11,
            documentItems = new[] { new { description = "Item", quantity = 1, unitPrice = 10, amount = 10 } }
        });
        var mismatched = JsonContent.Create(new
        {
            refNumber = "INV-1",
            sentDate = "2026-10-01",
            companyUen = "UEN-1",
            subtotal = 10,
            subtotalTaxAmount = 1,
            totalAmount = 99,
            documentItems = new[] { new { description = "Item", quantity = 1, unitPrice = 10, amount = 10 } }
        });

        var invalidId = await client.PatchAsync("/api/documents/0", valid, TestContext.Current.CancellationToken);
        var missingId = await client.PatchAsync("/api/documents/42", valid, TestContext.Current.CancellationToken);
        var invalidBody = await client.PatchAsync("/api/documents/7", mismatched, TestContext.Current.CancellationToken);
        var updated = await client.PatchAsync("/api/documents/7", valid, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.BadRequest, invalidId.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, missingId.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidBody.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
    }

    private sealed class MissingDocumentService : ITransactionDocumentService
    {
        public Task<DocumentSearchResponse> SearchAsync(
            string? search,
            int page,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentSearchResponse([], 0));

        public Task<DocumentSearchResponse> GetBelowThresholdAsync(
            int page,
            int pageSize,
            string sortBy,
            string sortDirection,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new DocumentSearchResponse([], 0));

        public Task<TransactionDocumentRecord?> GetByIdAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDocumentRecord?>(id == 7 ? Record(id) : null);

        public Task<TransactionDocumentRecord?> UpdateAsync(
            long id,
            UpdateTransactionDocumentRequest request,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<TransactionDocumentRecord?>(id == 7 ? Record(id) : null);

        private static TransactionDocumentRecord Record(long id) =>
            new(id, "INV-1", "INV", new TransactionDocumentCompany(1, "UEN-1", "Supplier"),
                new DateTime(2026, 10, 1), 11m, null, null, 10m, 0m, 1m, new DateTime(2026, 10, 2), []);

        public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == 7);
    }
}
