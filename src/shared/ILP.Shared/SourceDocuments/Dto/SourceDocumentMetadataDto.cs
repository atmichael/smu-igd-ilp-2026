using System.Text.Json.Serialization;
using ILP.Shared.SourceDocuments.Constant;

namespace ILP.Shared.SourceDocuments.Dto;

public sealed class SourceDocumentMetadataDto
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

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public SourceDocumentOriginDto? Origin { get; set; }
}
