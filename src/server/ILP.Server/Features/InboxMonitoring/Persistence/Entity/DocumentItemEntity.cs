namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public sealed class DocumentItemEntity : AuditedEntity
{
    public long DocumentId { get; set; }

    public DocumentEntity Document { get; set; } = null!;

    public string Description { get; set; } = "";

    public decimal Quantity { get; set; }

    public string CurrencyCode { get; set; } = "SGD";

    public decimal UnitPrice { get; set; }

    public string TaxName { get; set; } = "GST";

    public decimal TaxRate { get; set; }

    public int? SerialNumber { get; set; }

    public decimal? Amount { get; set; }

    public decimal? TaxAmount { get; set; }
}
