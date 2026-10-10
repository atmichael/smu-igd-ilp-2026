using System.Text.Json;
using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Durable repository persisting one JSON document per evidence package with atomic replace-on-write.</summary>
public sealed class FileEvidenceRepository : IEvidenceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly string _rootPath;

    public FileEvidenceRepository(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(_rootPath);
    }

    public EvidencePackage? Get(string evidencePackageId)
    {
        var path = ResolvePath(evidencePackageId);
        return path is not null && File.Exists(path) ? Read(path) : null;
    }

    public IReadOnlyList<EvidencePackage> List() =>
        Directory.EnumerateFiles(_rootPath, "*.json").Select(Read).ToList();

    public void Save(EvidencePackage package)
    {
        var path = ResolvePath(package.EvidencePackageId)
            ?? throw new InvalidOperationException("Evidence package identifiers must be GUIDs.");

        var tempPath = path + ".tmp";
        File.WriteAllText(tempPath, JsonSerializer.Serialize(package, JsonOptions));
        File.Move(tempPath, path, overwrite: true);
    }

    // Only GUID ids map to file names, which prevents path traversal through route values.
    private string? ResolvePath(string evidencePackageId) =>
        Guid.TryParse(evidencePackageId, out var id) ? Path.Combine(_rootPath, $"{id:D}.json") : null;

    private static EvidencePackage Read(string path) =>
        JsonSerializer.Deserialize<EvidencePackage>(File.ReadAllText(path), JsonOptions)
            ?? throw new InvalidDataException("Evidence package file is empty.");
}
