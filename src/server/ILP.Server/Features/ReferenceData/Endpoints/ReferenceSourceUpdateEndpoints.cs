using ILP.Server.Features.ReferenceData.Adapters;
using ILP.Server.Features.ReferenceData.Service;

namespace ILP.Server.Features.ReferenceData.Endpoints;

public static class ReferenceSourceUpdateEndpoints
{
    public static IEndpointRouteBuilder MapReferenceSourceUpdateEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reference-data/sources/{source}/update", async (string source, IEnumerable<IReferenceDataSourceAdapter> adapters, ReferenceDataSynchronizationService service, CancellationToken cancellationToken) =>
        {
            var adapter = adapters.FirstOrDefault(x => string.Equals(x.Source, source, StringComparison.OrdinalIgnoreCase));
            if (adapter is null) return Results.NotFound();
            if (!adapter.SupportsUpdateNotifications) return Results.Accepted(null, new { reconciliationRequired = true });
            return Results.Ok(await service.SynchronizeAsync(source, "Source update notification", cancellationToken));
        });
        return app;
    }
}
