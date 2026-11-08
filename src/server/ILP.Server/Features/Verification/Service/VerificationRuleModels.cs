namespace ILP.Server.Features.Verification.Service;

public sealed record VerificationRequiredField(string Identifier, string Description);

public static class VerificationRuleModels
{
    public static readonly IReadOnlyList<VerificationRequiredField> SimplifiedTaxInvoice =
    [
        new("supplier-name", "Supplier name"),
        new("supplier-address", "Supplier address"),
        new("supplier-gst-registration-number", "Supplier GST registration number"),
        new("invoice-issue-date", "Invoice issue date"),
        new("invoice-identifying-number", "Invoice identifying number"),
        new("supply-description", "Description of goods or services supplied"),
        new("gst-inclusive-total", "Total amount payable including GST"),
        new("gst-inclusive-pricing-statement", "GST-inclusive pricing statement")
    ];

    public static IReadOnlyList<VerificationRequiredField> ForVariant(string documentVariant) => documentVariant switch
    {
        "SimplifiedTaxInvoice" => SimplifiedTaxInvoice,
        "CreditNote" =>
        [
            new("credit-note-title", "Prominent credit-note title"),
            new("credit-note-number", "Credit-note number"),
            new("credit-note-date", "Credit-note date"),
            new("supplier-name", "Supplier name and address"),
            new("customer-name", "Customer name and address"),
            new("original-invoice-number", "Original invoice number"),
            new("original-invoice-date", "Original invoice date"),
            new("credited-supplies", "Detailed credited supplies"),
            new("credited-amount", "Credited amount"),
            new("credited-gst-rate", "GST rate"),
            new("credited-gst-amount", "Credited GST amount"),
            new("credited-total", "Total credited including GST")
        ],
        _ =>
        [
            new("tax-invoice-title", "Tax-invoice title"),
            new("supplier-name", "Supplier name"),
            new("supplier-address", "Supplier address"),
            new("customer-name", "Customer name"),
            new("customer-address", "Customer address"),
            new("supplier-gst-registration-number", "Supplier GST registration number"),
            new("invoice-issue-date", "Invoice issue date"),
            new("invoice-identifying-number", "Invoice identifying number"),
            new("supply-description", "Supply descriptions"),
            new("gst-rate", "Applicable GST rate"),
            new("amount-excluding-gst", "Amount excluding GST"),
            new("gst-amount", "GST amount"),
            new("total-including-gst", "Total including GST")
        ]
    };
}
