using ILP.Server.Features.AccountsPayable.Model;
using ILP.Server.Features.InboxMonitoring.Persistence;
using ILP.Shared.AccountsPayable.Dto;
using ILP.Shared.AccountsPayable.Provider;
using Microsoft.EntityFrameworkCore;

namespace ILP.Server.Features.AccountsPayable.Service;

public sealed class AccountsPayableService(
    IDbContextFactory<InboxExtractionDbContext> contextFactory) : IAccountsPayableService
{
    public async Task<AccountsPayableReferenceData> GetReferenceDataAsync(CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await context.AccountsPayableAccounts.AsNoTracking()
            .OrderBy(account => account.Code)
            .Select(account => new AccountsPayableReferenceAccount(
                account.Code, account.Name, account.Type, account.NormalBalance, account.ApRole, account.Enabled,
                account.UseWhen, account.Sector, account.AiSuggestion, account.Notes))
            .ToListAsync(cancellationToken);
        var gstCodes = await context.AccountsPayableGstTaxCodes.AsNoTracking()
            .OrderBy(code => code.Code)
            .Select(code => new AccountsPayableReferenceGstCode(
                code.Code, code.Description, code.AutoProcessing == "Conditional", code.ReviewConditions))
            .ToListAsync(cancellationToken);
        return new AccountsPayableReferenceData(accounts, gstCodes);
    }

    public async Task<AccountsPayableCodingResult> EvaluateAsync(
        AccountsPayableInvoiceRequest invoice,
        CancellationToken cancellationToken)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var accounts = await context.AccountsPayableAccounts.AsNoTracking()
            .OrderBy(account => account.Code)
            .Select(account => new AccountsPayableReferenceAccount(
                account.Code, account.Name, account.Type, account.NormalBalance, account.ApRole, account.Enabled,
                account.UseWhen, account.Sector, account.AiSuggestion, account.Notes))
            .ToListAsync(cancellationToken);
        var gstCodes = await context.AccountsPayableGstTaxCodes.AsNoTracking()
            .OrderBy(code => code.Code)
            .Select(code => new AccountsPayableReferenceGstCode(
                code.Code, code.Description, code.AutoProcessing == "Conditional", code.ReviewConditions))
            .ToListAsync(cancellationToken);

        List<DuplicateCandidate> duplicateCandidates = [];
        if (!string.IsNullOrWhiteSpace(invoice.VendorIdentifier)
            && !string.IsNullOrWhiteSpace(invoice.InvoiceNumber))
        {
            duplicateCandidates = await context.Documents.AsNoTracking()
                .Where(document => document.ActiveStatus
                    && document.RefNumber.ToUpper() == invoice.InvoiceNumber.ToUpper()
                    && document.Company.Uen != null
                    && document.Company.Uen.ToUpper() == invoice.VendorIdentifier!.ToUpper()
                    && (IsCreditNote(invoice.DocumentType)
                        ? document.TypeCode.ToLower() == "credit note"
                            || document.TypeCode.ToLower() == "credit-note"
                        : document.TypeCode.ToLower() == "invoice"))
                .Select(document => new DuplicateCandidate(
                    document.RefNumber,
                    document.TypeCode,
                    document.SentDate,
                    document.TotalAmount,
                    document.DocumentItems.Select(item => item.CurrencyCode).FirstOrDefault()))
                .ToListAsync(cancellationToken);
        }

        var duplicateFound = duplicateCandidates.Count > 0;
        var checkedInvoice = invoice with
        {
            DuplicateCheckPassed = invoice.DuplicateCheckPassed == true && !duplicateFound
        };
        var suggestion = await AccountsPayableCodingProvider.GetRecommendation(
            checkedInvoice,
            accounts.Select(account => new AccountsPayableAccountOption(
                account.Code, account.Name, account.Type, account.ApRole, account.Enabled,
                account.UseWhen, account.Sector, account.AiSuggestion, account.Notes)).ToList(),
            gstCodes.Select(code => new AccountsPayableGstOption(
                code.Code, code.Description, code.CanAutoProcess ? "Conditional" : "No", code.ReviewConditions)).ToList(),
            Guid.NewGuid().ToString(),
            cancellationToken);

        if (duplicateFound)
        {
            string candidates = string.Join(
                "; ",
                duplicateCandidates.Select(candidate =>
                    $"{candidate.TypeCode} {candidate.RefNumber}: {candidate.SentDate:yyyy-MM-dd} "
                    + $"{candidate.TotalAmount} {candidate.Currency ?? "currency unavailable"}"));
            suggestion = suggestion with
            {
                ReviewReasons = suggestion.ReviewReasons
                    .Append($"Possible duplicate invoice matches: {candidates}.")
                    .ToList()
            };
        }

        return AccountsPayableRuleEvaluator.Evaluate(checkedInvoice, suggestion, accounts, gstCodes);
    }

    private static bool IsCreditNote(string? documentType) =>
        string.Equals(documentType, "credit-note", StringComparison.OrdinalIgnoreCase)
        || string.Equals(documentType, "credit note", StringComparison.OrdinalIgnoreCase);

    private sealed record DuplicateCandidate(
        string RefNumber,
        string TypeCode,
        DateTime SentDate,
        decimal TotalAmount,
        string? Currency);
}
