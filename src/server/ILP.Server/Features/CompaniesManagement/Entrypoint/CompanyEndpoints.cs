using ILP.Server.Features.CompaniesManagement.Service;
using Microsoft.AspNetCore.Mvc;

namespace ILP.Server.Features.CompaniesManagement.Entrypoint;

public static class CompanyEndpoints
{
    public const string AuthorizationPolicy = "CompaniesReadPolicy";
    private const int DefaultPageSize = 20;
    private const int MaximumPageSize = 100;

    private static readonly HashSet<string> SortFields = new(StringComparer.OrdinalIgnoreCase)
    {
        "uen",
        "name",
        "totalOutstandingSgd"
    };

    public static void MapCompanyEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/companies", async (
            ICompanyService service,
            CancellationToken cancellationToken,
            string? search = null,
            int page = 1,
            int pageSize = DefaultPageSize,
            string? sortBy = "name",
            string? sortDirection = "asc") =>
        {
            var validation = ValidateQuery(page, pageSize, sortBy, sortDirection);
            if (validation is not null)
            {
                return validation;
            }

            return Results.Ok(await service.SearchAsync(
                search, page, pageSize, sortBy ?? "name", sortDirection ?? "asc", cancellationToken));
        })
        .RequireAuthorization(AuthorizationPolicy);

        app.MapGet("/api/companies/{id:long}", async (
            long id,
            ICompanyService service,
            CancellationToken cancellationToken) =>
        {
            if (id <= 0)
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid company ID.",
                    Detail = "The company ID must be a positive integer.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var company = await service.GetByIdAsync(id, cancellationToken);
            return company is null ? Results.NotFound() : Results.Ok(company);
        })
        .RequireAuthorization(AuthorizationPolicy);

        app.MapGet("/api/companies/by-uen/{uen}", async (
            string uen,
            ICompanyService service,
            CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(uen))
            {
                return Results.BadRequest(new ProblemDetails
                {
                    Title = "Invalid company UEN.",
                    Detail = "The company UEN is required.",
                    Status = StatusCodes.Status400BadRequest
                });
            }

            var company = await service.GetByUenAsync(uen.Trim(), cancellationToken);
            return company is null ? Results.NotFound() : Results.Ok(company);
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
