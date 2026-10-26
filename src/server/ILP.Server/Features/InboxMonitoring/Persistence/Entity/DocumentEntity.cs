namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public sealed class DocumentEntity : AuditedEntity
{
    public string RefNumber { get; set; } = "";

    public string TypeCode { get; set; } = "";

    public long CompanyId { get; set; }

    public CompanyEntity Company { get; set; } = null!;

    public DateTime SentDate { get; set; }

    public decimal TotalAmount { get; set; }

    public string? RelatedDocumentNumbers { get; set; }

    public decimal Subtotal { get; set; }

    public decimal SubtotalTaxRate { get; set; }

    public decimal SubtotalTaxAmount { get; set; }

    public decimal? Confidence { get; set; }

    public bool? ManualOverrideFlag { get; set; }

    public string? ManualOverrideBy { get; set; }

    public ICollection<DocumentItemEntity> DocumentItems { get; set; } = new List<DocumentItemEntity>();

    public DocumentInboxMessageEntity? InboxMessageMapping { get; set; }
}
