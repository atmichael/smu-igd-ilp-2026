using ILP.Server.Features.ReferenceData.Repository;
using ILP.Server.Features.ReferenceData.Service;
using ILP.Shared.ReferenceData;
using Xunit;

namespace ILP.Server.Tests;

public sealed class ReferenceDataAndRulesTests
{
    [Fact]
    public void SnapshotValidator_rejects_duplicate_records_and_conflicting_aliases()
    {
        var record = new ReferenceDataRecordDto("vendor-1", "Vendor", "V-001", "ERP", null, null,
            new Dictionary<string, string> { ["Name"] = "Contoso" }, new DateOnly(2026, 1, 1), null, "1", "Approved");
        var snapshot = new ReferenceSnapshotDto("snapshot-1", "Approved", "ERP vendor master", DateTimeOffset.UtcNow, "1",
            [record, record],
            [new ReferenceAliasDto("Contoso", "V-001", "ERP", "vendor-1") , new ReferenceAliasDto("Contoso", "V-002", "ERP", "vendor-2")]);

        var errors = new ReferenceDataSnapshotValidator().Validate(snapshot);

        Assert.Contains(errors, error => error.Contains("duplicate", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(errors, error => error.Contains("alias conflicts", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void DocumentRuleSelector_rejects_ambiguous_variant_and_selects_only_matching_rule()
    {
        var selector = new DocumentRuleSelector();
        Assert.Equal(ReferenceDataOutcomeReason.AmbiguousVariant, selector.ValidateVariant("Unknown"));

        var invoiceRule = SingaporeRule("FullTaxInvoice", "2026-1");
        var receiptRule = SingaporeRule("Receipt", "2026-2");
        Assert.Same(invoiceRule, selector.Select([receiptRule, invoiceRule], "Singapore", "FullTaxInvoice", new DateOnly(2026, 6, 1)));
    }

    [Fact]
    public void SingaporeGstPolicyValidator_requires_supported_policy_fields_and_threshold()
    {
        var package = SingaporeRule("SimplifiedTaxInvoice", "gst-1") with
        {
            RuleValues = new Dictionary<string, string>
            {
                ["GstRate"] = "9%",
                ["SimplifiedInvoiceThresholdSgd"] = "1000",
                ["RequiredFields"] = "Supplier;GST number;amount;rate",
                ["RoundingMethod"] = "HalfUp-to-cent",
                ["ForeignCurrencyConversionMethod"] = "IRAS-approved rate",
                ["CreditNoteAdjustment"] = "Reverse original tax treatment"
            }
        };

        Assert.Empty(new SingaporeGstPolicyValidator().Validate(package));
    }

    [Fact]
    public void FileRepository_publishes_snapshot_atomically_and_preserves_current_snapshot()
    {
        var directory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        try
        {
            var repository = new FileReferenceDataRepository(directory);
            var snapshot = new ReferenceSnapshotDto("snapshot-1", "Approved", "ERP vendor master", DateTimeOffset.UtcNow, "1", [], []);
            repository.Publish(snapshot);

            Assert.Equal(snapshot.SnapshotId, repository.GetCurrentSnapshot("ERP vendor master")?.SnapshotId);
            Assert.Single(repository.GetSnapshots("ERP vendor master"));
        }
        finally
        {
            Directory.Delete(directory, true);
        }
    }

    private static BusinessRulePackageDto SingaporeRule(string variant, string version) => new(
        "rule-" + variant, "Singapore", variant, new DateOnly(2026, 1, 1), null,
        "Inland Revenue Authority of Singapore",
        "https://www.iras.gov.sg/taxes/goods-services-tax-(gst)/basics-of-gst/invoicing-price-display-and-record-keeping/invoicing-customers",
        new Dictionary<string, string>(), "Approved", "Tax policy owner", new DateOnly(2026, 1, 1), new DateOnly(2026, 12, 31), version);
}
