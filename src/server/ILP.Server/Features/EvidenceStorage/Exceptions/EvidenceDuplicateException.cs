namespace ILP.Server.Features.EvidenceStorage.Exceptions;

public sealed class EvidenceDuplicateException : EvidenceException
{
    public EvidenceDuplicateException(string message) : base(message)
    {
    }
}
