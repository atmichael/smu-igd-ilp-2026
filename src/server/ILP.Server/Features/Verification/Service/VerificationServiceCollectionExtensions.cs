using ILP.Server.Features.Verification.Persistence;
using ILP.Server.Features.Verification.Review;

namespace ILP.Server.Features.Verification.Service;

public static class VerificationServiceCollectionExtensions
{
    public const string AuthorizationPolicy = "VerificationPolicy";

    public static IServiceCollection AddVerification(this IServiceCollection services)
    {
        services.AddSingleton<IVerificationRepository, VerificationRepository>();
        services.AddScoped<VerificationOrchestrator>();
        services.AddScoped<VerificationReviewService>();
        return services;
    }
}
