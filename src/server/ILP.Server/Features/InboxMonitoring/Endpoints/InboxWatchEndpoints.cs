using System.Text.Json;
using ILP.Server.Features.InboxMonitoring.Service;

namespace ILP.Server.Features.InboxMonitoring.Endpoints;

public static class InboxWatchEndpoints
{
    public const string NotificationAuthorizationPolicy = "DocumentNotificationPolicy";

    public static void MapInboxWatchEndpoints(this IEndpointRouteBuilder app)
    {
        // Called by Google Pub/Sub push; the body is intentionally ignored and any 2xx response is an ACK.
        app.MapPost("/api/webhook/inbox-watch", (InboxSyncSignal signal, ILogger<InboxSyncSignal> logger) =>
        {
            logger.LogDebug("Received Pub/Sub webhook call on inbox-watch endpoint");
            signal.Signal();
            return Results.NoContent();
        }).AllowAnonymous();

        app.MapGet("/api/notifications/stream", async (
            HttpContext context,
            DocumentNotificationBroker broker,
            CancellationToken cancellationToken) =>
        {
            using var subscription = broker.Subscribe();
            context.Response.Headers.ContentType = "text/event-stream";
            context.Response.Headers.CacheControl = "no-cache";
            await context.Response.WriteAsync(": connected\n\n", cancellationToken);
            await context.Response.Body.FlushAsync(cancellationToken);

            try
            {
                await foreach (var notification in subscription.ReadAllAsync(cancellationToken))
                {
                    var json = JsonSerializer.Serialize(notification, JsonSerializerOptions.Web);
                    await context.Response.WriteAsync($"data: {json}\n\n", cancellationToken);
                    await context.Response.Body.FlushAsync(cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
            }
        }).RequireAuthorization(NotificationAuthorizationPolicy);
    }
}
