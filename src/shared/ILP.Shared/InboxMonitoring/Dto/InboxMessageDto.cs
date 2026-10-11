namespace ILP.Shared.InboxMonitoring.Dto;

public sealed class InboxMessageDto
{
    public string Subject { get; init; } = "";

    public string From { get; init; } = "";

    public string Body { get; init; } = "";

    public IReadOnlyDictionary<string, string> InlineImages { get; init; } =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>Temporary files holding attachments; deleted by the sync service once processed.</summary>
    public IReadOnlyList<string> AttachmentPaths { get; init; } = [];
}
