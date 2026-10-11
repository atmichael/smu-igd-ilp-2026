namespace ILP.Shared.InboxMonitoring.Abstraction;

public interface IDocumentContentExtractor
{
    Task<string> ExtractAsync(string body, string attachmentPath, string traceId, IReadOnlyDictionary<string, string> inlineImages);
}
