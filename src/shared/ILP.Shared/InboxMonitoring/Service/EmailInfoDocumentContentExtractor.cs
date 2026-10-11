using ILP.Shared.InboxMonitoring.Abstraction;
using ILP.Shared.InfoExtraction.Provider;

namespace ILP.Shared.InboxMonitoring.Service;

public sealed class EmailInfoDocumentContentExtractor : IDocumentContentExtractor
{
    public Task<string> ExtractAsync(string body, string attachmentPath, string traceId, IReadOnlyDictionary<string, string> inlineImages) =>
        EmailInfoExtractionProvider.GetDocumentContent(body, attachmentPath, traceId, inlineImages);
}
