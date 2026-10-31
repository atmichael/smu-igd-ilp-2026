using System.Net;
using System.Net.Http.Headers;
using ILP.Server.Features.InboxMonitoring.Service;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Dto;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Xunit;

namespace ILP.Server.Tests.InboxMonitoring;

public class InboxWatchEndpointTests : IClassFixture<EvidenceApiFactory>
{
    private sealed class StubSync : IInboxSyncService
    {
        public int Calls;
        public TaskCompletionSource Called { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<InboxSyncResultDto> SyncAsync(CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref Calls);
            Called.TrySetResult();
            return Task.FromResult(new InboxSyncResultDto());
        }
    }

    private readonly EvidenceApiFactory _factory;

    public InboxWatchEndpointTests(EvidenceApiFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Webhook_WithAnyBodyAndNoAuthentication_AcksAndTriggersSync()
    {
        var stub = new StubSync();
        using var factory = _factory.WithWebHostBuilder(b => b.ConfigureServices(s =>
        {
            s.RemoveAll<IInboxSyncService>();
            s.AddSingleton<IInboxSyncService>(stub);
        }));
        using var client = factory.CreateClient();

        var response = await client.PostAsync(
            "/api/webhook/inbox-watch",
            new StringContent("{\"message\":{\"data\":\"abc\"}}"),
            TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        await stub.Called.Task.WaitAsync(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        Assert.Equal(1, stub.Calls);
    }

    [Fact]
    public async Task NotificationStream_Authenticated_ReceivesNotificationsPublishedAfterConnecting()
    {
        var broker = _factory.Services.GetRequiredService<DocumentNotificationBroker>();
        broker.Publish(new DocumentNotificationDto("Invoice", "BEFORE", "UEN-0"));
        using var client = _factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Test");

        using var response = await client.GetAsync(
            "/api/notifications/stream", HttpCompletionOption.ResponseHeadersRead, TestContext.Current.CancellationToken);
        using var reader = new StreamReader(await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken));
        Assert.Equal(": connected", await reader.ReadLineAsync(TestContext.Current.CancellationToken));
        Assert.Equal("", await reader.ReadLineAsync(TestContext.Current.CancellationToken));

        broker.Publish(new DocumentNotificationDto("Invoice", "INV-1", "UEN-1"));
        var line = await reader.ReadLineAsync(TestContext.Current.CancellationToken);

        Assert.Equal(
            "data: {\"documentType\":\"Invoice\",\"documentReferenceNumber\":\"INV-1\",\"companyUen\":\"UEN-1\"}",
            line);
    }

    [Fact]
    public void Server_RegistersInboxWatchRenewalWorker()
    {
        using var client = _factory.CreateClient();

        Assert.Contains(_factory.Services.GetServices<IHostedService>(),
            h => h.GetType().Name == "InboxWatchRenewalWorker");
    }
}
