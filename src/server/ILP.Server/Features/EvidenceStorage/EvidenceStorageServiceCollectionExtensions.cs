namespace ILP.Server.Features.EvidenceStorage;

public static class EvidenceStorageServiceCollectionExtensions
{
    public const string AuthorizationPolicy = "EvidenceStoragePolicy";
    public const string ConnectionStringName = "IlpDatabase";

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

        services.AddSingleton<IDocumentContentStore>(provider =>
        {
            var options = configuration.GetSection(EvidenceStorageOptions.SectionName).Get<EvidenceStorageOptions>() ?? new EvidenceStorageOptions();
            if (string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                return new InMemoryDocumentContentStore();
            }

            if (string.Equals(options.ContentProvider, "MySql", StringComparison.OrdinalIgnoreCase))
            {
                var connectionString = configuration.GetConnectionString(ConnectionStringName);
                if (string.IsNullOrWhiteSpace(connectionString))
                {
                    throw new InvalidOperationException(
                        $"ConnectionStrings:{ConnectionStringName} is required when EvidenceStorage:ContentProvider is MySql.");
                }

                return new MySqlDocumentContentStore(connectionString);
            }

            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            return new FileDocumentContentStore(Path.Combine(environment.ContentRootPath, options.ContentRootPath));
        });

        services.AddSingleton<ISourceDocumentRepository>(provider =>
        {
            var options = configuration.GetSection(EvidenceStorageOptions.SectionName).Get<EvidenceStorageOptions>() ?? new EvidenceStorageOptions();
            if (string.Equals(options.Provider, "InMemory", StringComparison.OrdinalIgnoreCase))
            {
                return new InMemorySourceDocumentRepository();
            }

            var environment = provider.GetRequiredService<IWebHostEnvironment>();
            return new FileSourceDocumentRepository(Path.Combine(environment.ContentRootPath, options.SourceDocumentRecordsPath));
        });

        services.AddSingleton<AuditTrailService>();
        services.AddSingleton<RecordChangeService>();
        services.AddSingleton<MatchOutcomeLinkService>();
        services.AddSingleton<RetentionRules>();
        services.AddSingleton<EvidencePackageService>();

        return services;
    }
}
