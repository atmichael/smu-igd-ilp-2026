using ILP.Server.Features.AccountsPayable.Model;
using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Server.Features.TransactionDocuments.Configuration;
using ILP.Shared.AccountsPayable.Abstraction;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.AccountsPayable.Provider;
using ILP.Shared.TransactionDocument.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace ILP.Server.Features.AccountsPayable.Service;

public sealed class InboxAccountsPayableCodingProcessor(
    IDbContextFactory<InboxExtractionDbContext> contextFactory,
    IOptions<TransactionDocumentsOptions> documentOptions) : IAccountsPayableCodingProcessor
{
    private static readonly IReadOnlyDictionary<string, string> VerificationPrompts =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["document_type_supported"] = "Document type requires verification",
            ["credit_note_original_treatment_available"] = "Credit note coding requires review of the original invoice",
            ["required_fields_present"] = "Invoice details are incomplete",
            ["company_accounting_policy_available"] = "Accounting policy requires verification",
            ["currency_supported"] = "Currency requires verification",
            ["vendor_identity_verified"] = "Supplier identity requires verification",
            ["duplicate_check_passed"] = "Duplicate invoice check requires verification",
            ["supporting_documents_complete"] = "Supporting documents require verification",
            ["invoice_total_reconciles"] = "Invoice totals do not reconcile",
            ["coa_suggestions_enabled"] = "AP account coding requires verification",
            ["gst_code_supported"] = "GST treatment requires verification",
            ["ap_control_account_enabled"] = "AP control account setup requires verification",
            ["gst_control_account_enabled"] = "GST control account setup requires verification",
            ["journal_balanced"] = "Draft journal balance requires verification"
        };

    public async Task<DocumentAPJournalDto> ProcessAsync(
        DocumentDto document,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accountReferences = await context.AccountsPayableAccounts.AsNoTracking()
            .OrderBy(account => account.Code)
            .Select(account => new AccountsPayableReferenceAccount(
                account.Code, account.Name, account.Type, account.NormalBalance, account.ApRole, account.Enabled,
                account.UseWhen, account.Sector, account.AiSuggestion, account.Notes))
            .ToListAsync(cancellationToken);
        var gstReferences = await context.AccountsPayableGstTaxCodes.AsNoTracking()
            .OrderBy(code => code.Code)
            .Select(code => new AccountsPayableReferenceGstCode(
                code.Code, code.Description, code.AutoProcessing == "Conditional", code.ReviewConditions))
            .ToListAsync(cancellationToken);
        var accounts = accountReferences.Select(account => new AccountsPayableAccountOption(
            account.Code, account.Name, account.Type, account.ApRole, account.Enabled,
            account.UseWhen, account.Sector, account.AiSuggestion, account.Notes)).ToArray();
        var gstCodes = gstReferences.Select(code => new AccountsPayableGstOption(
            code.Code, code.Description, code.CanAutoProcess ? "Conditional" : "No", code.ReviewConditions)).ToArray();

        var invoice = new AccountsPayableInvoiceRequest
        {
            DocumentType = document.TypeCode,
            DocumentId = document.RefNumber,
            VendorName = document.Company.Name,
            VendorIdentifier = document.Company.UEN,
            InvoiceNumber = document.RefNumber,
            InvoiceDate = DateOnly.FromDateTime(document.SentDate),
            Currency = document.DocumentItems.FirstOrDefault()?.CurrencyCode ?? "SGD",
            Subtotal = document.Subtotal,
            TaxAmount = document.SubtotalTaxAmount,
            TotalAmount = document.TotalAmount,
            Lines = document.DocumentItems.Select((item, index) => new AccountsPayableInvoiceLine(
                index + 1,
                item.Description,
                item.Amount ?? item.GetBasePrice(),
                item.TaxAmount ?? item.GetTaxAmount(),
                item.Description,
                item.Quantity,
                item.UnitPrice)).ToArray(),
            DuplicateCheckPassed = true
        };
        var recommendation = await AccountsPayableCodingProvider.GetRecommendation(
            invoice, accounts, gstCodes, Guid.NewGuid().ToString(), cancellationToken);
        var result = AccountsPayableRuleEvaluator.Evaluate(
            invoice, recommendation, accountReferences, gstReferences);
        var notes = result.Validation
            .Where(check => !check.Passed)
            .Select(check => VerificationPrompts.GetValueOrDefault(check.Check, "AP coding requires human verification"))
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (result.Confidence <= documentOptions.Value.ConfidenceThreshold)
        {
            notes.Add("AP coding confidence is below the review threshold");
        }

        return new DocumentAPJournalDto(
            result.Confidence,
            result.GstCode,
            recommendation.CoaSuggestions,
            notes.Distinct(StringComparer.Ordinal).ToArray());
    }

}
