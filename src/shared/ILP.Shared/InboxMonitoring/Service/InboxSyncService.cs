using ILP.Shared.Helper;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using Microsoft.Extensions.Options;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class InboxSyncService : IInboxSyncService
{
    private readonly IInboxMessageSource _source;
    private readonly IDocumentContentExtractor _extractor;
    private readonly InboxMonitoringOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public InboxSyncService(IInboxMessageSource source, IDocumentContentExtractor extractor, IOptions<InboxMonitoringOptions> options)
    {
        _options = options.Value;
        _source = source;
        _extractor = extractor;
    }

    public async Task<InboxSyncResultDto> SyncAsync(CancellationToken cancellationToken = default)
    {
        // Serialize runs so the background worker and a manual trigger never read the inbox concurrently.
        await _gate.WaitAsync(cancellationToken);
        try
        {
            string traceId = Guid.NewGuid().ToString();
            var messages = await _source.FetchUnreadAsync(traceId, cancellationToken);

            var contents = new List<string>();
            int processed = 0;
            int failed = 0;
            var processedUniqueIds = new List<uint>();

            foreach (var message in messages)
            {
                try
                {
                    LogHelper.Info(traceId, $"Subject: {message.Subject}");
                    LogHelper.Info(traceId, $"From: {message.From}");

                    if (message.AttachmentPaths.Count == 0)
                    {
                        contents.Add(await _extractor.ExtractAsync(message.Body, "", traceId, message.InlineImages));
                    }

                    foreach (var file in message.AttachmentPaths)
                    {
                        contents.Add(await _extractor.ExtractAsync(message.Body, file, traceId, message.InlineImages));
                    }

                    processed++;
                    processedUniqueIds.Add(message.UniqueId);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    failed++;
                    LogHelper.Error(traceId, ex, $"Failed to process message '{message.Subject}'");
                }
                finally
                {
                    DeleteAttachments(message);
                }
            }

            if (_options.AutoMarkAsRead && processedUniqueIds.Count > 0)
            {
                try
                {
                    await _source.MarkAsReadAsync(processedUniqueIds, traceId, cancellationToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    LogHelper.Error(traceId, ex, "Failed to mark processed messages as read");
                }
            }

            return new InboxSyncResultDto
            {
                TraceId = traceId,
                MessagesProcessed = processed,
                MessagesFailed = failed,
                ExtractedContents = contents
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    private static void DeleteAttachments(InboxMessageDto message)
    {
        foreach (var file in message.AttachmentPaths)
        {
            try
            {
                File.Delete(file);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }
    }
}
