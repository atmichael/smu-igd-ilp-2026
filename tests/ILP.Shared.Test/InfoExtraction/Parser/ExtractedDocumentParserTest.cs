using ILP.Shared.Evidence;
using ILP.Shared.InfoExtraction.Parser;
using ILP.Shared.TransactionDocument.Model.Dto;
using Xunit;

namespace ILP.Shared.Test.InfoExtraction.Parser
{
    public class ExtractedDocumentParserTest
    {
        // Same shape as the "Output Format" example in Prompts/ExtractDocumentHeaderInfo.md.
        private const string SampleOutput = """
            company-name| ACME LOGISTICS PTE LTD
            company-uen| 201509876K
            company-tax-registration-number| null
            document-number| INV-2026-00422
            document-type| Invoice
            document-date| 2026-10-04
            related-document-numbers| PO-88192, DO-1002
            subtotal| 1200.00
            subtotal-tax-rate| 0.09
            subtotal-tax-amount| 108.00
            total-amount| 1308.00
            """;

        [Fact]
        public void Parse_ReadsKnownKeysAndTreatsNullAsMissing()
        {
            var fields = ExtractedDocumentParser.Parse(SampleOutput);

            Assert.Equal("INV-2026-00422", fields[ExtractedDocumentDto.DocumentNumber]);
            Assert.Equal("PO-88192, DO-1002", fields[ExtractedDocumentDto.RelatedDocumentNumbers]);
            Assert.Null(fields[ExtractedDocumentDto.CompanyTaxRegistrationNumber]);
            Assert.Equal(ExtractedDocumentDto.All.Count, fields.Count);
        }

        [Fact]
        public void Parse_IgnoresUnknownKeysAndCommentary()
        {
            var fields = ExtractedDocumentParser.Parse("Here is the result:\nfavourite-colour| blue\ntotal-amount| 10.00");

            Assert.Equal("10.00", Assert.Single(fields).Value);
        }

        [Theory]
        [InlineData("Invoice", DocumentType.Invoice)]
        [InlineData("Purchase Order", DocumentType.PurchaseOrder)]
        [InlineData("Service Order", DocumentType.PurchaseOrder)]
        [InlineData("Delivery Order", DocumentType.Receipt)]
        [InlineData("Sales Order", DocumentType.OtherEvidence)]
        [InlineData("Statement of Account", DocumentType.OtherEvidence)]
        [InlineData("purchase-order", DocumentType.PurchaseOrder)]
        [InlineData("goods-receipt", DocumentType.Receipt)]
        [InlineData("service-acceptance", DocumentType.Receipt)]
        [InlineData("sales-order", DocumentType.OtherEvidence)]
        [InlineData("unclassified", DocumentType.OtherEvidence)]
        [InlineData(null, DocumentType.OtherEvidence)]
        public void ToDocumentType_MapsPromptLabels(string? label, DocumentType expected)
        {
            Assert.Equal(expected, ExtractedDocumentMapper.ToDocumentType(label));
        }

        [Fact]
        public void ToEvidenceDocument_CreatesOneHeaderRecordPerExtractedField()
        {
            var fields = ExtractedDocumentParser.Parse(SampleOutput);

            var document = ExtractedDocumentMapper.ToEvidenceDocument(fields, "protected://evidence/inv.pdf", "sha256:abc", modelVersion: "gemini-2.5-flash");

            Assert.Equal("invoice", document.DocumentType);
            Assert.Equal("INV-2026-00422", document.SourceReference);
            Assert.Equal(ExtractedDocumentDto.All.Count - 1, document.Records!.Count);
            var total = Assert.Single(document.Records, record => record.RecordType == ExtractedDocumentDto.TotalAmount);
            Assert.Equal("document-header", total.RecordCategory);
            Assert.Equal("1308.00", total.RawValue);
            Assert.Equal("extracted", Assert.Single(total.Provenance!).EventType);
        }

        [Fact]
        public void DocumentItemDto_Constructor_KeepsCurrencyTaxNameAndId()
        {
            var item = new DocumentItemDto(2, 10m, id: 7, description: "Pallet", currencyCode: "USD", taxName: "VAT", taxRate: 0.2m);

            Assert.Equal(7, item.Id);
            Assert.Equal("USD", item.CurrencyCode);
            Assert.Equal("VAT", item.TaxName);
            Assert.Equal(24.00m, item.GetTotalAmount());
        }
    }
}
