namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public sealed class DocumentInboxMessageEntity : AuditedEntity
{
    public long DocumentId { get; set; }

    public DocumentEntity Document { get; set; } = null!;

    public long MessageId { get; set; }

    public InboxMessageEntity Message { get; set; } = null!;

    public long? SourceAttachmentId { get; set; }

    public InboxMessageAttachmentEntity? SourceAttachment { get; set; }
}
