using System.Text.Json.Serialization;

namespace ILP.Shared.SourceDocuments.Dto;

public sealed class SourceDocumentOriginDto
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public UploadOriginDto? Upload { get; set; }
}

/// <summary>Display-only details of an uploaded file; never used for type detection, storage paths, or logs.</summary>
public sealed class UploadOriginDto
{
    public string OriginalFileName { get; set; } = string.Empty;
}
