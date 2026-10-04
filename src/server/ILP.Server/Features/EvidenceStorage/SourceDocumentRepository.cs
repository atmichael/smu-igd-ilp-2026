using System.Collections.Concurrent;
using System.Text.Json;
using ILP.Shared.SourceDocuments;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>An accepted intake submission plus the idempotency data needed to recognise retries after a restart.</summary>
public sealed record SourceDocumentRecord(SourceDocumentMetadata Document, string IdempotencyKey, string PayloadHash);

/// <summary>Durable intake records; the original files themselves live in <see cref="IDocumentContentStore"/>.</summary>
public interface ISourceDocumentRepository
{
    SourceDocumentRecord? Get(string sourceDocumentId);

    SourceDocumentRecord? FindByIdempotencyKey(string idempotencyKey);

    void Save(SourceDocumentRecord record);
}

public sealed class FileSourceDocumentRepository : ISourceDocumentRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _rootPath;

    public FileSourceDocumentRepository(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public SourceDocumentRecord? Get(string sourceDocumentId)
    {
        var path = ResolvePath(sourceDocumentId);
        return path is not null && File.Exists(path) ? Read(path) : null;
    }

    // Pilot scale: a linear scan is acceptable until intake records move to the database.
    public SourceDocumentRecord? FindByIdempotencyKey(string idempotencyKey) =>
        Directory.EnumerateFiles(_rootPath, "*.json")
            .Select(Read)
            .FirstOrDefault(record => string.Equals(record.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

    public void Save(SourceDocumentRecord record)
    {
        var path = ResolvePath(record.Document.SourceDocumentId)
            ?? throw new InvalidOperationException("Source document identifiers must be GUIDs.");

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(record, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    private string? ResolvePath(string sourceDocumentId) =>
        DocumentContentLocations.ParseId(sourceDocumentId) is { } id ? Path.Combine(_rootPath, $"{id:D}.json") : null;

    private static SourceDocumentRecord Read(string path) =>
        JsonSerializer.Deserialize<SourceDocumentRecord>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Source document record file is empty.");
}

public sealed class InMemorySourceDocumentRepository : ISourceDocumentRepository
{
    private readonly ConcurrentDictionary<string, SourceDocumentRecord> _records = new(StringComparer.OrdinalIgnoreCase);

    public SourceDocumentRecord? Get(string sourceDocumentId) =>
        _records.TryGetValue(sourceDocumentId, out var record) ? record : null;

    public SourceDocumentRecord? FindByIdempotencyKey(string idempotencyKey) =>
        _records.Values.FirstOrDefault(record => string.Equals(record.IdempotencyKey, idempotencyKey, StringComparison.Ordinal));

    public void Save(SourceDocumentRecord record) => _records[record.Document.SourceDocumentId] = record;
}
