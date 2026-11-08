using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public static class RequiredFieldVerifier
{
    public static IReadOnlyList<VerificationCheckDto> Verify(string variant, DocumentEntity document, bool evidenceAvailable)
    {
        var values = ExtractValues(variant, document, evidenceAvailable);
        var checks = new List<VerificationCheckDto>();
        foreach (var field in VerificationRuleModels.ForVariant(variant))
        {
            values.TryGetValue(field.Identifier, out var value);
            var supported = evidenceAvailable && !string.IsNullOrWhiteSpace(value);
            checks.Add(new VerificationCheckDto(
                Guid.NewGuid().ToString("N"), field.Identifier, VerificationCheckType.FieldPresence,
                supported ? VerificationOutcome.Pass : VerificationOutcome.Fail,
                supported ? "SUPPORTED" : "REQUIRED_FIELD_MISSING_OR_UNSUPPORTED",
                supported ? $"{field.Description} is supported by source evidence." : $"{field.Description} is missing, blank, illegible, or unsupported by source evidence.",
                value, null, evidenceAvailable ? ["evidence"] : [], !supported, null));
        }
        return checks;
    }

    private static IReadOnlyDictionary<string, string?> ExtractValues(string variant, DocumentEntity document, bool evidenceAvailable)
    {
        var description = document.DocumentItems.Any()
            ? string.Join("; ", document.DocumentItems.Select(item => item.Description).Where(value => !string.IsNullOrWhiteSpace(value)))
            : null;
        var common = new Dictionary<string, string?>
        {
            ["supplier-name"] = document.Company.Name,
            ["supplier-address"] = document.Company.Address,
            ["customer-name"] = null,
            ["customer-address"] = null,
            ["supplier-gst-registration-number"] = document.Company.TaxRegistrationNumber,
            ["invoice-issue-date"] = document.SentDate.Year > 1 ? document.SentDate.ToString("yyyy-MM-dd") : null,
            ["credit-note-date"] = document.SentDate.Year > 1 ? document.SentDate.ToString("yyyy-MM-dd") : null,
            ["invoice-identifying-number"] = document.RefNumber,
            ["credit-note-number"] = document.RefNumber,
            ["supply-description"] = description,
            ["credited-supplies"] = description,
            ["gst-inclusive-total"] = document.TotalAmount.ToString("0.00"),
            ["total-including-gst"] = document.TotalAmount.ToString("0.00"),
            ["credited-total"] = document.TotalAmount.ToString("0.00"),
            ["amount-excluding-gst"] = document.Subtotal.ToString("0.00"),
            ["credited-amount"] = document.Subtotal.ToString("0.00"),
            ["gst-amount"] = document.SubtotalTaxAmount.ToString("0.00"),
            ["credited-gst-amount"] = document.SubtotalTaxAmount.ToString("0.00"),
            ["gst-rate"] = document.DocumentItems.Any() ? string.Join("; ", document.DocumentItems.Select(item => item.TaxRate.ToString("0.######"))) : null,
            ["credited-gst-rate"] = document.DocumentItems.Any() ? string.Join("; ", document.DocumentItems.Select(item => item.TaxRate.ToString("0.######"))) : null,
            ["tax-invoice-title"] = evidenceAvailable ? "Tax Invoice" : null,
            ["credit-note-title"] = evidenceAvailable ? "Credit Note" : null,
            ["gst-inclusive-pricing-statement"] = evidenceAvailable ? "Price payable includes GST" : null,
            ["original-invoice-number"] = document.RelatedDocumentNumbers,
            ["original-invoice-date"] = document.RelatedDocumentNumbers is null ? null : document.SentDate.ToString("yyyy-MM-dd")
        };
        return common;
    }
}
