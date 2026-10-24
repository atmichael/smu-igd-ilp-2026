using ILP.Shared.InboxMonitoring.Abstraction;

namespace ILP.Server.Features.InboxMonitoring.Persistence;

public static class InboxExtractionPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInboxExtractionPersistence(this IServiceCollection services)
    {
        services.AddSingleton<IInboxExtractionPersistence, SqliteInboxExtractionPersistence>();
        return services;
    }
}
