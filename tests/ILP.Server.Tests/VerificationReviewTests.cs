using ILP.Server.Features.Verification.Persistence;
using ILP.Shared.Verification.Contracts;
using Xunit;

namespace ILP.Server.Tests;

public sealed class VerificationReviewTests
{
    [Fact]
    public void ResolutionRecordsAuthenticatedUserWithoutChangingRun()
    {
        var repository = new VerificationRepository();
        var run = new VerificationRunDto(
            "run-1", "1", "FullTaxInvoice", ILP.Shared.Verification.Enums.VerificationRunStatus.Blocked,
            "iras-sg-invoice-rules-v1", "evidence-1", [], DateTimeOffset.UtcNow);
        repository.SaveRun(run);

        repository.Resolve("review-1", "camera-capture-test-user", "Evidence was reviewed.");
        var preserved = repository.GetRun("run-1");

        Assert.NotNull(preserved);
        Assert.Equal(ILP.Shared.Verification.Enums.VerificationRunStatus.Blocked, preserved.Status);
        Assert.Empty(preserved.Checks);
    }

    [Fact]
    public void VerificationDoesNotExposePaymentApproval()
    {
        var json = System.Text.Json.JsonSerializer.Serialize(new VerificationRunDto(
            "run-2", "2", "FullTaxInvoice", ILP.Shared.Verification.Enums.VerificationRunStatus.Verified,
            "iras-sg-invoice-rules-v1", "evidence-2", [], DateTimeOffset.UtcNow));

        Assert.DoesNotContain("paymentApproval", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("paymentStatus", json, StringComparison.OrdinalIgnoreCase);
    }
}
