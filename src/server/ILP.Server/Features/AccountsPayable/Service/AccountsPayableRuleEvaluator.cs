using ILP.Server.Features.AccountsPayable.Model;
using ILP.Shared.AccountsPayable.Dto;

namespace ILP.Server.Features.AccountsPayable.Service;

public static class AccountsPayableRuleEvaluator
{
    private const string StandardGstCode = "SG_STD_9";
    private const string UnknownGstCode = "SG_UNKNOWN";
    private const string GstInputAccount = "1200";
    private const string AccountsPayableAccount = "2000";
    private const string ReviewOnlyExpenseAccount = "6130";

    public static AccountsPayableCodingResult Evaluate(
        AccountsPayableInvoiceRequest invoice,
        AccountsPayableCodingRecommendation recommendation,
        IReadOnlyList<AccountsPayableReferenceAccount> accounts,
        IReadOnlyList<AccountsPayableReferenceGstCode> gstCodes)
    {
        var validation = new List<AccountsPayableValidation>();
        var reviewReasons = recommendation.ReviewReasons.ToList();
        var documentTypeValid = IsInvoiceOrCreditNote(invoice.DocumentType);
        AddCheck(validation, reviewReasons, "document_type_supported", documentTypeValid,
            "Only invoices and credit notes can create an AP journal.");

        var invoiceOnly = !IsCreditNote(invoice.DocumentType);
        AddCheck(validation, reviewReasons, "credit_note_original_treatment_available", invoiceOnly,
            "Credit notes require the original invoice coding and tax treatment; they cannot be classified as new expenses.");

        var requiredFieldsValid = HasRequiredFields(invoice);
        AddCheck(validation, reviewReasons, "required_fields_present", requiredFieldsValid,
            "Invoice identity, currency, line data, or source evidence is missing.");

        var accountingPolicyAvailable = invoice.CapitalizationThreshold is > 0m
            && !string.IsNullOrWhiteSpace(invoice.CompanyPolicy);
        AddCheck(validation, reviewReasons, "company_accounting_policy_available", accountingPolicyAvailable,
            "Capitalization threshold or company accounting policy is missing.");

        var currencySupported = string.Equals(invoice.Currency, "SGD", StringComparison.OrdinalIgnoreCase);
        AddCheck(validation, reviewReasons, "currency_supported", currencySupported,
            "Foreign-currency conversion requires separately evidenced SGD amounts and human review.");

        var vendorVerified = invoice.VendorWhitelisted == true
            && invoice.UenMasterVerified == true
            && invoice.VendorBankDetailsChanged == false;
        AddCheck(validation, reviewReasons, "vendor_identity_verified", vendorVerified,
            "Vendor approval, UEN verification, or bank-detail verification is missing or failed.");

        var duplicateCheckPassed = invoice.DuplicateCheckPassed == true;
        AddCheck(validation, reviewReasons, "duplicate_check_passed", duplicateCheckPassed,
            "A duplicate check is missing or a possible duplicate was found.");

        var supportingDocumentsComplete =
            invoice.RequiredDocumentsProfile is "INVOICE_ONLY" or "INVOICE_DO" or "INVOICE_DO_PO" or "INVOICE_PO" or "HUMAN_SIGNATURE"
            && invoice.RequiredDocumentsProfile != "HUMAN_SIGNATURE"
            && invoice.SupportingDocumentsComplete == true;
        AddCheck(validation, reviewReasons, "supporting_documents_complete", supportingDocumentsComplete,
            "Required supplier documents are missing or their profile is unknown.");

        var arithmeticValid = ValidateAmounts(invoice);
        AddCheck(validation, reviewReasons, "invoice_total_reconciles", arithmeticValid,
            "Line, subtotal, tax, and invoice total amounts do not reconcile exactly.");

        var accountMap = accounts.ToDictionary(account => account.Code, StringComparer.OrdinalIgnoreCase);
        var suggestions = new List<AccountsPayableCoaSuggestion>();
        var suggestionsValid = ValidateSuggestions(invoice, recommendation, accountMap, suggestions, reviewReasons);
        AddCheck(validation, reviewReasons, "coa_suggestions_enabled", suggestionsValid,
            "One or more line suggestions are missing, disabled, or require review.");

        var gstCode = recommendation.GstCode.Trim().ToUpperInvariant();
        var gstExists = gstCodes.Any(item => string.Equals(item.Code, gstCode, StringComparison.OrdinalIgnoreCase));
        if (!gstExists)
        {
            reviewReasons.Add($"GST code '{recommendation.GstCode}' is not in the approved reference list.");
        }

        var standardGstSupported = gstCode != StandardGstCode
            || (invoice.CompanyGstRegistered == true
                && invoice.DomesticStandardRatedConfirmed == true
                && invoice.InvoiceGstEvidenceValid == true
                && invoice.InputGstClaimableConfirmed == true);
        var gstEvidenceSupported = invoice.InvoiceGstEvidenceValid == true
            && (gstCode != StandardGstCode || standardGstSupported);
        if (!gstEvidenceSupported)
        {
            reviewReasons.Add("GST treatment is not fully supported by the supplied evidence.");
            gstCode = gstCodes.Any(item => item.Code == UnknownGstCode) ? UnknownGstCode : gstCode;
        }
        else if (gstCode == UnknownGstCode
            || gstCode is "SG_IMPORT_REVIEW" or "SG_BLOCKED" or "SG_OUT")
        {
            reviewReasons.Add($"GST treatment '{gstCode}' requires tax review.");
        }
        else if ((gstCode is "SG_ZERO" or "SG_EXEMPT" or "SG_NO_GST") && invoice.TaxAmount != 0m)
        {
            reviewReasons.Add($"GST treatment '{gstCode}' conflicts with the invoice tax amount.");
            gstEvidenceSupported = false;
        }

        var gstRequiresReview = gstCode is UnknownGstCode or "SG_IMPORT_REVIEW" or "SG_BLOCKED" or "SG_OUT";
        AddCheck(validation, reviewReasons, "gst_code_supported",
            gstExists && gstEvidenceSupported && !gstRequiresReview,
            "GST treatment is unsupported or requires tax review.");

        var enabledApControl = accounts.Any(account =>
            account.Code == AccountsPayableAccount
            && account.Enabled
            && account.ApRole.Equals("AP Control", StringComparison.OrdinalIgnoreCase));
        AddCheck(validation, reviewReasons, "ap_control_account_enabled", enabledApControl,
            "Enabled AP Trade control account 2000 is not available.");

        var enabledGstControl = gstCode != StandardGstCode
            || accounts.Any(account =>
                account.Code == GstInputAccount
                && account.Enabled
                && account.ApRole.Equals("Tax", StringComparison.OrdinalIgnoreCase));
        AddCheck(validation, reviewReasons, "gst_control_account_enabled", enabledGstControl,
            "Enabled GST input-tax control account 1200 is not available.");

        bool journalInputsValid = documentTypeValid
            && invoiceOnly
            && requiredFieldsValid
            && accountingPolicyAvailable
            && currencySupported
            && arithmeticValid
            && suggestionsValid
            && gstExists
            && gstEvidenceSupported
            && !gstRequiresReview
            && enabledApControl
            && enabledGstControl;
        var journal = journalInputsValid
            ? BuildJournal(invoice, suggestions, gstCode)
            : [];
        bool journalBalanced = journal.Count > 0
            && journal.Sum(line => line.Debit) == journal.Sum(line => line.Credit);
        AddCheck(validation, reviewReasons, "journal_balanced", journalBalanced,
            "The draft journal is not balanced.");

        var status = reviewReasons.Count == 0 && validation.All(check => check.Passed)
            ? "READY_FOR_APPROVAL"
            : "NEEDS_REVIEW";
        return new AccountsPayableCodingResult(
            invoice.DocumentId,
            invoice.VendorName,
            invoice.InvoiceNumber,
            invoice.Currency,
            recommendation.Confidence,
            invoice.SourceEvidence,
            gstCode,
            suggestions,
            journal,
            validation,
            reviewReasons.Distinct(StringComparer.Ordinal).ToList(),
            status,
            Posted: false);
    }

