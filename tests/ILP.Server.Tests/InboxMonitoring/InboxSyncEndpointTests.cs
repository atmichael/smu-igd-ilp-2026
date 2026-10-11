using System.Net;
using System.Net.Http.Headers;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Dto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ILP.Server.Tests.InboxMonitoring;

public class InboxSyncEndpointTests : IClassFixture<EvidenceApiFactory>
{
    private sealed class StubSync : IInboxSyncService
    {
        public int Calls;

        public Task<InboxSyncResultDto> SyncAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            return Task.FromResult(new InboxSyncResultDto { MessagesProcessed = 2 });
        }
    }

    private readonly StubSync _stub = new();
    private readonly EvidenceApiFactory _factory;

    public InboxSyncEndpointTests(EvidenceApiFactory factory)
    {
        _factory = factory;
    }

    private HttpClient CreateClient() => _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
    {
        s.RemoveAll<IInboxSyncService>();
        s.AddSingleton<IInboxSyncService>(_stub);
    })).CreateClient();

    [Fact]
    public async Task PostSync_WithoutAuthentication_IsUnauthorized()
    {
        var response = await CreateClient().PostAsync("/api/inbox/sync", null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(0, _stub.Calls);
    }

    [Fact]
    public async Task PostSync_Authenticated_TriggersInboxSync()
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        var response = await client.PostAsync("/api/inbox/sync", null);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("\"messagesProcessed\":2", await response.Content.ReadAsStringAsync());
        Assert.Equal(1, _stub.Calls);
    }

    [Fact]
    public void Server_RegistersInboxMonitorWorker()
    {
        using var client = _factory.CreateClient();
        var hosted = _factory.Services.GetServices<IHostedService>();

        Assert.Contains(hosted, h => h.GetType().Name == "InboxMonitorWorker");
    }
}
