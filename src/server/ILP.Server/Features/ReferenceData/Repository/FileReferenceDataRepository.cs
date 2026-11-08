using System.Text.Json;
using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Repository;

public sealed class FileReferenceDataRepository : IReferenceDataRepository
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web) { WriteIndented = true };
    private readonly object _gate = new();
    private readonly string _rootPath;

    public FileReferenceDataRepository(string rootPath)
    {
        _rootPath = Path.GetFullPath(rootPath);
        Directory.CreateDirectory(Path.Combine(_rootPath, "snapshots"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "runs"));
        Directory.CreateDirectory(Path.Combine(_rootPath, "traces"));
    }

    public ReferenceSnapshotDto? GetCurrentSnapshot(string source)
    {
        var currentPath = Path.Combine(_rootPath, "snapshots", Uri.EscapeDataString(source) + ".current.json");
        return File.Exists(currentPath) ? Read<ReferenceSnapshotDto>(currentPath) : null;
    }

    public IReadOnlyList<ReferenceSnapshotDto> GetSnapshots(string source)
    {
        var prefix = Uri.EscapeDataString(source) + ".";
        return Directory.EnumerateFiles(Path.Combine(_rootPath, "snapshots"), "*.json")
            .Where(path => Path.GetFileName(path).StartsWith(prefix, StringComparison.OrdinalIgnoreCase) && !path.EndsWith(".current.json", StringComparison.OrdinalIgnoreCase))
            .Select(Read<ReferenceSnapshotDto>).OrderByDescending(x => x.PublishedAt).ToList();
    }

    public void Publish(ReferenceSnapshotDto snapshot)
    {
        lock (_gate)
        {
            var escapedSource = Uri.EscapeDataString(snapshot.Source);
            var snapshotPath = Path.Combine(_rootPath, "snapshots", $"{escapedSource}.{snapshot.SnapshotId}.json");
            var currentPath = Path.Combine(_rootPath, "snapshots", escapedSource + ".current.json");
            var stagedPath = snapshotPath + ".tmp";
            WriteAtomically(stagedPath, snapshotPath, snapshot);
            WriteAtomically(currentPath + ".tmp", currentPath, snapshot);
        }
    }

    public SynchronizationRunDto SaveRun(SynchronizationRunDto run)
    {
        lock (_gate)
        {
            var path = Path.Combine(_rootPath, "runs", Uri.EscapeDataString(run.RunId) + ".json");
            WriteAtomically(path + ".tmp", path, run);
        }
        return run;
    }

    public IReadOnlyList<SynchronizationRunDto> GetRuns(string source) =>
        Directory.EnumerateFiles(Path.Combine(_rootPath, "runs"), "*.json")
            .Select(Read<SynchronizationRunDto>)
            .Where(run => string.Equals(run.Source, source, StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(run => run.StartedAt).ToList();

    public void SaveTrace(RuleDecisionTraceDto trace)
    {
        lock (_gate)
        {
            var path = Path.Combine(_rootPath, "traces", Uri.EscapeDataString(trace.TraceId) + ".json");
            WriteAtomically(path + ".tmp", path, trace);
        }
    }

    public RuleDecisionTraceDto? GetTrace(string traceId)
    {
        var path = Path.Combine(_rootPath, "traces", Uri.EscapeDataString(traceId) + ".json");
        return File.Exists(path) ? Read<RuleDecisionTraceDto>(path) : null;
    }

    private static void WriteAtomically<T>(string temporaryPath, string finalPath, T value)
    {
        File.WriteAllText(temporaryPath, JsonSerializer.Serialize(value, JsonOptions));
        File.Move(temporaryPath, finalPath, overwrite: true);
    }

    private static T Read<T>(string path) =>
        JsonSerializer.Deserialize<T>(File.ReadAllText(path), JsonOptions) ?? throw new InvalidDataException($"Reference data file {path} is empty.");
}
