using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Durable store for evidence packages. Implementations return detached copies so callers cannot mutate stored state without <see cref="Save"/>.</summary>
public interface IEvidenceRepository
{
    EvidencePackage? Get(string evidencePackageId);

    IReadOnlyList<EvidencePackage> List();

    void Save(EvidencePackage package);
}

public sealed class EvidenceStorageOptions
{
    public const string SectionName = "EvidenceStorage";

    /// <summary><c>File</c> (durable, default) or <c>InMemory</c> (tests only).</summary>
    public string Provider { get; set; } = "File";

    public string RootPath { get; set; } = "App_Data/evidence-store";

    public int RetentionYears { get; set; } = 3;
}
