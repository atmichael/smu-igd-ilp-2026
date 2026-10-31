using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ILP.Shared.InboxMonitoring.Worker;

public sealed class InboxWatchRenewalWorker : BackgroundService
{
    private readonly IInboxWatchClient _watchClient;
    private readonly InboxMonitoringOptions _options;
    private readonly ILogger<InboxWatchRenewalWorker> _logger;

    public InboxWatchRenewalWorker(
        IInboxWatchClient watchClient,
        IOptions<InboxMonitoringOptions> options,
        ILogger<InboxWatchRenewalWorker> logger)
    {
        _watchClient = watchClient;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Inbox monitoring is disabled; inbox watch will not be renewed.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await _watchClient.WatchAsync(InboxWatchConfig.TopicName, stoppingToken);
                _logger.LogInformation("Inbox watch renewed for topic {Topic}.", InboxWatchConfig.TopicName);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Inbox watch renewal failed; will retry after the renewal interval.");
            }

            try
            {
                await Task.Delay(InboxWatchConfig.RenewInterval, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }
}
