using ILP.Server.Features.AccountsPayable.Model;
using ILP.Server.Features.AccountsPayable.Service;
using ILP.Shared.AccountsPayable.Dto;

namespace ILP.Server.Tests.AccountsPayable;

public sealed class AccountsPayableRuleEvaluatorTests
{
    [Fact]
    public void Evaluate_ValidInvoice_ReturnsBalancedDraftForApproval()
    {
        var result = AccountsPayableRuleEvaluator.Evaluate(
            ValidRequest(),
            new AccountsPayableCodingRecommendation
            {
                GstCode = "SG_STD_9",
                CoaSuggestions = [new(1, "6000", "Stationery invoice line")]
            },
            [Account("6000", enabled: true), Account("1200", enabled: true), Account("2000", enabled: true)],
            [GstCode("SG_STD_9")]);

        Assert.Equal("READY_FOR_APPROVAL", result.Status);
        Assert.False(result.Posted);
        Assert.Equal(0m, result.Journal.Sum(entry => entry.Debit) - result.Journal.Sum(entry => entry.Credit));
        Assert.Contains(result.Journal, entry => entry.AccountCode == "6000" && entry.Debit == 1000m);
        Assert.Contains(result.Journal, entry => entry.AccountCode == "1200" && entry.Debit == 90m);
        Assert.Contains(result.Journal, entry => entry.AccountCode == "2000" && entry.Credit == 1090m);
    }

    [Theory]
    [InlineData("statement-of-account")]
    [InlineData("purchase-order")]
    [InlineData("goods-receipt")]
    public void Evaluate_NonInvoiceDocument_RequiresReviewWithoutJournal(string documentType)
    {
        var result = AccountsPayableRuleEvaluator.Evaluate(
            ValidRequest() with { DocumentType = documentType },
            Recommendation(),
            EnabledAccounts(),
            [GstCode("SG_STD_9")]);

        Assert.Equal("NEEDS_REVIEW", result.Status);
        Assert.Empty(result.Journal);
    }

    [Fact]
    public void Evaluate_DisabledOrReviewOnlyAccount_RequiresReviewAndDoesNotSubstitute()
    {
        var result = AccountsPayableRuleEvaluator.Evaluate(
            ValidRequest(),
            Recommendation("5010"),
            [Account("5010", enabled: false), Account("2000", enabled: true), Account("1200", enabled: true)],
            [GstCode("SG_STD_9")]);

        Assert.Equal("NEEDS_REVIEW", result.Status);
        Assert.Contains(result.ReviewReasons, reason => reason.Contains("5010", StringComparison.Ordinal));
        Assert.DoesNotContain(result.Journal, entry => entry.AccountCode == "6130");
    }

    [Fact]
    public void Evaluate_UnreconciledAmounts_RequiresReviewWithoutJournal()
    {
        var result = AccountsPayableRuleEvaluator.Evaluate(
            ValidRequest() with { TotalAmount = 1091m },
            Recommendation(),
            EnabledAccounts(),
            [GstCode("SG_STD_9")]);

        Assert.Equal("NEEDS_REVIEW", result.Status);
        Assert.Contains(result.Validation, check => check.Check == "invoice_total_reconciles" && !check.Passed);
        Assert.Empty(result.Journal);
    }

    [Fact]
    public void Evaluate_UnsupportedTaxEvidence_UsesUnknownTaxCodeAndRequiresReview()
    {
        var result = AccountsPayableRuleEvaluator.Evaluate(
            ValidRequest() with { InputGstClaimableConfirmed = false },
            Recommendation(),
            EnabledAccounts(),
            [GstCode("SG_STD_9"), GstCode("SG_UNKNOWN")]);

        Assert.Equal("NEEDS_REVIEW", result.Status);
        Assert.Equal("SG_UNKNOWN", result.GstCode);
        Assert.Contains(result.ReviewReasons, reason => reason.Contains("GST", StringComparison.OrdinalIgnoreCase));
    }

    private static AccountsPayableInvoiceRequest ValidRequest() => new()
    {
        DocumentType = "invoice",
        DocumentId = "TEST-001",
        VendorName = "Example Supplier",
        VendorIdentifier = "UEN-1",
        InvoiceNumber = "INV-001",
        Currency = "SGD",
        Subtotal = 1000m,
        TaxAmount = 90m,
        TotalAmount = 1090m,
        Lines = [new(1, "Office stationery", 1000m, 90m, "page 1")],
        SourceEvidence = ["invoice.pdf page 1"],
        CompanyGstRegistered = true,
        InvoiceGstEvidenceValid = true,
        InputGstClaimableConfirmed = true,
        VendorWhitelisted = true,
        UenMasterVerified = true,
        DuplicateCheckPassed = true,
        RequiredDocumentsProfile = "INVOICE_ONLY",
        SupportingDocumentsComplete = true
    };

    private static AccountsPayableCodingRecommendation Recommendation(string accountCode = "6000") => new()
    {
        GstCode = "SG_STD_9",
        CoaSuggestions = [new(1, accountCode, "Stationery invoice line")]
    };

    private static IReadOnlyList<AccountsPayableReferenceAccount> EnabledAccounts() =>
    [
        Account("6000", enabled: true),
        Account("1200", enabled: true),
        Account("2000", enabled: true)
    ];

    private static AccountsPayableReferenceAccount Account(string code, bool enabled) =>
        new(code, $"Account {code}", "Expense", "Debit", code == "2000" ? "AP Control" : "Expense", enabled);

    private static AccountsPayableReferenceGstCode GstCode(string code) =>
        new(code, code, true);
}
