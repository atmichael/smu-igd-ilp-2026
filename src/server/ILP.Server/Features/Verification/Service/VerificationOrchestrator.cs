using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using ILP.Server.Features.Verification.Persistence;
using ILP.Shared.Verification.Contracts;
using ILP.Shared.Verification.Enums;

namespace ILP.Server.Features.Verification.Service;

public sealed class VerificationOrchestrator(IVerificationRepository repository)
{
    public VerificationRunDto Verify(DocumentEntity document, string? evidencePackageId, string? originalInvoiceReferenceId)
    {
        var evidenceAvailable = !string.IsNullOrWhiteSpace(evidencePackageId);
        var variant = ResolveVariant(document.TypeCode);
        var checks = new List<VerificationCheckDto>();

        AddRequiredFieldChecks(checks, document, evidenceAvailable);
        AddVariantChecks(checks, variant, document, evidenceAvailable, originalInvoiceReferenceId);
        AddArithmeticChecks(checks, document, evidenceAvailable);
        if (variant is "CreditNote") AddCreditNoteChecks(checks, document, originalInvoiceReferenceId, evidenceAvailable);
        checks.Add(ZeroRatedSupplyVerifier.Verify(document, evidenceAvailable));
        checks.Add(MixedSupplyVerifier.Verify(document, evidenceAvailable));
        if (!string.IsNullOrWhiteSpace(document.Company.Name))
            checks.Add(VendorIdentityVerifier.Verify(document.Company.Name, [document.Company.Name], [], evidenceAvailable));

        var status = checks.Any(c => c.Outcome is VerificationOutcome.Fail or VerificationOutcome.Unavailable)
            ? VerificationRunStatus.Blocked
            : VerificationRunStatus.Verified;

        var run = new VerificationRunDto(
            Guid.NewGuid().ToString("N"), document.Id.ToString(), variant, status,
            "iras-sg-invoice-rules-v1", evidencePackageId, checks, DateTimeOffset.UtcNow);
        return repository.SaveRun(run);
    }

    private static string ResolveVariant(string typeCode)
    {
        if (typeCode.Contains("credit", StringComparison.OrdinalIgnoreCase)) return "CreditNote";
        if (typeCode.Contains("simplified", StringComparison.OrdinalIgnoreCase)) return "SimplifiedTaxInvoice";
        if (typeCode.Contains("debit", StringComparison.OrdinalIgnoreCase)) return "DebitNote";
        if (typeCode.Contains("sales", StringComparison.OrdinalIgnoreCase) && typeCode.Contains("voucher", StringComparison.OrdinalIgnoreCase)) return "SalesVoucher";
        return "FullTaxInvoice";
    }

    private static void AddRequiredFieldChecks(List<VerificationCheckDto> checks, DocumentEntity document, bool evidenceAvailable)
    {
        var variant = ResolveVariant(document.TypeCode);
        checks.AddRange(RequiredFieldVerifier.Verify(variant, document, evidenceAvailable));
    }

    private static void AddVariantChecks(List<VerificationCheckDto> checks, string variant, DocumentEntity document, bool evidenceAvailable, string? originalInvoiceReferenceId)
    {
        if (variant is "SimplifiedTaxInvoice")
            checks.Add(SimplifiedTaxInvoiceVerifier.VerifyThreshold(document, evidenceAvailable));
        if (variant is "DebitNote" or "SalesVoucher")
            checks.Add(SimplifiedDocumentVerifier.VerifyVariant(variant, document, evidenceAvailable));
        if (variant is "CreditNote")
        {
            checks.Add(CreditNoteVerifier.Verify(document, evidenceAvailable));
            checks.Add(OriginalInvoiceAdjustmentVerifier.Verify(document, originalInvoiceReferenceId, evidenceAvailable));
        }
    }

    private static void AddArithmeticChecks(List<VerificationCheckDto> checks, DocumentEntity document, bool evidenceAvailable)
    {
        if (!evidenceAvailable)
        {
            checks.Add(Check("gst-calculation", VerificationCheckType.Arithmetic, false, null, [], "GST calculation is unavailable because source evidence is not attached."));
            return;
        }
        var calculatedGst = VerificationCalculationPolicy.SubtotalGst(document.Subtotal, document.SubtotalTaxRate);
        var gstMatches = VerificationCalculationPolicy.MatchesCent(document.SubtotalTaxAmount, calculatedGst);
        var gstTrace = new ILP.Shared.Verification.Contracts.VerificationCalculationTraceDto(document.Subtotal, document.SubtotalTaxRate, calculatedGst, document.SubtotalTaxAmount, null, null, "nearest-cent", null, "SGD", null, null, null, null);
        checks.Add(Check("gst-calculation", VerificationCheckType.Arithmetic, gstMatches,
            document.SubtotalTaxAmount.ToString("0.00"), ["evidence"],
            gstMatches ? "Displayed GST matches the approved subtotal calculation." : $"Displayed GST does not match calculated GST {calculatedGst.ToString("0.00")}.", calculatedGst.ToString("0.00"), gstTrace));

        var totalMatches = VerificationCalculationPolicy.MatchesCent(document.TotalAmount, VerificationCalculationPolicy.RoundCent(document.Subtotal + document.SubtotalTaxAmount));
        checks.Add(Check("total-calculation", VerificationCheckType.Arithmetic, totalMatches,
            document.TotalAmount.ToString("0.00"), ["evidence"],
            totalMatches ? "Displayed total matches taxable amount plus GST." : "Displayed total does not match taxable amount plus GST.", VerificationCalculationPolicy.RoundCent(document.Subtotal + document.SubtotalTaxAmount).ToString("0.00")));
    }

    private static void AddCreditNoteChecks(List<VerificationCheckDto> checks, DocumentEntity document, string? originalInvoiceReferenceId, bool evidenceAvailable)
    {
        var adjustmentSupported = evidenceAvailable && !string.IsNullOrWhiteSpace(originalInvoiceReferenceId);
        checks.Add(Check("original-invoice-adjustment", VerificationCheckType.Adjustment, adjustmentSupported,
            document.RelatedDocumentNumbers, ["evidence"],
            adjustmentSupported ? "Credit note adjustment has supported original-invoice evidence." : "Credit note adjustment has no supported original-invoice evidence."));
    }

    private static VerificationCheckDto Check(string field, VerificationCheckType type, bool passed, string? extracted,
        string[] evidence, string message, string? expected = null, VerificationCalculationTraceDto? calculationTrace = null)
    {
        var outcome = passed ? VerificationOutcome.Pass : VerificationOutcome.Fail;
        return new VerificationCheckDto(Guid.NewGuid().ToString("N"), field, type, outcome, passed ? "SUPPORTED" : "EVIDENCE_OR_VALUE_INVALID",
            message, extracted, expected, evidence, !passed, calculationTrace);
    }
}
