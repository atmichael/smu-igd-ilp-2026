namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public sealed class InboxMessageAttachmentEntity : AuditedEntity
{
    public long MessageId { get; set; }

    public InboxMessageEntity Message { get; set; } = null!;

    public string FileName { get; set; } = "";

    public string DriveFileId { get; set; } = "";

    public string WebViewLink { get; set; } = "";

    public ICollection<DocumentInboxMessageEntity> DocumentMappings { get; set; } = new List<DocumentInboxMessageEntity>();
}
