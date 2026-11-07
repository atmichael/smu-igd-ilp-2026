using ILP.Server.Features.AccountsPayable.Service;
using ILP.Shared.AccountsPayable.Dto;
using Microsoft.AspNetCore.Mvc;

namespace ILP.Server.Features.AccountsPayable.Entrypoint;

public static class AccountsPayableEndpoints
{
    public const string AuthorizationPolicy = "AccountsPayablePolicy";

    public static void MapAccountsPayableEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/accounts-payable/reference-data", async (
            IAccountsPayableService service,
            CancellationToken cancellationToken) =>
            Results.Ok(await service.GetReferenceDataAsync(cancellationToken)))
            .RequireAuthorization(AuthorizationPolicy);

        app.MapGet("/api/accounts-payable/coding/{documentId:long}", async (
            long documentId,
            IAccountsPayableService service,
            CancellationToken cancellationToken) =>
        {
            if (documentId <= 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid document ID.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var coding = await service.GetCodingAsync(documentId, cancellationToken);
            return coding is null ? Results.NotFound() : Results.Ok(coding);
        })
        .RequireAuthorization(AuthorizationPolicy);

        app.MapPost("/api/accounts-payable/coding", async (
            AccountsPayableInvoiceRequest? request,
            IAccountsPayableService service,
            CancellationToken cancellationToken) =>
        {
            if (request is null)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invoice coding input is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return Results.Ok(await service.EvaluateAsync(request, cancellationToken));
        })
        .RequireAuthorization(AuthorizationPolicy);
    }
}
