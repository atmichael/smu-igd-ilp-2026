namespace ILP.Server.Features.TransactionDocuments.Configuration;

public sealed class TransactionDocumentsOptions
{
    public const string SectionName = "TransactionDocuments";

    public decimal ConfidenceThreshold { get; set; } = 0.95m;
}
