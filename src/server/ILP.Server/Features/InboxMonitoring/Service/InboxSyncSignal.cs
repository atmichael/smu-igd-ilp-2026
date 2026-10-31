using System.Threading.Channels;
using ILP.Shared.InboxMonitoring.Abstraction;

namespace ILP.Server.Features.InboxMonitoring.Service;

/// <summary>Coalesces webhook signals and runs the inbox sync off the request thread so Pub/Sub is ACKed promptly.</summary>
public sealed class InboxSyncSignal : BackgroundService
{
    private readonly Channel<bool> _channel = Channel.CreateBounded<bool>(
        new BoundedChannelOptions(1) { FullMode = BoundedChannelFullMode.DropWrite });
    private readonly IInboxSyncService _syncService;
    private readonly ILogger<InboxSyncSignal> _logger;

    public InboxSyncSignal(IInboxSyncService syncService, ILogger<InboxSyncSignal> logger)
    {
        _syncService = syncService;
        _logger = logger;
    }

    public void Signal() => _channel.Writer.TryWrite(true);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            await foreach (var _ in _channel.Reader.ReadAllAsync(stoppingToken))
            {
                try
                {
                    var result = await _syncService.SyncAsync(stoppingToken);
                    _logger.LogInformation("Webhook-triggered inbox sync {TraceId}: {Processed} processed, {Failed} failed.",
                        result.TraceId, result.MessagesProcessed, result.MessagesFailed);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Webhook-triggered inbox sync failed.");
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
    }
}
