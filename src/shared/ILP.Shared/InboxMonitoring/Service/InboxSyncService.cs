using ILP.Shared.Helper;
using ILP.Shared.AccountsPayable.Abstraction;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.InfoExtraction.Parser;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Extensions.Options;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class InboxSyncService : IInboxSyncService
{
    private readonly IInboxMessageSource _source;
    private readonly IDocumentContentExtractor _extractor;
    private readonly IGoogleDriveFileProvider? _driveFileProvider;
    private readonly IInboxExtractionPersistence? _persistence;
    private readonly IDocumentNotificationPublisher? _notificationPublisher;
    private readonly IAccountsPayableCodingProcessor? _accountsPayableCodingProcessor;
    private readonly IInboxMessageSender? _messageSender;
    private readonly InboxMonitoringOptions _options;
    private readonly SemaphoreSlim _gate = new(1, 1);

    public InboxSyncService(
        IInboxMessageSource source,
        IDocumentContentExtractor extractor,
        IOptions<InboxMonitoringOptions> options,
        IGoogleDriveFileProvider? driveFileProvider = null,
        IInboxExtractionPersistence? persistence = null,
        IDocumentNotificationPublisher? notificationPublisher = null,
        IAccountsPayableCodingProcessor? accountsPayableCodingProcessor = null,
        IInboxMessageSender? messageSender = null)
    {
        _notificationPublisher = notificationPublisher;
        _accountsPayableCodingProcessor = accountsPayableCodingProcessor;
        _messageSender = messageSender;
        _options = options.Value;
        _source = source;
        _extractor = extractor;
        _driveFileProvider = driveFileProvider;
        _persistence = persistence;
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

                    var oversized = message.AttachmentPaths
                        .Where(file => new FileInfo(file).Length > _options.AttachmentLimitInBytes)
                        .Select(file => message.AttachmentNames.TryGetValue(file, out var name) ? name : "attachment")
                        .ToArray();
                    if (oversized.Length > 0)
                    {
                        if (_messageSender is null) throw new InvalidOperationException("Inbox message sender is not configured.");
                        await _messageSender.SendOversizeReplyAsync(message, oversized, _options.AttachmentLimitInMB, cancellationToken);
                        await _source.MarkAsReadAsync([message.UniqueId], traceId, cancellationToken);
                        processed++;
                        continue;
                    }

                    var extractedDocuments = new List<(DocumentDto Document, InboxMessageAttachmentDto? Attachment)>();
                    var driveAttachments = new List<InboxMessageAttachmentDto>();

                    var seenKeys = new HashSet<(string, string, string)>();

                    if (message.AttachmentPaths.Count == 0)
                    {
                        string content = await _extractor.ExtractAsync(message.Body, "", traceId, message.InlineImages);
                        contents.Add(content);
                        foreach (var parsed in ExtractedDocumentParser.ParseAll(content))
                        {
                            var document = parsed.GetDocumentInfo();
                            if (!await IsDuplicateAsync(document, message, seenKeys, traceId, cancellationToken))
                            {
                                extractedDocuments.Add((document, null));
                            }
                        }
                    }

                    foreach (var file in message.AttachmentPaths)
                    {
                        string content = await _extractor.ExtractAsync(message.Body, file, traceId, message.InlineImages);
                        contents.Add(content);

                        foreach (var parsed in ExtractedDocumentParser.ParseAll(content))
                        {
                            var document = parsed.GetDocumentInfo();
                            if (await IsDuplicateAsync(document, message, seenKeys, traceId, cancellationToken))
                            {
                                continue;
                            }

                            InboxMessageAttachmentDto? uploadedAttachment = null;
                            if (_driveFileProvider != null)
                            {
                                uploadedAttachment = await _driveFileProvider.UploadAsync(file, document, cancellationToken);
                                if (!driveAttachments.Any(existing => existing.DriveFileId == uploadedAttachment.DriveFileId))
                                {
                                    driveAttachments.Add(uploadedAttachment);
                                }
                            }

                            extractedDocuments.Add((document, uploadedAttachment));
                        }
                    }

                    if (_persistence != null && extractedDocuments.Count > 0)
                    {
                        var persistedMessage = new InboxMessageDto
                        {
                            UniqueId = message.UniqueId,
                            Subject = message.Subject,
                            From = message.From,
                            Body = message.Body,
                            InlineImages = message.InlineImages,
                            AttachmentPaths = message.AttachmentPaths,
                            GoogleDriveAttachments = driveAttachments
                        };

                        var mappings = new List<DocumentInboxMessageMappingDto>();
                        foreach (var extracted in extractedDocuments)
                        {
                            DocumentAPJournalDto? apJournal = null;
                            if (_accountsPayableCodingProcessor is not null && IsInvoiceOrCreditNote(extracted.Document.TypeCode))
                            {
                                try
                                {
                                    apJournal = await _accountsPayableCodingProcessor.ProcessAsync(extracted.Document, cancellationToken);
                                }
                                catch (Exception ex) when (ex is not OperationCanceledException)
                                {
                                    LogHelper.Error(traceId, ex, $"Failed to suggest AP coding for '{extracted.Document.RefNumber}'");
                                    apJournal = new DocumentAPJournalDto(
                                        0m,
                                        "SG_UNKNOWN",
                                        [],
                                        ["AP coding could not be completed"]);
                                }
                            }

                            mappings.Add(new DocumentInboxMessageMappingDto(
                                extracted.Document,
                                persistedMessage,
                                extracted.Attachment,
                                apJournal));
                        }

                        await _persistence.SaveAsync(mappings, cancellationToken);
                        PublishNotifications(extractedDocuments.Select(extracted => extracted.Document), traceId);
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

    private static bool IsInvoiceOrCreditNote(string? documentType) =>
        string.Equals(documentType, "invoice", StringComparison.OrdinalIgnoreCase)
        || string.Equals(documentType, "credit-note", StringComparison.OrdinalIgnoreCase)
        || string.Equals(documentType, "credit note", StringComparison.OrdinalIgnoreCase);

    private void PublishNotifications(IEnumerable<DocumentDto> documents, string traceId)
    {
        if (_notificationPublisher == null)
        {
            return;
        }

        foreach (var document in documents)
        {
            try
            {
                _notificationPublisher.Publish(
                    new DocumentNotificationDto(document.TypeCode, document.RefNumber, document.Company?.UEN));
            }
            catch (Exception ex)
            {
                LogHelper.Error(traceId, ex, "Failed to publish document notification");
            }
        }
    }

    private async Task<bool> IsDuplicateAsync(
        DocumentDto document,
        InboxMessageDto message,
        HashSet<(string, string, string)> seenKeys,
        string traceId,
        CancellationToken cancellationToken)
    {
        if (_persistence == null)
        {
            return false;
        }

        string uen = document.Company.UEN ?? "";
        bool duplicate = !string.IsNullOrWhiteSpace(uen)
            && (!seenKeys.Add((uen, document.TypeCode, document.RefNumber))
                || await _persistence.ExistsAsync(document, cancellationToken));
        if (duplicate)
        {
            LogHelper.Info(traceId,
                $"Email {message.UniqueId} contains duplicate document type for {uen} , {document.TypeCode} , {document.RefNumber}");
        }

        return duplicate;
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
