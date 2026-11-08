using ILP.Server.Features.ReferenceData.Repository;
using ILP.Server.Features.ReferenceData.Service;

namespace ILP.Server.Features.ReferenceData.Endpoints;

public static class ReferenceGovernanceEndpoints
{
    public static IEndpointRouteBuilder MapReferenceGovernanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reference-data/sources/{source}/refresh", async (string source, string? trigger, ReferenceDataSynchronizationService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.SynchronizeAsync(source, trigger ?? "Manual", cancellationToken)));

        app.MapGet("/api/reference-data/sources/{source}/runs", (string source, IReferenceDataRepository repository) => Results.Ok(repository.GetRuns(source)));
        app.MapPost("/api/reference-data/rule-changes", (ReferenceChangeRequest request, ReferenceChangeApprovalService approvalService) => Results.Ok(approvalService.Submit(request)));

        app.MapPost("/api/reference-data/rule-changes/{changeId}/approve", (string changeId, ReferenceChangeApprovalDto request, ReferenceChangeApprovalService approvalService) =>
        {
            var decision = new ReferenceChangeDecision(changeId, "PendingApproval", null, null);
            return Results.Ok(approvalService.Approve(decision, request.Approver, request.Reason));
        });
        return app;
    }

    public sealed record ReferenceChangeApprovalDto(string Approver, string Reason);
}
