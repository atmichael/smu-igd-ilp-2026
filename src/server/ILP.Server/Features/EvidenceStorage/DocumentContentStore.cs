using System.Collections.Concurrent;
using MySqlConnector;

namespace ILP.Server.Features.EvidenceStorage;

public sealed record DocumentContentPart(string FileName, byte[] Content, string ContentType);

/// <summary>Protected storage for original document files; evidence documents point at it through <c>storageLocation</c>.</summary>
public interface IDocumentContentStore
{
    /// <summary>Stores every part or nothing, and returns the document's storage location.</summary>
    Task<string> SaveAsync(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string sourceDocumentId, CancellationToken cancellationToken = default);

    /// <summary>Returns one stored part, or null when the document or part does not exist.</summary>
    Task<byte[]?> ReadAsync(string sourceDocumentId, string fileName, CancellationToken cancellationToken = default);
}

public static class DocumentContentLocations
{
    private const string Prefix = "protected://source-documents/";

    public static string For(string sourceDocumentId) => Prefix + sourceDocumentId;

    // Only GUID ids are accepted, which keeps route and request values out of file paths and queries.
    public static Guid? ParseId(string sourceDocumentId) =>
        Guid.TryParse(sourceDocumentId, out var id) ? id : null;

    public static string RequireId(string sourceDocumentId) =>
        (ParseId(sourceDocumentId) ?? throw new ArgumentException("Source document identifiers must be GUIDs.", nameof(sourceDocumentId))).ToString("D");
}

/// <summary>Keeps document files as LONGBLOB rows in MySQL, one row per stored part.</summary>
public sealed class MySqlDocumentContentStore : IDocumentContentStore
{
    private const string CreateTableSql = """
        CREATE TABLE IF NOT EXISTS source_document_content (
            source_document_id CHAR(36) NOT NULL,
            file_name VARCHAR(64) NOT NULL,
            content_type VARCHAR(100) NOT NULL,
            size_bytes INT NOT NULL,
            content LONGBLOB NOT NULL,
            created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
            PRIMARY KEY (source_document_id, file_name)
        ) ENGINE=InnoDB
        """;

    private readonly string _connectionString;
    private readonly SemaphoreSlim _schemaGate = new(1, 1);
    private volatile bool _schemaReady;

    public MySqlDocumentContentStore(string connectionString)
    {
        _connectionString = connectionString;
    }

    public async Task<string> SaveAsync(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts, CancellationToken cancellationToken = default)
    {
        var id = DocumentContentLocations.RequireId(sourceDocumentId);
        await using var connection = await OpenAsync(cancellationToken);
        await using var transaction = await connection.BeginTransactionAsync(cancellationToken);

        foreach (var part in parts)
        {
            await using var command = new MySqlCommand(
                """
                INSERT INTO source_document_content (source_document_id, file_name, content_type, size_bytes, content)
                VALUES (@id, @fileName, @contentType, @sizeBytes, @content)
                """,
                connection,
                transaction);
            command.Parameters.AddWithValue("@id", id);
            command.Parameters.AddWithValue("@fileName", Path.GetFileName(part.FileName));
            command.Parameters.AddWithValue("@contentType", part.ContentType);
            command.Parameters.AddWithValue("@sizeBytes", part.Content.Length);
            command.Parameters.Add("@content", MySqlDbType.LongBlob).Value = part.Content;
            await command.ExecuteNonQueryAsync(cancellationToken);
        }

        await transaction.CommitAsync(cancellationToken);
        return DocumentContentLocations.For(id);
    }

