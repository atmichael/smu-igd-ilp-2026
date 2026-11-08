using ILP.Shared.ReferenceData;
using ILP.Server.Features.ReferenceData.Adapters;
using ILP.Server.Features.ReferenceData.Repository;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class ReferenceDataSynchronizationService(IEnumerable<IReferenceDataSourceAdapter> adapters, IReferenceDataRepository repository, ReferenceDataSnapshotValidator validator, TimeProvider timeProvider)
{
    public async Task<SynchronizationRunDto> SynchronizeAsync(string source, string triggerType, CancellationToken cancellationToken = default)
    {
        var adapter = adapters.FirstOrDefault(x => string.Equals(x.Source, source, StringComparison.OrdinalIgnoreCase))
            ?? throw new KeyNotFoundException($"Unknown reference source {source}.");
        var now = timeProvider.GetUtcNow();
        var run = new SynchronizationRunDto(Guid.NewGuid().ToString("D"), source, triggerType, "Started", now, null, null, null);
        repository.SaveRun(run);
        try
        {
            var records = await adapter.ReadRecordsAsync(cancellationToken);
            var snapshot = new ReferenceSnapshotDto(Guid.NewGuid().ToString("D"), "Approved", source, now, Guid.NewGuid().ToString("D"), records, []);
            var validationErrors = validator.Validate(snapshot);
            if (validationErrors.Count > 0)
            {
                run = run with { Status = "Failed", CompletedAt = timeProvider.GetUtcNow(), Error = string.Join("; ", validationErrors) };
                return repository.SaveRun(run);
            }
            repository.Publish(snapshot);
            run = run with { Status = "Published", CompletedAt = timeProvider.GetUtcNow(), PublishedSnapshotId = snapshot.SnapshotId };
            return repository.SaveRun(run);
        }
        catch (Exception exception)
        {
            run = run with { Status = "Failed", CompletedAt = timeProvider.GetUtcNow(), Error = exception.Message };
            return repository.SaveRun(run);
        }
    }
}
