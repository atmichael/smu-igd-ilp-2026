using Microsoft.Extensions.Hosting;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class ReferenceDataReconciliationBackgroundService(IServiceScopeFactory scopeFactory, ILogger<ReferenceDataReconciliationBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var service = scope.ServiceProvider.GetRequiredService<ReferenceDataSynchronizationService>();
                foreach (var adapter in scope.ServiceProvider.GetServices<Adapters.IReferenceDataSourceAdapter>().Where(x => !x.SupportsUpdateNotifications))
                    await service.SynchronizeAsync(adapter.Source, "Daily reconciliation", stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception exception) { logger.LogError(exception, "Reference-data reconciliation failed."); }
            await Task.Delay(TimeSpan.FromDays(1), stoppingToken);
        }
    }
}
