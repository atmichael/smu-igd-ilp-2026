using System.Collections.Concurrent;
using System.Text.Json;
using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Non-durable repository for tests; stores serialized snapshots to mirror the file store's copy semantics.</summary>
public class InMemoryEvidenceRepository : IEvidenceRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
    private readonly ConcurrentDictionary<string, string> _packages = new(StringComparer.OrdinalIgnoreCase);

    public EvidencePackage? Get(string evidencePackageId) =>
        _packages.TryGetValue(evidencePackageId, out var json)
            ? JsonSerializer.Deserialize<EvidencePackage>(json, JsonOptions)
            : null;

    public IReadOnlyList<EvidencePackage> List() =>
        _packages.Values.Select(json => JsonSerializer.Deserialize<EvidencePackage>(json, JsonOptions)!).ToList();

    public virtual void Save(EvidencePackage package) =>
        _packages[package.EvidencePackageId] = JsonSerializer.Serialize(package, JsonOptions);
}
