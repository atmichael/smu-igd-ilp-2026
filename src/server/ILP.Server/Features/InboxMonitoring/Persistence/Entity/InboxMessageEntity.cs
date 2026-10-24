namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public sealed class InboxMessageEntity : AuditedEntity
{
    public uint UniqueId { get; set; }

    public string Subject { get; set; } = "";

    public string Sender { get; set; } = "";

    public string Body { get; set; } = "";

    public ICollection<InboxMessageAttachmentEntity> Attachments { get; set; } = new List<InboxMessageAttachmentEntity>();

    public ICollection<DocumentInboxMessageEntity> DocumentMappings { get; set; } = new List<DocumentInboxMessageEntity>();
}
