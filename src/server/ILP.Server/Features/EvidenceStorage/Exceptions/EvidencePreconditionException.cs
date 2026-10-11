namespace ILP.Server.Features.EvidenceStorage.Exceptions;

public sealed class EvidencePreconditionException : EvidenceException
{
    public EvidencePreconditionException(string message) : base(message)
    {
    }
}
