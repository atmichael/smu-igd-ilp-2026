namespace ILP.Shared.SourceDocuments;

public sealed class SourceDocumentMetadata
{
    public string SourceDocumentId { get; set; } = Guid.NewGuid().ToString();
    public string Channel { get; set; } = SourceDocumentChannels.CameraCapture;
    public string Status { get; set; } = "received";
    public DateTimeOffset ReceivedAt { get; set; }
    public string? SubmittedBy { get; set; }
    public string MediaType { get; set; } = string.Empty;
    public int PageCount { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string StorageLocation { get; set; } = string.Empty;
}

public static class SourceDocumentChannels
{
    public const string CameraCapture = "camera-capture";
    public const string FileUpload = "file-upload";
    public const string Mailbox = "mailbox";
}
