namespace ILP.Server.Features.EvidenceStorage.Exceptions;

public sealed class EvidencePersistenceException : EvidenceException
{
    public EvidencePersistenceException(string evidencePackageId, string visibleStatus, Exception innerException)
        : base("Evidence persistence failed; no success state was recorded.", innerException)
    {
        EvidencePackageId = evidencePackageId;
        VisibleStatus = visibleStatus;
    }

    public string EvidencePackageId { get; }

    public string VisibleStatus { get; }
}
