namespace ILP.Server.Features.EvidenceStorage;

public static class EvidenceStorageServiceCollectionExtensions
{
    public const string AuthorizationPolicy = "EvidenceStoragePolicy";

    public static IServiceCollection AddEvidenceStorage(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<EvidenceStorageOptions>(configuration.GetSection(EvidenceStorageOptions.SectionName));
        services.AddSingleton(TimeProvider.System);

        services.AddSingleton<IEvidenceRepository>(provider =>
        {
            var options = configuration.GetSection(EvidenceStorageOptions.SectionName).Get<EvidenceStorageOptions>() ?? new EvidenceStorageOptions();
            if (string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                return new InMemoryEvidenceRepository();
            }

            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            return new FileEvidenceRepository(Path.Combine(environment.ContentRootPath, options.RootPath));
        });

        services.AddSingleton<AuditTrailService>();
        services.AddSingleton<RecordChangeService>();
        services.AddSingleton<MatchOutcomeLinkService>();
        services.AddSingleton<RetentionRules>();
        services.AddSingleton<EvidencePackageService>();

        return services;
    }
}
