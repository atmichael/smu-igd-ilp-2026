using ILP.Shared.AccountsPayable.Abstraction;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Config;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.InboxMonitoring.Service;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace ILP.Shared.Test.InboxMonitoring.Service;

public class InboxSyncServiceTest
{
    private sealed class FakeSource(params InboxMessageDto[] messages) : IInboxMessageSource
    {
        public Task<IReadOnlyList<InboxMessageDto>> FetchUnreadAsync(string traceId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InboxMessageDto>>(messages);

        public List<uint> MarkedAsRead { get; } = [];
        public bool FailMarkOnce { get; set; }

        public Task MarkAsReadAsync(IReadOnlyCollection<uint> uniqueIds, string traceId, CancellationToken cancellationToken)
        {
            if (FailMarkOnce)
            {
                FailMarkOnce = false;
                throw new IOException("Mark failed");
            }
            MarkedAsRead.AddRange(uniqueIds);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeSender : IInboxMessageSender
    {
        public List<(InboxMessageDto Message, IReadOnlyList<string> Names)> Replies { get; } = [];
        public bool Fail { get; set; }

        public Task SendAttachmentsAsync(string recipient, string subject, string body, IReadOnlyList<EmailAttachmentDto> attachments, CancellationToken cancellationToken = default) => Task.CompletedTask;

        public Task SendOversizeReplyAsync(InboxMessageDto message, IReadOnlyList<string> names, int limitInMB, CancellationToken cancellationToken = default)
        {
            if (Fail) throw new InvalidOperationException("Send failed");
            Replies.Add((message, names));
            return Task.CompletedTask;
        }
    }

    [Theory]
    [InlineData(2097151, false)]
    [InlineData(2097152, false)]
    [InlineData(2097153, true)]
    public async Task SyncAsync_AttachmentSize_EnforcesInclusiveLimit(int size, bool oversized)
    {
        string file = Path.GetTempFileName();
        using (var stream = File.OpenWrite(file)) stream.SetLength(size);
        var source = new FakeSource(new InboxMessageDto
        {
            UniqueId = 42,
            From = "supplier@example.com",
            AttachmentPaths = [file],
            AttachmentNames = new Dictionary<string, string> { [file] = "invoice.pdf" }
        });
        var sender = new FakeSender();
        var extractor = new FakeExtractor();
        var sut = new InboxSyncService(source, extractor, Opts("N"), messageSender: sender);

        await sut.SyncAsync();

        Assert.Equal(oversized ? 1 : 0, sender.Replies.Count);
        Assert.Equal(oversized ? 0 : 1, extractor.Calls.Count);
        Assert.Equal(oversized ? new uint[] { 42 } : [], source.MarkedAsRead);
        if (oversized) Assert.Equal("invoice.pdf", Assert.Single(sender.Replies[0].Names));
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task SyncAsync_WhenOversizeReplyFails_DoesNotMarkReadOrExtract()
    {
        string file = Path.GetTempFileName();
        using (var stream = File.OpenWrite(file)) stream.SetLength(2097153);
        var source = new FakeSource(new InboxMessageDto { UniqueId = 42, AttachmentPaths = [file] });
        var extractor = new FakeExtractor();
        var sut = new InboxSyncService(source, extractor, Opts(), messageSender: new FakeSender { Fail = true });

        var result = await sut.SyncAsync();

        Assert.Equal(1, result.MessagesFailed);
        Assert.Empty(source.MarkedAsRead);
        Assert.Empty(extractor.Calls);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task SyncAsync_RetriesMarkWithoutSendingAnotherReply()
    {
        string file = Path.GetTempFileName();
        using (var stream = File.OpenWrite(file)) stream.SetLength(2097153);
        var source = new FakeSource(new InboxMessageDto { UniqueId = 42, AttachmentPaths = [file] }) { FailMarkOnce = true };
        var sender = new FakeSender();
        var sut = new InboxSyncService(source, new FakeExtractor(), Opts(), messageSender: sender);

        var first = await sut.SyncAsync();
        using (var stream = File.OpenWrite(file)) stream.SetLength(2097153);
        var second = await sut.SyncAsync();

        Assert.Equal(1, first.MessagesFailed);
        Assert.Equal(0, second.MessagesFailed);
        Assert.Single(sender.Replies);
        Assert.Equal([42u], source.MarkedAsRead);
    }

    [Fact]
    public async Task SyncAsync_OneOversizedAttachment_RejectsWholeMessageUsingConfiguredLimit()
    {
        string small = Path.GetTempFileName();
        string large = Path.GetTempFileName();
        using (var stream = File.OpenWrite(large)) stream.SetLength(3 * 1024 * 1024 + 1);
        var source = new FakeSource(new InboxMessageDto { UniqueId = 42, AttachmentPaths = [small, large] });
        var extractor = new FakeExtractor();
        var sender = new FakeSender();
        var options = Options.Create(new InboxMonitoringOptions { AttachmentLimitInMB = 3 });
        var sut = new InboxSyncService(source, extractor, options, messageSender: sender);

        await sut.SyncAsync();

        Assert.Single(sender.Replies);
        Assert.Empty(extractor.Calls);
        Assert.Equal([42u], source.MarkedAsRead);
        Assert.False(File.Exists(small));
        Assert.False(File.Exists(large));
    }

    [Fact]
    public void CreateOversizeReply_UsesReplyAddressAndThreadHeaders()
    {
        var original = new InboxMessageDto { ReplyTo = "reply@example.com", Subject = "Invoice", MessageId = "original@example.com" };
        var reply = GmailInboxMessageSender.CreateOversizeReply(original, ["invoice.pdf"], 2, "inbox@example.com");

        Assert.Equal("inbox@example.com", Assert.Single(reply.From.Mailboxes).Address);
        Assert.Equal("reply@example.com", Assert.Single(reply.To.Mailboxes).Address);
        Assert.Equal("Re: Invoice", reply.Subject);
        Assert.Equal("original@example.com", reply.InReplyTo);
        Assert.Contains("original@example.com", reply.References);
        Assert.Equal("auto-replied", reply.Headers["Auto-Submitted"]);
        Assert.Contains("invoice.pdf", reply.TextBody);
        Assert.Contains("2 MB", reply.TextBody);
        Assert.Contains("resend all attachments", reply.TextBody);
    }
    private sealed class FakeExtractor : IDocumentContentExtractor
    {
        public List<(string Body, string Attachment)> Calls { get; } = [];

        public Task<string> ExtractAsync(string body, string attachmentPath, string traceId, IReadOnlyDictionary<string, string> inlineImages)
        {
            Calls.Add((body, attachmentPath));
            if (body == "boom")
            {
                throw new InvalidOperationException("boom");
            }

            return Task.FromResult($"content:{body}:{attachmentPath}");
        }
    }

    private sealed class FixedExtractor(string content) : IDocumentContentExtractor
    {
        public Task<string> ExtractAsync(string body, string attachmentPath, string traceId, IReadOnlyDictionary<string, string> inlineImages) =>
            Task.FromResult(content);
    }

    private sealed class FakeDriveFileProvider : IGoogleDriveFileProvider
    {
        public List<string> UploadedFiles { get; } = [];
        public List<DocumentDto> Documents { get; } = [];

        public Task<InboxMessageAttachmentDto> UploadAsync(
            string filePath,
            DocumentDto document,
            CancellationToken cancellationToken = default)
        {
            UploadedFiles.Add(filePath);
            Documents.Add(document);
            return Task.FromResult(new InboxMessageAttachmentDto(
                Path.GetFileName(filePath),
                "drive-file-id",
                "https://drive.google.com/file/d/drive-file-id/view"));
        }
    }

    private sealed class FakePersistence : IInboxExtractionPersistence
    {
        public List<IReadOnlyList<DocumentInboxMessageMappingDto>> Saves { get; } = [];

        public HashSet<(string Uen, string Type, string Ref)> Existing { get; } = [];

        public Task<bool> ExistsAsync(DocumentDto document, CancellationToken cancellationToken = default) =>
            Task.FromResult(Existing.Contains((document.Company.UEN ?? "", document.TypeCode, document.RefNumber)));

        public Task SaveAsync(IReadOnlyList<DocumentInboxMessageMappingDto> mappings, CancellationToken cancellationToken = default)
        {
            Saves.Add(mappings);
            return Task.CompletedTask;
        }
    }

    private sealed class FakeAccountsPayableCodingProcessor : IAccountsPayableCodingProcessor
    {
        public List<DocumentDto> ProcessedDocuments { get; } = [];

        public Task<DocumentAPJournalDto> ProcessAsync(DocumentDto document, CancellationToken cancellationToken)
        {
            ProcessedDocuments.Add(document);
            return Task.FromResult(new DocumentAPJournalDto(
                0.82m,
                "SG_UNKNOWN",
                [new AccountsPayableLineSuggestion(1, "6000", "Office supplies", true)],
                ["Invoice totals do not reconcile", "GST treatment requires verification"]));
        }
    }

    private static IOptions<InboxMonitoringOptions> Opts(string? autoMarkAsRead = null) =>
        Options.Create(new InboxMonitoringOptions { AutoMarkAsReadSetting = autoMarkAsRead });

    private const string MultiDocumentContent =
        "[DOCUMENT]\n[HEADER]\ncompany-uen| 201509876K\ndocument-number| INV-1\ndocument-type| Invoice\n"
        + "[DOCUMENT]\n[HEADER]\ncompany-uen| 201509876K\ndocument-number| INV-2\ndocument-type| Invoice";

    private static InboxMessageDto MessageWithAttachment() =>
        new() { UniqueId = 7, Body = "b", AttachmentPaths = [Path.GetTempFileName()] };

    [Fact]
    public async Task SyncAsync_MultipleDocumentsInOneAttachment_PersistsAll()
    {
        var persistence = new FakePersistence();
        var sut = new InboxSyncService(new FakeSource(MessageWithAttachment()), new FixedExtractor(MultiDocumentContent), Opts(), null, persistence);

        await sut.SyncAsync();

        var mappings = Assert.Single(persistence.Saves);
        Assert.Equal(["INV-1", "INV-2"], mappings.Select(m => m.Document.RefNumber).ToArray());
    }

    [Fact]
    public async Task SyncAsync_MultipleDocumentsWithSomeDuplicates_PersistsOnlyNew()
    {
        var persistence = new FakePersistence();
        var existing = ILP.Shared.InfoExtraction.Parser.ExtractedDocumentParser.ParseAll(MultiDocumentContent)[0].GetDocumentInfo();
        persistence.Existing.Add(("201509876K", existing.TypeCode, existing.RefNumber));
        var source = new FakeSource(MessageWithAttachment());
        var sut = new InboxSyncService(source, new FixedExtractor(MultiDocumentContent), Opts(), null, persistence);

        var result = await sut.SyncAsync();

        Assert.Equal(0, result.MessagesFailed);
        var mappings = Assert.Single(persistence.Saves);
        Assert.Equal("INV-2", Assert.Single(mappings).Document.RefNumber);
    }

    [Fact]
    public async Task SyncAsync_SuccessAndFailure_MarksOnlySuccessfulAsRead()
    {
        var source = new FakeSource(
            new InboxMessageDto { UniqueId = 1, Body = "ok" },
            new InboxMessageDto { UniqueId = 2, Body = "boom" });
        var sut = new InboxSyncService(source, new FakeExtractor(), Opts());

        await sut.SyncAsync();

        Assert.Equal([1u], source.MarkedAsRead);
    }

    [Fact]
    public async Task SyncAsync_AutoMarkAsReadDisabled_DoesNotMark()
    {
        var source = new FakeSource(new InboxMessageDto { UniqueId = 1, Body = "ok" });
        var sut = new InboxSyncService(source, new FakeExtractor(), Opts("N"));

        await sut.SyncAsync();

        Assert.Empty(source.MarkedAsRead);
    }

    [Theory]
    [InlineData(null, true)]
    [InlineData("Y", true)]
    [InlineData("y", true)]
    [InlineData("N", false)]
    [InlineData("true", false)]
    public void AutoMarkAsRead_ParsesSetting(string? value, bool expected)
    {
        Assert.Equal(expected, new InboxMonitoringOptions { AutoMarkAsReadSetting = value }.AutoMarkAsRead);
    }

    [Fact]
    public void AutoMarkAsRead_BindsFromConfiguration()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["AutoMarkAsRead"] = "n" })
            .Build();

        var options = config.Get<InboxMonitoringOptions>()!;

        Assert.False(options.AutoMarkAsRead);
        Assert.True(new ConfigurationBuilder().Build().Get<InboxMonitoringOptions>() is null);
    }

    [Fact]
    public async Task SyncAsync_NoMessages_ReturnsEmptyResult()
    {
        var sut = new InboxSyncService(new FakeSource(), new FakeExtractor(), Opts());

        var result = await sut.SyncAsync();

        Assert.Equal(0, result.MessagesProcessed);
        Assert.Empty(result.ExtractedContents);
    }

    [Fact]
    public async Task SyncAsync_MessageWithoutAttachment_ExtractsFromBody()
    {
        var extractor = new FakeExtractor();
        var sut = new InboxSyncService(new FakeSource(new InboxMessageDto { Body = "hello" }), extractor, Opts());

        var result = await sut.SyncAsync();

        Assert.Equal(1, result.MessagesProcessed);
        Assert.Equal([("hello", "")], extractor.Calls);
    }

    [Fact]
    public async Task SyncAsync_MessageWithAttachments_ExtractsEachAttachmentAndDeletesFiles()
    {
        string file1 = Path.GetTempFileName();
        string file2 = Path.GetTempFileName();
        var extractor = new FakeExtractor();
        var message = new InboxMessageDto { Body = "b", AttachmentPaths = [file1, file2] };
        var sut = new InboxSyncService(new FakeSource(message), extractor, Opts());

        var result = await sut.SyncAsync();

        Assert.Equal(2, result.ExtractedContents.Count);
        Assert.Equal([("b", file1), ("b", file2)], extractor.Calls);
        Assert.False(File.Exists(file1));
        Assert.False(File.Exists(file2));
    }

    [Fact]
    public async Task SyncAsync_ExtractionFailure_CountsFailureAndContinues()
    {
        var sut = new InboxSyncService(
            new FakeSource(new InboxMessageDto { Body = "boom" }, new InboxMessageDto { Body = "ok" }),
            new FakeExtractor(), Opts());

        var result = await sut.SyncAsync();

        Assert.Equal(1, result.MessagesFailed);
        Assert.Equal(1, result.MessagesProcessed);
    }

    [Fact]
    public async Task SyncAsync_UploadsAndPersistsExtractedDocumentWithSourceLinks()
    {
        string file = Path.GetTempFileName();
        var source = new FakeSource(new InboxMessageDto
        {
            UniqueId = 21,
            Subject = "Invoice",
            From = "supplier@example.com",
            Body = "Please find attached.",
            AttachmentPaths = [file]
        });
        var drive = new FakeDriveFileProvider();
        var persistence = new FakePersistence();
        var sut = new InboxSyncService(
            source,
            new FixedExtractor("""
                [HEADER]
                document-number| INV-21
                document-type| Invoice
                company-uen| UEN-21
                total-amount| 12.50
                [CONFIDENCE]
                confidence|0.91
                """),
            Opts(),
            drive,
            persistence);

        var result = await sut.SyncAsync();

        Assert.Equal(1, result.MessagesProcessed);
        Assert.Equal([file], drive.UploadedFiles);
        Assert.Equal("UEN-21", Assert.Single(drive.Documents).Company.UEN);
        Assert.Equal("Invoice", drive.Documents[0].TypeCode);
        Assert.Equal("INV-21", drive.Documents[0].RefNumber);
        var mapping = Assert.Single(Assert.Single(persistence.Saves));
        Assert.Equal("INV-21", mapping.Document.RefNumber);
        Assert.Equal(0.91m, mapping.Document.Confidence);
        Assert.Equal(21u, mapping.InboxMessage.UniqueId);
        Assert.Equal("https://drive.google.com/file/d/drive-file-id/view",
            Assert.Single(mapping.InboxMessage.GoogleDriveAttachments).WebViewLink);
        Assert.Equal("drive-file-id", mapping.SourceAttachment?.DriveFileId);
        Assert.False(File.Exists(file));
    }

    [Fact]
    public async Task SyncAsync_Invoice_PersistsAccountsPayableCodingAndVerificationNotes()
    {
        var persistence = new FakePersistence();
        var codingProcessor = new FakeAccountsPayableCodingProcessor();
        var sut = new InboxSyncService(
            new FakeSource(new InboxMessageDto { UniqueId = 32, Body = "b" }),
            new FixedExtractor("""
                [HEADER]
                document-number| INV-32
                document-type| Invoice
                company-uen| UEN-32
                [LINE_ITEMS]
                1|Office supplies|1|10|10|0.09|0.90
                [CONFIDENCE]
                confidence|0.98
                """),
            Opts(),
            persistence: persistence,
            accountsPayableCodingProcessor: codingProcessor);

        await sut.SyncAsync();

        Assert.Equal("INV-32", Assert.Single(codingProcessor.ProcessedDocuments).RefNumber);
        var mapping = Assert.Single(Assert.Single(persistence.Saves));
        Assert.Equal(0.82m, mapping.ApJournal!.Confidence);
        Assert.Equal("SG_UNKNOWN", mapping.ApJournal.GstCode);
        Assert.Equal(
            ["Invoice totals do not reconcile", "GST treatment requires verification"],
            mapping.ApJournal.VerificationNotes);
    }

    private const string InvoiceContent = """
        [HEADER]
        document-number| INV-1
        document-type| Invoice
        company-uen| UEN-1
        total-amount| 5
        """;

    [Fact]
    public async Task SyncAsync_DuplicateComposite_DoesNotInsertAndMarksAsRead()
    {
        var source = new FakeSource(new InboxMessageDto { UniqueId = 5, Body = "b" });
        var persistence = new FakePersistence();
        persistence.Existing.Add(("UEN-1", "Invoice", "INV-1"));
        var sut = new InboxSyncService(source, new FixedExtractor(InvoiceContent), Opts(), null, persistence);

        var result = await sut.SyncAsync();

        Assert.Empty(persistence.Saves);
        Assert.Equal(1, result.MessagesProcessed);
        Assert.Equal([5u], source.MarkedAsRead);
    }

    private sealed class FakePublisher : IDocumentNotificationPublisher
    {
        public List<DocumentNotificationDto> Published { get; } = [];

        public void Publish(DocumentNotificationDto notification) => Published.Add(notification);
    }

    [Fact]
    public async Task SyncAsync_NewDocument_PublishesNotification()
    {
        var source = new FakeSource(new InboxMessageDto { UniqueId = 7, Body = "b" });
        var publisher = new FakePublisher();
        var sut = new InboxSyncService(
            source, new FixedExtractor(InvoiceContent), Opts(), null, new FakePersistence(), publisher);

        await sut.SyncAsync();

        Assert.Equal(new DocumentNotificationDto("Invoice", "INV-1", "UEN-1"), Assert.Single(publisher.Published));
    }

    [Fact]
    public async Task SyncAsync_DuplicateDocument_DoesNotPublishNotification()
    {
        var persistence = new FakePersistence();
        persistence.Existing.Add(("UEN-1", "Invoice", "INV-1"));
        var publisher = new FakePublisher();
        var sut = new InboxSyncService(
            new FakeSource(new InboxMessageDto { UniqueId = 8, Body = "b" }),
            new FixedExtractor(InvoiceContent), Opts(), null, persistence, publisher);

        await sut.SyncAsync();

        Assert.Empty(publisher.Published);
    }

    [Fact]
    public async Task SyncAsync_NewComposite_InsertsRow()
    {
        var source = new FakeSource(new InboxMessageDto { UniqueId = 6, Body = "b" });
        var persistence = new FakePersistence();
        var sut = new InboxSyncService(source, new FixedExtractor(InvoiceContent), Opts(), null, persistence);

        await sut.SyncAsync();

        Assert.Single(Assert.Single(persistence.Saves));
        Assert.Equal([6u], source.MarkedAsRead);
    }

    [Fact]
    public void GoogleDriveFileProvider_GetFolderNames_UsesSanitizedDocumentFields()
    {
        var document = new DocumentDto(
            "INV\\001",
            "Invoice",
            DateTime.UnixEpoch,
            0m,
            company: new CompanyDto("Supplier", "UEN/21"));

        Assert.Equal(["UEN_21", "Invoice", "INV_001"], GoogleDriveFileProvider.GetFolderNames(document));
    }
}
