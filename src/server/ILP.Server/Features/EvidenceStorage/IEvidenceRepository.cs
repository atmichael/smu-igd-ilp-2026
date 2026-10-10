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

    /// <summary><c>File</c> (durable, default) or <c>InMemory</c> (tests only, also keeps document files in memory).</summary>
    public string Provider { get; set; } = "File";

    /// <summary>Where original document files go: <c>MySql</c> (target, connection string <c>IlpDatabase</c>) or <c>File</c> (local folder fallback).</summary>
    public string ContentProvider { get; set; } = "File";

    public string RootPath { get; set; } = "App_Data/evidence-store";

    /// <summary>Local folder for document files when <see cref="ContentProvider"/> is <c>File</c>; one folder per source document.</summary>
    public string ContentRootPath { get; set; } = "App_Data/source-documents";

    /// <summary>Where intake source-document records are kept until they move to the database.</summary>
    public string SourceDocumentRecordsPath { get; set; } = "App_Data/source-document-records";

    public int RetentionYears { get; set; } = 3;
}
