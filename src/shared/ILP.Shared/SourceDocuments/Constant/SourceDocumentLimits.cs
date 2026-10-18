namespace ILP.Shared.SourceDocuments.Constant;

public static class SourceDocumentLimits
{
    /// <summary>Largest accepted <c>file-upload</c> file (20 MB). Camera capture and mailbox have their own limits.</summary>
    public const long FileUploadMaxBytes = 20 * 1024 * 1024;
}
