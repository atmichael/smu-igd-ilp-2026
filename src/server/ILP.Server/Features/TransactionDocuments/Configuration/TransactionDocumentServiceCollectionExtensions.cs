using ILP.Server.Features.TransactionDocuments.Repository;
using ILP.Server.Features.TransactionDocuments.Service;

namespace ILP.Server.Features.TransactionDocuments.Configuration;

public static class TransactionDocumentServiceCollectionExtensions
{
    public static IServiceCollection AddTransactionDocuments(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddOptions<TransactionDocumentsOptions>()
            .Bind(configuration.GetSection(TransactionDocumentsOptions.SectionName))
            .Validate(options => options.ConfidenceThreshold is >= 0m and <= 1m,
                $"{TransactionDocumentsOptions.SectionName}:ConfidenceThreshold must be between 0 and 1.")
            .ValidateOnStart();
        services.AddSingleton<ITransactionDocumentRepository, SqliteTransactionDocumentRepository>();
        services.AddScoped<ITransactionDocumentService, TransactionDocumentService>();
        return services;
    }
}
