using ILP.Server.Features.ReferenceData.Repository;
using ILP.Server.Features.ReferenceData.Service;
using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Endpoints;

public static class ReferenceDataEndpoints
{
    public static IEndpointRouteBuilder MapReferenceDataEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reference-data/sources/{source}/snapshot", (string source, IReferenceDataRepository repository) =>
            repository.GetCurrentSnapshot(source) is { } snapshot ? Results.Ok(snapshot) : Results.NotFound());

        app.MapGet("/api/reference-data/sources/{source}/references/{category}/{identity}",
            (string source, string category, string identity, DateOnly effectiveDate, ReferenceDataLookupService lookup) =>
                Results.Ok(lookup.Lookup(source, category, identity, effectiveDate)));

        app.MapGet("/api/reference-data/traces/{traceId}", (string traceId, IReferenceDataRepository repository) =>
            repository.GetTrace(traceId) is { } trace ? Results.Ok(trace) : Results.NotFound());
        return app;
    }
}
