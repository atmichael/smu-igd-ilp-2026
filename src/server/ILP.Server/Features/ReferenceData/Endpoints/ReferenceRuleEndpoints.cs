using ILP.Server.Features.ReferenceData.Service;
using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Endpoints;

public static class ReferenceRuleEndpoints
{
    public static IEndpointRouteBuilder MapReferenceRuleEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/reference-data/rules/select", (RuleSelectionRequest request, DocumentRuleSelector selector) =>
        {
            var reason = selector.ValidateVariant(request.DocumentVariant);
            if (!string.IsNullOrEmpty(reason)) return Results.BadRequest(new { reasonCode = reason, recoveryGuidance = ReferenceDataOutcomeReason.RecoveryGuidance(reason) });
            return Results.Ok(new { selectedDocumentVariant = request.DocumentVariant });
        });
        return app;
    }

    public sealed record RuleSelectionRequest(string Jurisdiction, string DocumentVariant, DateOnly EffectiveDate);
}
