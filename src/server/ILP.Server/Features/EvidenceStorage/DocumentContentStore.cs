using System.Collections.Concurrent;

namespace ILP.Server.Features.EvidenceStorage;

public sealed record DocumentContentPart(string FileName, byte[] Content);

/// <summary>Protected storage for original document files; evidence documents point at it through <c>storageLocation</c>.</summary>
public interface IDocumentContentStore
{
    /// <summary>Stores every part or nothing, and returns the document's storage location.</summary>
    string Save(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts);

    bool Exists(string sourceDocumentId);

    /// <summary>Returns one stored part, or null when the document or part does not exist.</summary>
    byte[]? Read(string sourceDocumentId, string fileName);
}

public static class DocumentContentLocations
{
    private const string Prefix = "protected://source-documents/";

    public static string For(string sourceDocumentId) => Prefix + sourceDocumentId;

    // Only GUID ids are accepted, which keeps route and request values out of file paths.
    public static Guid? ParseId(string sourceDocumentId) =>
        Guid.TryParse(sourceDocumentId, out var id) ? id : null;
}

public sealed class FileDocumentContentStore : IDocumentContentStore
{
    private readonly string _rootPath;

    public FileDocumentContentStore(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public string Save(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts)
    {
        var id = DocumentContentLocations.ParseId(sourceDocumentId)
            ?? throw new ArgumentException("Source document identifiers must be GUIDs.", nameof(sourceDocumentId));
        var finalPath = Path.Combine(_rootPath, id.ToString("D"));
        var tempPath = finalPath + ".tmp";

        try
        {
            Directory.CreateDirectory(tempPath);
            foreach (var part in parts)
            {
                File.WriteAllBytes(Path.Combine(tempPath, Path.GetFileName(part.FileName)), part.Content);
            }

            Directory.Move(tempPath, finalPath);
        }
        catch
        {
            if (Directory.Exists(tempPath))
            {
                Directory.Delete(tempPath, recursive: true);
            }

            throw;
        }

        return DocumentContentLocations.For(id.ToString("D"));
    }

    public bool Exists(string sourceDocumentId) =>
        DocumentContentLocations.ParseId(sourceDocumentId) is { } id
        && Directory.Exists(Path.Combine(_rootPath, id.ToString("D")));

    public byte[]? Read(string sourceDocumentId, string fileName)
    {
        if (DocumentContentLocations.ParseId(sourceDocumentId) is not { } id)
        {
            return null;
        }

        var path = Path.Combine(_rootPath, id.ToString("D"), Path.GetFileName(fileName));
        return File.Exists(path) ? File.ReadAllBytes(path) : null;
    }
}

public class InMemoryDocumentContentStore : IDocumentContentStore
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<DocumentContentPart>> _documents = new();

    public virtual string Save(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts)
    {
        var id = DocumentContentLocations.ParseId(sourceDocumentId)
            ?? throw new ArgumentException("Source document identifiers must be GUIDs.", nameof(sourceDocumentId));
        _documents[id] = parts.ToList();
        return DocumentContentLocations.For(id.ToString("D"));
    }

    public bool Exists(string sourceDocumentId) =>
        DocumentContentLocations.ParseId(sourceDocumentId) is { } id && _documents.ContainsKey(id);

    public IReadOnlyList<DocumentContentPart>? Get(string sourceDocumentId) =>
        DocumentContentLocations.ParseId(sourceDocumentId) is { } id && _documents.TryGetValue(id, out var parts) ? parts : null;

    public byte[]? Read(string sourceDocumentId, string fileName) =>
        Get(sourceDocumentId)?.FirstOrDefault(part => string.Equals(part.FileName, Path.GetFileName(fileName), StringComparison.Ordinal))?.Content;
}
