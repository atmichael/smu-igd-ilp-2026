using ILP.Shared.Evidence;
using Microsoft.Extensions.Options;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>
/// Applies the pilot retention default (3 years from finalization). The production retention period
/// must be confirmed by policy review before go-live; deletion is intentionally not automated here.
/// </summary>
public sealed class RetentionRules
{
    public const string PilotPolicyName = "pilot-3-year";

    private static readonly ReviewStatus[] ArchivableStatuses = [ReviewStatus.Confirmed, ReviewStatus.Superseded];
    private readonly int _retentionYears;

    public RetentionRules(IOptions<EvidenceStorageOptions> options)
    {
        _retentionYears = options.Value.RetentionYears;
    }

    public DateTimeOffset RetentionUntil(DateTimeOffset finalizedAt) => finalizedAt.AddYears(_retentionYears);

    public void EnsureCanArchive(EvidencePackage package, DateTimeOffset now)
    {
        if (!ArchivableStatuses.Contains(package.ReviewStatus))
        {
            throw new EvidencePreconditionException(
                $"Only confirmed or superseded evidence can be archived; package is '{WireEnum.ToWire(package.ReviewStatus)}'.");
        }

        if (package.RetentionUntil is not { } retentionUntil || now < retentionUntil)
        {
            throw new EvidencePreconditionException(
                $"Retention period has not elapsed; package is retained until {package.RetentionUntil:O}.");
        }
    }

    /// <summary>Returns a copy safe for retrieval: archived evidence keeps metadata and lineage structure but withholds content and values.</summary>
    public static EvidencePackage ApplyAccessRestrictions(EvidencePackage package)
    {
        if (package.ReviewStatus != ReviewStatus.Archived)
        {
            return package;
        }

        package.ContentRestricted = true;
        foreach (var document in package.Documents)
        {
            document.StorageLocation = null;
            foreach (var record in document.Records)
            {
                record.RawValue = null;
                record.CurrentValue = null;
                foreach (var entry in record.Provenance)
                {
                    entry.PreviousValue = null;
                    entry.NewValue = null;
                }
            }
        }

        return package;
    }
}
