using ILP.Server.Features.Verification.Persistence;
using ILP.Server.Features.Verification.Service;
using ILP.Shared.Verification.Enums;
using ILP.Server.Features.InboxMonitoring.Persistence.Entity;
using Xunit;

namespace ILP.Server.Tests;

public sealed class DeterministicVerificationTests
{
    [Fact]
    public void SubtotalGstRoundsToNearestCent()
    {
        Assert.Equal(0.90m, VerificationCalculationPolicy.SubtotalGst(10m, 0.09m));
    }

    [Fact]
    public void MissingEvidenceBlocksRequiredFieldChecks()
    {
        var document = NewDocument();
        var run = new VerificationOrchestrator(new VerificationRepository()).Verify(document, null, null);

        Assert.Equal(VerificationRunStatus.Blocked, run.Status);
        Assert.Contains(run.Checks, check => check.Outcome == VerificationOutcome.Fail && check.FieldIdentifier == "supplier-name");
    }

    [Fact]
    public void SupportedEvidenceBlocksSimplifiedInvoiceAboveThreshold()
    {
        var document = NewDocument();
        document.TypeCode = "SimplifiedTaxInvoice";
        document.TotalAmount = 1000.01m;
        var run = new VerificationOrchestrator(new VerificationRepository()).Verify(document, "evidence-1", null);

        Assert.Equal(VerificationRunStatus.Blocked, run.Status);
        Assert.Contains(run.Checks, check => check.FieldIdentifier == "simplified-invoice-threshold" && check.Outcome == VerificationOutcome.Fail);
    }

    [Fact]
    public void SalesVoucherAboveThresholdDoesNotQualifyAsSimplifiedTaxInvoice()
    {
        var document = NewDocument();
        document.TypeCode = "SalesVoucher";
        document.TotalAmount = 1000.01m;
        var run = new VerificationOrchestrator(new VerificationRepository()).Verify(document, "evidence-1", null);

        Assert.Equal(VerificationRunStatus.Blocked, run.Status);
        Assert.Contains(run.Checks, check => check.FieldIdentifier == "simplified-document-applicability" && check.Outcome == VerificationOutcome.Fail);
    }

    [Fact]
    public void ZeroRatedSupplyIsRecognized()
    {
        var document = NewDocument();
        document.DocumentItems.Add(new DocumentItemEntity { Description = "Zero rated", TaxRate = 0m });
        var check = ZeroRatedSupplyVerifier.Verify(document, true);
        Assert.Equal(ILP.Shared.Verification.Enums.VerificationOutcome.Pass, check.Outcome);
    }

    [Fact]
    public void ItemByItemGstUsesEachLineRate()
    {
        var document = NewDocument();
        document.DocumentItems.Add(new DocumentItemEntity { Quantity = 1, UnitPrice = 10m, TaxRate = 0.09m });
        Assert.Equal(0.90m, ItemByItemGstVerifier.Calculate(document));
    }

    [Fact]
    public void CashRoundingIsReportedSeparately()
    {
        Assert.Equal(-0.01m, CashRoundingVerifier.CalculateAdjustment(10.01m));
    }

    private static DocumentEntity NewDocument() => new()
    {
        Id = 1,
        RefNumber = "INV-001",
        TypeCode = "FullTaxInvoice",
        SentDate = new DateTime(2026, 1, 1),
        Subtotal = 100m,
        SubtotalTaxRate = 0.09m,
        SubtotalTaxAmount = 9m,
        TotalAmount = 109m,
        Company = new CompanyEntity { Name = "Test Supplier", Address = "1 Test Street", TaxRegistrationNumber = "M12345678X" }
    };
}
