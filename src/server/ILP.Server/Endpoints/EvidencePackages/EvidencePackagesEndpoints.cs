using System.Security.Claims;
using ILP.Server.Features.EvidenceStorage;
using ILP.Shared.Evidence;
using Microsoft.AspNetCore.Mvc;

namespace ILP.Server.Endpoints.EvidencePackages;

public static class EvidencePackagesEndpoints
{
    public static void MapEvidencePackagesEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/evidence-packages")
            .RequireAuthorization(EvidenceStorageServiceCollectionExtensions.AuthorizationPolicy);

        group.MapPost("", (CreateEvidencePackageRequest request, EvidencePackageService service, ClaimsPrincipal user) =>
            Execute(() =>
            {
                var package = service.Create(request, ActorId(user));
                return Results.Created($"/api/evidence-packages/{package.EvidencePackageId}", package);
            }));

        group.MapGet("/{evidencePackageId}", (string evidencePackageId, EvidencePackageService service) =>
            Execute(() => Results.Ok(service.Get(evidencePackageId))));

        group.MapGet("", (string? caseId, string? documentId, string? sourceReference, string? reviewStatus, EvidencePackageService service) =>
            Execute(() => Results.Ok(service.Query(caseId, documentId, sourceReference, reviewStatus))));

        group.MapPost("/{evidencePackageId}/finalize", (string evidencePackageId, EvidencePackageService service, ClaimsPrincipal user) =>
            Execute(() => Results.Ok(service.Finalize(evidencePackageId, ActorId(user)))));

        group.MapPost("/{evidencePackageId}/status", (string evidencePackageId, StatusChangeRequest request, EvidencePackageService service, ClaimsPrincipal user) =>
            Execute(() => Results.Ok(service.ChangeStatus(evidencePackageId, request, ActorId(user)))));

        group.MapPost("/{evidencePackageId}/records/{recordId}/corrections",
            (string evidencePackageId, string recordId, RecordCorrectionRequest request, EvidencePackageService service, ClaimsPrincipal user) =>
                Execute(() => Results.Ok(service.CorrectRecord(evidencePackageId, recordId, request, ActorId(user)))));

        group.MapPost("/{evidencePackageId}/records/{recordId}/verifications",
            (string evidencePackageId, string recordId, RecordVerificationRequest request, EvidencePackageService service, ClaimsPrincipal user) =>
                Execute(() => Results.Ok(service.VerifyRecord(evidencePackageId, recordId, request, ActorId(user)))));

        group.MapPost("/{evidencePackageId}/match-outcomes", (string evidencePackageId, MatchOutcomeRequest request, EvidencePackageService service, ClaimsPrincipal user) =>
            Execute(() => Results.Ok(service.LinkMatchOutcome(evidencePackageId, request, ActorId(user)))));

        group.MapPost("/{evidencePackageId}/archive", (string evidencePackageId, EvidencePackageService service, ClaimsPrincipal user) =>
            Execute(() => Results.Ok(service.Archive(evidencePackageId, ActorId(user)))));
    }

    private static string? ActorId(ClaimsPrincipal user) => user.Identity?.Name;

    private static IResult Execute(Func<IResult> action)
    {
        try
        {
            return action();
        }
        catch (EvidenceValidationException ex)
        {
            return Results.ValidationProblem(ex.Errors, title: "Invalid evidence package.", statusCode: ex.StatusCode);
        }
        catch (EvidenceNotFoundException ex)
        {
            return Problem(StatusCodes.Status404NotFound, "Evidence not found.", ex.Message);
        }
        catch (EvidenceDuplicateException ex)
        {
            return Problem(StatusCodes.Status409Conflict, "Duplicate final evidence.", ex.Message);
        }
        catch (EvidencePreconditionException ex)
        {
            return Problem(StatusCodes.Status412PreconditionFailed, "Evidence state does not allow this operation.", ex.Message);
        }
        catch (EvidencePersistenceException ex)
        {
            return Results.Problem(
                title: "Evidence package save failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError,
                extensions: new Dictionary<string, object?>
                {
                    ["evidencePackageId"] = ex.EvidencePackageId,
                    ["reviewStatus"] = ex.VisibleStatus
                });
        }
    }

    private static IResult Problem(int statusCode, string title, string detail) =>
        Results.Problem(new ProblemDetails { Status = statusCode, Title = title, Detail = detail });
}
