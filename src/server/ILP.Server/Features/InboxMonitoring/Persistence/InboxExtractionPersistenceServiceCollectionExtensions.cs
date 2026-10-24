using ILP.Server.Config;
using ILP.Server.Features.InboxMonitoring.Persistence.Repository;
using ILP.Shared.InboxMonitoring.Abstraction;
using Microsoft.EntityFrameworkCore;

namespace ILP.Server.Features.InboxMonitoring.Persistence;

public static class InboxExtractionPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInboxExtractionPersistence(this IServiceCollection services)
    {
        services.AddDbContextFactory<InboxExtractionDbContext>(options =>
            options.UseSqlite(InboxExtractionPersistenceConfig.ConnectionString));
        services.AddSingleton<IInboxExtractionPersistence, SqliteInboxExtractionRepo>();
        return services;
    }
}
