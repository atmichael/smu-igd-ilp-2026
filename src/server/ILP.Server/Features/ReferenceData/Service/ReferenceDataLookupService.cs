using ILP.Shared.ReferenceData;
using ILP.Server.Features.ReferenceData.Repository;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class ReferenceDataLookupService(IReferenceDataRepository repository)
{
    public ReferenceLookupResult Lookup(string source, string category, string identity, DateOnly effectiveDate)
    {
        var snapshot = repository.GetCurrentSnapshot(source);
        if (snapshot is null)
            return ReferenceLookupResult.Unverified(ReferenceDataOutcomeReason.SourceUnavailable, "Reference source has no approved synchronized snapshot.");

        var canonical = snapshot.Aliases.FirstOrDefault(x => string.Equals(x.AliasValue, identity, StringComparison.OrdinalIgnoreCase))?.CanonicalIdentity ?? identity;
        var matches = snapshot.Records.Where(x =>
            string.Equals(x.Category, category, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.Identity, canonical, StringComparison.OrdinalIgnoreCase) &&
            x.EffectiveStart <= effectiveDate && (x.EffectiveEnd is null || x.EffectiveEnd >= effectiveDate)).ToList();

        if (matches.Count == 0)
            return ReferenceLookupResult.Unverified(ReferenceDataOutcomeReason.MissingReference, "No approved reference record covers the requested effective date.");
        if (matches.Count > 1)
            return ReferenceLookupResult.Unverified(ReferenceDataOutcomeReason.ConflictingReference, "Multiple approved records cover the requested effective date.");
        return new ReferenceLookupResult(matches[0], snapshot, null, null);
    }
}

public sealed record ReferenceLookupResult(ReferenceDataRecordDto? Record, ReferenceSnapshotDto? Snapshot, string? ReasonCode, string? RecoveryGuidance)
{
    public static ReferenceLookupResult Unverified(string reasonCode, string guidance) => new(null, null, reasonCode, guidance);
}
