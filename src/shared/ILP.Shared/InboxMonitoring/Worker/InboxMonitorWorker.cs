using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ILP.Shared.InboxMonitoring.Worker;

public sealed class InboxMonitorWorker : BackgroundService
{
    private readonly IInboxSyncService _syncService;
    private readonly InboxMonitoringOptions _options;
    private readonly ILogger<InboxMonitorWorker> _logger;

    public InboxMonitorWorker(
        IInboxSyncService syncService,
        IOptions<InboxMonitoringOptions> options,
        ILogger<InboxMonitorWorker> logger)
    {
        _syncService = syncService;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Inbox monitoring is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var result = await _syncService.SyncAsync(stoppingToken);
                _logger.LogInformation("Inbox sync {TraceId}: {Processed} processed, {Failed} failed.",
                    result.TraceId, result.MessagesProcessed, result.MessagesFailed);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inbox sync failed; will retry after the poll interval.");
            }

            try
            {
                await Task.Delay(_options.PollInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
