using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class ReferenceDataSnapshotValidator
{
    public IReadOnlyList<string> Validate(ReferenceSnapshotDto snapshot)
    {
        var errors = new List<string>();
        if (string.IsNullOrWhiteSpace(snapshot.SnapshotId)) errors.Add("Snapshot identifier is required.");
        if (string.IsNullOrWhiteSpace(snapshot.Source)) errors.Add("Snapshot source is required.");
        if (snapshot.Records.Select(x => x.ReferenceDataRecordId).Distinct().Count() != snapshot.Records.Count)
            errors.Add("Reference records contain duplicate identifiers.");
        if (snapshot.Aliases.GroupBy(x => x.AliasValue, StringComparer.OrdinalIgnoreCase).Any(g => g.Select(x => x.CanonicalIdentity).Distinct(StringComparer.OrdinalIgnoreCase).Count() > 1))
            errors.Add("An alias conflicts with multiple canonical identities.");
        foreach (var record in snapshot.Records)
        {
            if (record.EffectiveEnd is { } end && end < record.EffectiveStart)
                errors.Add($"Reference {record.ReferenceDataRecordId} has an invalid effective period.");
        }
        return errors;
    }
}
