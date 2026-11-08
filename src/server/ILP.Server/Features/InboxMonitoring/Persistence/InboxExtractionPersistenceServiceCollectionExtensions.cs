using ILP.Server.Config;
using ILP.Server.Features.InboxMonitoring.Persistence.Repository;
using ILP.Shared.InboxMonitoring.Abstraction;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;

namespace ILP.Server.Features.InboxMonitoring.Persistence;

public static class InboxExtractionPersistenceServiceCollectionExtensions
{
    public static IServiceCollection AddInboxExtractionPersistence(this IServiceCollection services)
    {
        services.AddDbContextFactory<InboxExtractionDbContext>(options =>
            options.UseSqlite(InboxExtractionPersistenceConfig.ConnectionString)
                .ConfigureWarnings(warnings => warnings.Ignore(RelationalEventId.PendingModelChangesWarning)));
        services.AddSingleton<IInboxExtractionPersistence, SqliteInboxExtractionRepo>();
        return services;
    }
}
