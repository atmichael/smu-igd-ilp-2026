namespace ILP.Server.Features.EvidenceStorage.Exceptions;

public sealed class EvidenceNotFoundException : EvidenceException
{
    public EvidenceNotFoundException(string message) : base(message)
    {
    }
}
