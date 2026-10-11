namespace ILP.Server.Features.EvidenceStorage.Exceptions;

public abstract class EvidenceException : Exception
{
    protected EvidenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
