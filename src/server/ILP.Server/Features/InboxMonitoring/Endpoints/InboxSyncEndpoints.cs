using ILP.Shared.InboxMonitoring.Abstraction;

namespace ILP.Server.Features.InboxMonitoring.Endpoints;

public static class InboxSyncEndpoints
{
    public const string AuthorizationPolicy = "InboxSyncPolicy";

    public static void MapInboxSyncEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/inbox/sync", async (IInboxSyncService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SyncAsync(cancellationToken)))
            .RequireAuthorization(AuthorizationPolicy);
    }
}
