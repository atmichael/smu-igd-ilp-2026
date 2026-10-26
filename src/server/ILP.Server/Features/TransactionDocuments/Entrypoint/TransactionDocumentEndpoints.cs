using ILP.Server.Features.TransactionDocuments.Service;
using Microsoft.AspNetCore.Mvc;

namespace ILP.Server.Features.TransactionDocuments.Entrypoint;

public static class TransactionDocumentEndpoints
{
    public const string AuthorizationPolicy = "TransactionDocumentsReadPolicy";
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "refNumber",
        "typeCode",
        "sentDate",
        "totalAmount",
        "confidence",
        "createdAt",
        "companyName"
    };

    public static void MapTransactionDocumentEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/documents", async (
            ITransactionDocumentService service,
            CancellationToken cancellationToken,
            string? search = null,
            int page = 1,
            int pageSize = DefaultPageSize,
            string? sortBy = "sentDate",
            string? sortDirection = "desc") =>
        {
            var validation = ValidateQuery(page, pageSize, sortBy, sortDirection);
            if (validation is not null)
            {
                return validation;
            }

            return Results.Ok(await service.SearchAsync(
                search, page, pageSize, sortBy ?? "sentDate", sortDirection ?? "desc", cancellationToken));
        })
        .RequireAuthorization(AuthorizationPolicy);

        app.MapGet("/api/documents/below-threshold", async (
            ITransactionDocumentService service,
            CancellationToken cancellationToken,
            int page = 1,
            int pageSize = DefaultPageSize,
            string? sortBy = "sentDate",
            string? sortDirection = "desc") =>
        {
            var validation = ValidateQuery(page, pageSize, sortBy, sortDirection);
            if (validation is not null)
            {
                return validation;
            }

            return Results.Ok(await service.GetBelowThresholdAsync(
                page, pageSize, sortBy ?? "sentDate", sortDirection ?? "desc", cancellationToken));
        })
        .RequireAuthorization(AuthorizationPolicy);

        app.MapGet("/api/documents/{id:long}", async (
            long id,
            ITransactionDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid document ID.",
                    Detail = "The document ID must be a positive integer.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var document = await service.GetByIdAsync(id, cancellationToken);
            return document is null ? Results.NotFound() : Results.Ok(document);
        })
        .RequireAuthorization(AuthorizationPolicy);

        app.MapDelete("/api/documents/{id:long}", async (
            long id,
            ITransactionDocumentService service,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid document ID.",
                    Detail = "The document ID must be a positive integer.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            return await service.DeleteAsync(id, cancellationToken)
                ? Results.NoContent()
                : Results.NotFound(new ProblemDetails
                {
                    Title = "Document not found.",
                    Detail = "The document does not exist or has already been deleted.",
                    Status = StatusCodes.Status404NotFound
                });
        })
        .RequireAuthorization(AuthorizationPolicy);
    }

    private static IResult? ValidateQuery(int page, int pageSize, string? sortBy, string? sortDirection)
    {
        if (page < 1 || pageSize is < 1 or > MaximumPageSize || (long)(page - 1) * pageSize > int.MaxValue)
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid pagination parameters.",
                Detail = $"Page must be positive and pageSize must be between 1 and {MaximumPageSize}.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (sortBy is not null && !SortFields.Contains(sortBy))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid sort field.",
                Detail = "The sortBy parameter is not supported.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        if (sortDirection is not null
            && !string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(sortDirection, "desc", StringComparison.OrdinalIgnoreCase))
        {
            return Results.BadRequest(new ProblemDetails
            {
                Title = "Invalid sort direction.",
                Detail = "The sortDirection parameter must be asc or desc.",
                Status = StatusCodes.Status400BadRequest
            });
        }

        return null;
    }
}