    private static bool IsInvoiceOrCreditNote(string? documentType) =>
        string.Equals(documentType, "invoice", StringComparison.OrdinalIgnoreCase)
        || string.Equals(documentType, "credit-note", StringComparison.OrdinalIgnoreCase)
        || string.Equals(documentType, "credit note", StringComparison.OrdinalIgnoreCase);

    private static bool IsCreditNote(string? documentType) =>
        string.Equals(documentType, "credit-note", StringComparison.OrdinalIgnoreCase)
        || string.Equals(documentType, "credit note", StringComparison.OrdinalIgnoreCase);

    private static bool HasRequiredFields(AccountsPayableInvoiceRequest invoice) =>
        !string.IsNullOrWhiteSpace(invoice.DocumentId)
        && !string.IsNullOrWhiteSpace(invoice.VendorName)
        && !string.IsNullOrWhiteSpace(invoice.VendorIdentifier)
        && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber)
        && invoice.InvoiceDate.HasValue
        && !string.IsNullOrWhiteSpace(invoice.Currency)
        && invoice.CapitalizationThreshold is > 0m
        && !string.IsNullOrWhiteSpace(invoice.CompanyPolicy)
        && invoice.Subtotal is > 0m
        && invoice.TaxAmount is >= 0m
        && invoice.TotalAmount is > 0m
        && invoice.SourceEvidence is { Count: > 0 }
        && invoice.SourceEvidence.All(evidence => !string.IsNullOrWhiteSpace(evidence))
        && invoice.Lines is { Count: > 0 }
        && invoice.Lines.All(line => line is not null
            && line.LineIndex > 0
            && !string.IsNullOrWhiteSpace(line.Description)
            && !string.IsNullOrWhiteSpace(line.EvidenceReference)
            && line.NetAmount is >= 0m
            && line.TaxAmount is >= 0m
            && line.Quantity is > 0m);

    private static bool ValidateAmounts(AccountsPayableInvoiceRequest invoice)
    {
        if (invoice.Subtotal is null || invoice.TaxAmount is null || invoice.TotalAmount is null
            || invoice.Lines is not { Count: > 0 }
            || invoice.Lines.Any(line => line is null || line.NetAmount is null || line.TaxAmount is null))
        {
            return false;
        }

        return invoice.Lines.Sum(line => line.NetAmount!.Value) == invoice.Subtotal.Value
            && invoice.Lines.Sum(line => line.TaxAmount!.Value) == invoice.TaxAmount.Value
            && invoice.Subtotal.Value + invoice.TaxAmount.Value == invoice.TotalAmount.Value
            && invoice.Lines.All(line => !line.UnitPrice.HasValue
                || (line.Quantity.HasValue
                    && line.UnitPrice.Value * line.Quantity.Value == line.NetAmount!.Value));
    }

    private static bool ValidateSuggestions(
        AccountsPayableInvoiceRequest invoice,
        AccountsPayableCodingRecommendation recommendation,
        IReadOnlyDictionary<string, AccountsPayableReferenceAccount> accounts,
        List<AccountsPayableCoaSuggestion> suggestions,
        List<string> reviewReasons)
    {
        if (invoice.Lines is null
            || invoice.Lines.Any(line => line is null)
            || recommendation.CoaSuggestions is null
            || recommendation.CoaSuggestions.Count != invoice.Lines.Count)
        {
            reviewReasons.Add("The LLM did not return exactly one account suggestion for every invoice line.");
            return false;
        }

        bool valid = true;
        foreach (var line in invoice.Lines)
        {
            var matches = recommendation.CoaSuggestions.Where(item => item.LineIndex == line.LineIndex).ToList();
            if (matches.Count != 1)
            {
                reviewReasons.Add($"Line {line.LineIndex} does not have exactly one account suggestion.");
                valid = false;
                continue;
            }

            var suggestion = matches[0];
            if (suggestion.RequiresReview)
            {
                reviewReasons.Add($"Line {line.LineIndex} requires human account-coding review.");
                valid = false;
                continue;
            }

            if (!accounts.TryGetValue(suggestion.AccountCode, out var account) || !account.Enabled)
            {
                reviewReasons.Add($"Suggested account '{suggestion.AccountCode}' is not enabled.");
                valid = false;
                continue;
            }

            if (!IsEligibleAccount(account))
            {
                reviewReasons.Add($"Suggested account '{suggestion.AccountCode}' cannot be automatically used for invoice lines.");
                valid = false;
                continue;
            }

            if (line.NetAmount >= invoice.CapitalizationThreshold
                && (account.Type is "Expense" or "Cost of Sales"))
            {
                reviewReasons.Add(
                    $"Line {line.LineIndex} exceeds the capitalization threshold and requires asset-policy review.");
                valid = false;
                continue;
            }

            suggestions.Add(new AccountsPayableCoaSuggestion(
                line.LineIndex,
                account.Code,
                account.Name,
                line.NetAmount ?? 0m,
                suggestion.Reason));
        }

        return valid;
    }

    private static bool IsEligibleAccount(AccountsPayableReferenceAccount account) =>
        account.Code != ReviewOnlyExpenseAccount
        && (account.ApRole.Equals("Expense", StringComparison.OrdinalIgnoreCase)
            || account.ApRole.Equals("Direct Cost", StringComparison.OrdinalIgnoreCase)
            || account.ApRole.Equals("Classification", StringComparison.OrdinalIgnoreCase));

    private static IReadOnlyList<AccountsPayableJournalLine> BuildJournal(
        AccountsPayableInvoiceRequest invoice,
        IReadOnlyList<AccountsPayableCoaSuggestion> suggestions,
        string gstCode)
    {
        bool isCreditNote = invoice.DocumentType.StartsWith("credit", StringComparison.OrdinalIgnoreCase);
        bool claimsGst = gstCode == StandardGstCode;
        var journal = new List<AccountsPayableJournalLine>();
        foreach (var suggestion in suggestions)
        {
            decimal amount = suggestion.NetAmount;
            if (!claimsGst)
            {
                amount += invoice.Lines.Single(line => line.LineIndex == suggestion.LineIndex).TaxAmount ?? 0m;
            }

            journal.Add(new AccountsPayableJournalLine(
                suggestion.AccountCode,
                isCreditNote ? 0m : amount,
                isCreditNote ? amount : 0m));
        }

        if (claimsGst && invoice.TaxAmount is > 0m)
        {
            journal.Add(new AccountsPayableJournalLine(
                GstInputAccount,
                isCreditNote ? 0m : invoice.TaxAmount.Value,
                isCreditNote ? invoice.TaxAmount.Value : 0m));
        }

        journal.Add(new AccountsPayableJournalLine(
            AccountsPayableAccount,
            isCreditNote ? invoice.TotalAmount ?? 0m : 0m,
            isCreditNote ? 0m : invoice.TotalAmount ?? 0m));
        return journal;
    }

    private static void AddCheck(
        ICollection<AccountsPayableValidation> validation,
        ICollection<string> reviewReasons,
        string check,
        bool passed,
        string failureReason)
    {
        validation.Add(new AccountsPayableValidation(check, passed));
        if (!passed)
        {
            reviewReasons.Add(failureReason);
        }
    }
}
