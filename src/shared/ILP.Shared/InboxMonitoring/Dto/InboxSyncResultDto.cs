namespace ILP.Shared.InboxMonitoring.Dto;

public sealed class InboxSyncResultDto
{
    public string TraceId { get; init; } = "";

    public int MessagesProcessed { get; init; }

    public int MessagesFailed { get; init; }

    public IReadOnlyList<string> ExtractedContents { get; init; } = [];
}
