using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.AccountsPayable.Persistence.Entity;

public sealed class DocumentAPJournalEntity : AuditedEntity
{
    public long DocumentId { get; set; }

    public DocumentEntity Document { get; set; } = null!;

    public decimal? Confidence { get; set; }

    public string GstCode { get; set; } = "";

    public string CoaSuggestionsJson { get; set; } = "[]";

    public string VerificationNotes { get; set; } = "";
}
