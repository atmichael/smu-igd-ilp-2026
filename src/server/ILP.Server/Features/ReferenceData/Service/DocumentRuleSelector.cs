using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class DocumentRuleSelector
{
    private static readonly HashSet<string> SupportedVariants = new(StringComparer.OrdinalIgnoreCase)
    {
        "FullTaxInvoice", "SimplifiedTaxInvoice", "Receipt", "DebitNote", "SalesVoucher", "CreditNote"
    };

    public string ValidateVariant(string? documentVariant)
    {
        if (string.IsNullOrWhiteSpace(documentVariant) || !SupportedVariants.Contains(documentVariant))
            return ReferenceDataOutcomeReason.AmbiguousVariant;
        return string.Empty;
    }

    public BusinessRulePackageDto? Select(IReadOnlyList<BusinessRulePackageDto> packages, string jurisdiction, string documentVariant, DateOnly effectiveDate)
    {
        var matches = packages.Where(x =>
            string.Equals(x.Jurisdiction, jurisdiction, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(x.DocumentVariant, documentVariant, StringComparison.OrdinalIgnoreCase) &&
            x.EffectiveStart <= effectiveDate && (x.EffectiveEnd is null || x.EffectiveEnd >= effectiveDate)).ToList();
        return matches.Count == 1 ? matches[0] : null;
    }
}
