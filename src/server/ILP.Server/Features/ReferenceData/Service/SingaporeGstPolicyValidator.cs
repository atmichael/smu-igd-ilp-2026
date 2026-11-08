using ILP.Shared.ReferenceData;

namespace ILP.Server.Features.ReferenceData.Service;

public sealed class SingaporeGstPolicyValidator
{
    public IReadOnlyList<string> Validate(BusinessRulePackageDto package)
    {
        var errors = new List<string>();
        var required = new[] { "GstRate", "SimplifiedInvoiceThresholdSgd", "RequiredFields", "RoundingMethod", "ForeignCurrencyConversionMethod", "CreditNoteAdjustment" };
        foreach (var key in required)
            if (!package.RuleValues.ContainsKey(key) || string.IsNullOrWhiteSpace(package.RuleValues[key])) errors.Add($"Missing {key} in Singapore GST policy package.");
        if (package.RuleValues.TryGetValue("SimplifiedInvoiceThresholdSgd", out var threshold) && threshold != "1000")
            errors.Add("Simplified-invoice threshold must be S$1,000.");
        return errors;
    }
}
