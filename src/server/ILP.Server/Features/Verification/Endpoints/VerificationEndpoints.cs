using ILP.Server.Features.TransactionDocuments.Repository;
using ILP.Server.Features.Verification.Persistence;
using ILP.Server.Features.Verification.Service;
using ILP.Shared.Verification.Contracts;
using System.Security.Claims;

namespace ILP.Server.Features.Verification.Endpoints;

public static class VerificationEndpoints
{
    public static RouteGroupBuilder MapVerificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").RequireAuthorization(VerificationServiceCollectionExtensions.AuthorizationPolicy);

        group.MapPost("/verification-runs", async (StartVerificationRequest request, ITransactionDocumentRepository documents,
            VerificationOrchestrator orchestrator, CancellationToken cancellationToken) =>
        {
            if (!long.TryParse(request.DocumentId, out var documentId)) return Results.BadRequest(new { message = "Document identifier must be numeric for verification MVP." });
            var document = await documents.GetByIdAsync(documentId, cancellationToken);
            return document is null ? Results.NotFound(new { message = "Document was not found." }) : Results.Ok(orchestrator.Verify(document, request.EvidencePackageId, request.OriginalInvoiceReferenceId));
        });

        group.MapGet("/verification-runs/{verificationRunId}", (string verificationRunId, IVerificationRepository repository) =>
        {
            var run = repository.GetRun(verificationRunId);
            return run is null ? Results.NotFound() : Results.Ok(run);
        });

        group.MapPost("/verification-review-items/{reviewItemId}/resolve", (string reviewItemId, ResolveReviewItemRequest request,
            ClaimsPrincipal user, IVerificationRepository repository) =>
        {
            if (string.IsNullOrWhiteSpace(request.ResolutionReason)) return Results.BadRequest(new { message = "Resolution reason is required." });
            var userId = user.Identity?.Name;
            if (string.IsNullOrWhiteSpace(userId)) return Results.Unauthorized();
            repository.Resolve(reviewItemId, userId, request.ResolutionReason.Trim());
            return Results.NoContent();
        });

        group.MapGet("/verification-runs/{verificationRunId}/checks/{verificationCheckId}/calculation-trace", (string verificationRunId, string verificationCheckId, IVerificationRepository repository) =>
        {
            var run = repository.GetRun(verificationRunId);
            var check = run?.Checks.SingleOrDefault(candidate => candidate.VerificationCheckId == verificationCheckId);
            return check?.CalculationTrace is null ? Results.NotFound() : Results.Ok(check.CalculationTrace);
        });

        return group;
    }
}
