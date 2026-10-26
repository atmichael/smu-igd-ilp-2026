using System.Net;
using System.Net.Http.Headers;
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
            Task.FromResult<TransactionDocumentRecord?>(null);

        public Task<bool> DeleteAsync(long id, CancellationToken cancellationToken = default) =>
            Task.FromResult(id == 7);
    }
}
