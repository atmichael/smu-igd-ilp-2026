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

        public Task MarkAsReadAsync(IReadOnlyCollection<uint> uniqueIds, string traceId, CancellationToken cancellationToken)
        {
            MarkedAsRead.AddRange(uniqueIds);
            return Task.CompletedTask;
        }
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

        public Task SaveAsync(IReadOnlyList<DocumentInboxMessageMappingDto> mappings, CancellationToken cancellationToken = default)
        {
            Saves.Add(mappings);
            return Task.CompletedTask;
        }
    }

    private static IOptions<InboxMonitoringOptions> Opts(string? autoMarkAsRead = null) =>
        Options.Create(new InboxMonitoringOptions { AutoMarkAsReadSetting = autoMarkAsRead });

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
