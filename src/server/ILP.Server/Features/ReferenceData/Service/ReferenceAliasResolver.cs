using ILP.Shared.ReferenceData;
using ILP.Server.Features.ReferenceData.Repository;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class ReferenceAliasResolver(IReferenceDataRepository repository)
{
    public string? Resolve(string source, string value)
    {
        var snapshot = repository.GetCurrentSnapshot(source);
        var aliases = snapshot?.Aliases
            .Where(x => string.Equals(x.AliasValue, value, StringComparison.OrdinalIgnoreCase))
            .Select(x => x.CanonicalIdentity)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList() ?? [];
        return aliases.Count == 1 ? aliases[0] : null;
    }
}
