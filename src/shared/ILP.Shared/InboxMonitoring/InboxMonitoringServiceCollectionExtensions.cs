using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Service;
using ILP.Shared.InboxMonitoring.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace ILP.Shared.InboxMonitoring;

public static class InboxMonitoringServiceCollectionExtensions
{
    public static IServiceCollection AddInboxSync(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<InboxMonitoringOptions>(configuration.GetSection(InboxMonitoringOptions.SectionName));
        services.AddSingleton<IInboxMessageSource, ImapInboxMessageSource>();
        services.AddSingleton<IInboxMessageSender, GmailInboxMessageSender>();
        services.AddSingleton<IDocumentContentExtractor, EmailInfoDocumentContentExtractor>();
        services.AddSingleton<IInboxSyncService, InboxSyncService>();
        return services;
    }

    public static IServiceCollection AddInboxMonitoring(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddInboxSync(configuration);
        services.AddHostedService<InboxMonitorWorker>();
        return services;
    }
}