    public async Task<bool> ExistsAsync(string sourceDocumentId, CancellationToken cancellationToken = default)
    {
        if (DocumentContentLocations.ParseId(sourceDocumentId) is not { } id)
        {
            return false;
        }

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            "SELECT 1 FROM source_document_content WHERE source_document_id = @id LIMIT 1", connection);
        command.Parameters.AddWithValue("@id", id.ToString("D"));
        return await command.ExecuteScalarAsync(cancellationToken) is not null;
    }

    public async Task<byte[]?> ReadAsync(string sourceDocumentId, string fileName, CancellationToken cancellationToken = default)
    {
        if (DocumentContentLocations.ParseId(sourceDocumentId) is not { } id)
        {
            return null;
        }

        await using var connection = await OpenAsync(cancellationToken);
        await using var command = new MySqlCommand(
            "SELECT content FROM source_document_content WHERE source_document_id = @id AND file_name = @fileName", connection);
        command.Parameters.AddWithValue("@id", id.ToString("D"));
        command.Parameters.AddWithValue("@fileName", Path.GetFileName(fileName));
        return await command.ExecuteScalarAsync(cancellationToken) as byte[];
    }

    private async Task<MySqlConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new MySqlConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken);
            await EnsureSchemaAsync(connection, cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    private async Task EnsureSchemaAsync(MySqlConnection connection, CancellationToken cancellationToken)
    {
        if (_schemaReady)
        {
            return;
        }

        await _schemaGate.WaitAsync(cancellationToken);
        try
        {
            if (!_schemaReady)
            {
                await using var command = new MySqlCommand(CreateTableSql, connection);
                await command.ExecuteNonQueryAsync(cancellationToken);
                _schemaReady = true;
            }
        }
        finally
        {
            _schemaGate.Release();
        }
    }
}

public sealed class FileDocumentContentStore : IDocumentContentStore
{
    private readonly string _rootPath;

    public FileDocumentContentStore(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public async Task<string> SaveAsync(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts, CancellationToken cancellationToken = default)
    {
        var id = DocumentContentLocations.RequireId(sourceDocumentId);
        var finalPath = Path.Combine(_rootPath, id);
        var tempPath = finalPath + ".tmp";

        try
        {
            Directory.CreateDirectory(tempPath);
            foreach (var part in parts)
            {
                await File.WriteAllBytesAsync(Path.Combine(tempPath, Path.GetFileName(part.FileName)), part.Content, cancellationToken);
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

        return DocumentContentLocations.For(id);
    }

    public Task<bool> ExistsAsync(string sourceDocumentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(DocumentContentLocations.ParseId(sourceDocumentId) is { } id
            && Directory.Exists(Path.Combine(_rootPath, id.ToString("D"))));

    public async Task<byte[]?> ReadAsync(string sourceDocumentId, string fileName, CancellationToken cancellationToken = default)
    {
        if (DocumentContentLocations.ParseId(sourceDocumentId) is not { } id)
        {
            return null;
        }

        var path = Path.Combine(_rootPath, id.ToString("D"), Path.GetFileName(fileName));
        return File.Exists(path) ? await File.ReadAllBytesAsync(path, cancellationToken) : null;
    }
}

public class InMemoryDocumentContentStore : IDocumentContentStore
{
    private readonly ConcurrentDictionary<Guid, IReadOnlyList<DocumentContentPart>> _documents = new();

    public virtual Task<string> SaveAsync(string sourceDocumentId, IReadOnlyList<DocumentContentPart> parts, CancellationToken cancellationToken = default)
    {
        var id = DocumentContentLocations.RequireId(sourceDocumentId);
        _documents[Guid.Parse(id)] = parts.ToList();
        return Task.FromResult(DocumentContentLocations.For(id));
    }

    public Task<bool> ExistsAsync(string sourceDocumentId, CancellationToken cancellationToken = default) =>
        Task.FromResult(DocumentContentLocations.ParseId(sourceDocumentId) is { } id && _documents.ContainsKey(id));

    public IReadOnlyList<DocumentContentPart>? Get(string sourceDocumentId) =>
        DocumentContentLocations.ParseId(sourceDocumentId) is { } id && _documents.TryGetValue(id, out var parts) ? parts : null;

    public Task<byte[]?> ReadAsync(string sourceDocumentId, string fileName, CancellationToken cancellationToken = default) =>
        Task.FromResult(Get(sourceDocumentId)?.FirstOrDefault(part => string.Equals(part.FileName, Path.GetFileName(fileName), StringComparison.Ordinal))?.Content);
}
