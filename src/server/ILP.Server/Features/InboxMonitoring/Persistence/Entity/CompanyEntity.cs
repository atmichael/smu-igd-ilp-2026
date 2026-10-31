namespace ILP.Server.Features.InboxMonitoring.Persistence.Entity;

public sealed class CompanyEntity : AuditedEntity
{
    public string? Uen { get; set; }

    public string Name { get; set; } = "";

    public string Address { get; set; } = "";

    public string TaxRegistrationNumber { get; set; } = "";

    public string Email { get; set; } = "";

    public ICollection<DocumentEntity> Documents { get; set; } = new List<DocumentEntity>();
}
