namespace ILP.Server.Features.EvidenceStorage;

public abstract class EvidenceException : Exception
{
    protected EvidenceException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}

public sealed class EvidenceNotFoundException : EvidenceException
{
    public EvidenceNotFoundException(string message) : base(message)
    {
    }
}

public sealed class EvidenceDuplicateException : EvidenceException
{
    public EvidenceDuplicateException(string message) : base(message)
    {
    }
}

public sealed class EvidencePreconditionException : EvidenceException
{
    public EvidencePreconditionException(string message) : base(message)
    {
    }
}

public sealed class EvidenceValidationException : EvidenceException
{
    public EvidenceValidationException(IDictionary<string, string[]> errors, int statusCode = StatusCodes.Status422UnprocessableEntity)
        : base("Evidence validation failed.")
    {
        Errors = errors;
        StatusCode = statusCode;
    }

    public IDictionary<string, string[]> Errors { get; }

    public int StatusCode { get; }
}

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
