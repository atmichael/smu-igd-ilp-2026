namespace ILP.Server.Features.EvidenceStorage.Exceptions;

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
