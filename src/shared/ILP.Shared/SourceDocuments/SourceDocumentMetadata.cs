namespace ILP.Shared.SourceDocuments;

public sealed class SourceDocumentMetadata
{
    public string SourceDocumentId { get; set; } = Guid.NewGuid().ToString();
    public string Source { get; set; } = "camera-capture";
    public int PageCount { get; set; }
    public string Status { get; set; } = "received";
}
