using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Endpoints;

public static class PolicySourceEndpoints
{
    public static IEndpointRouteBuilder MapPolicySourceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/reference-data/policy-sources/iras", () => Results.Ok(new PolicySourceDto(
            "IRAS GST invoicing guidance",
            "Inland Revenue Authority of Singapore",
            "https://www.iras.gov.sg/taxes/goods-services-tax-(gst)/basics-of-gst/invoicing-price-display-and-record-keeping/invoicing-customers",
            "NeedsReview", "Tax policy owner", null, null)));
        return app;
    }
}
