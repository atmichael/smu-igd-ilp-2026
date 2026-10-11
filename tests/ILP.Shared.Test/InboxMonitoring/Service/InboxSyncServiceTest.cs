using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InboxMonitoring.Dto;
using ILP.Shared.InboxMonitoring.Service;

namespace ILP.Shared.Test.InboxMonitoring.Service;

public class InboxSyncServiceTest
{
    private sealed class FakeSource(params InboxMessageDto[] messages) : IInboxMessageSource
    {
        public Task<IReadOnlyList<InboxMessageDto>> FetchUnreadAsync(string traceId, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<InboxMessageDto>>(messages);
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

    [Fact]
    public async Task SyncAsync_NoMessages_ReturnsEmptyResult()
    {
        var sut = new InboxSyncService(new FakeSource(), new FakeExtractor());

        var result = await sut.SyncAsync();

        Assert.Equal(0, result.MessagesProcessed);
        Assert.Empty(result.ExtractedContents);
    }

    [Fact]
    public async Task SyncAsync_MessageWithoutAttachment_ExtractsFromBody()
    {
        var extractor = new FakeExtractor();
        var sut = new InboxSyncService(new FakeSource(new InboxMessageDto { Body = "hello" }), extractor);

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
        var sut = new InboxSyncService(new FakeSource(message), extractor);

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
            new FakeExtractor());

        var result = await sut.SyncAsync();

        Assert.Equal(1, result.MessagesFailed);
        Assert.Equal(1, result.MessagesProcessed);
    }
}
