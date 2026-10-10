using ILP.Shared.Evidence;
using ILP.Shared.InfoExtraction.Parser;
using ILP.Shared.TransactionDocument.Constant;
using ILP.Shared.TransactionDocument.Dto;
using Xunit;

namespace ILP.Shared.Test.InfoExtraction.Parser
{
    public class ExtractedDocumentParserTest
    {
        private const string SampleOutput = """
            [HEADER]
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

            [LINE_ITEMS]
            1|Pallet|2.00|10.00|20.00|0.09|1.80
            2|Service|1.00|50.00|50.00|null|null

            [CONFIDENCE]
            confidence|0.95
            """;

        [Fact]
        public void Parse_ReadsKnownKeysAndTreatsNullAsMissing()
        {
            var fields = ExtractedDocumentParser.Parse(SampleOutput);

            Assert.Equal("INV-2026-00422", fields[ExtractedDocFieldNames.DocumentNumber]);
            Assert.Equal("PO-88192, DO-1002", fields[ExtractedDocFieldNames.RelatedDocumentNumbers]);
            Assert.Null(fields[ExtractedDocFieldNames.CompanyTaxRegistrationNumber]);
            Assert.Equal(ExtractedDocumentDto.All.Count, fields.Count);
            Assert.Equal(0.95m, fields.Confidence);
            var company = fields.GetCompany();
            Assert.Equal("ACME LOGISTICS PTE LTD", company.Name);
            Assert.Equal("201509876K", company.UEN);
            Assert.Equal(string.Empty, company.TaxRegistrationNumber);
            var documentInfo = fields.GetDocumentInfo();
            Assert.Equal("ACME LOGISTICS PTE LTD", documentInfo.Company.Name);
            Assert.Equal("201509876K", documentInfo.Company.UEN);
            Assert.Equal("INV-2026-00422", documentInfo.RefNumber);
            Assert.Equal("Invoice", documentInfo.TypeCode);
            Assert.Equal(new DateTime(2026, 10, 4), documentInfo.SentDate);
            Assert.Equal("PO-88192, DO-1002", documentInfo.RelatedDocumentNumbers);
            Assert.Equal(1200.00m, documentInfo.Subtotal);
            Assert.Equal(0.09m, documentInfo.SubtotalTaxRate);
            Assert.Equal(108.00m, documentInfo.SubtotalTaxAmount);
            Assert.Equal(1308.00m, documentInfo.TotalAmount);
            var items = documentInfo.DocumentItems;
            Assert.Equal(2, items.Count);
            var item = items[0];
            Assert.Equal(1, item.SerialNumber);
            Assert.Equal("Pallet", item.Description);
            Assert.Equal(20.00m, item.Amount);
            Assert.Equal(1.80m, item.TaxAmount);
            Assert.Equal(21.80m, item.GetTotalAmount());
        }

        [Fact]
        public void Parse_IgnoresUnknownKeysAndCommentary()
        {
            var fields = ExtractedDocumentParser.Parse("[HEADER]\nHere is the result:\nfavourite-colour| blue\ntotal-amount| 10.00");

            Assert.Equal("10.00", Assert.Single(fields).Value);
        }

        [Fact]
        public void GetLineItems_ReturnsEmptyListWhenOutputHasNoLineItems()
        {
            var fields = ExtractedDocumentParser.Parse("[HEADER]\ndocument-number| INV-1\n[LINE_ITEMS]\n[CONFIDENCE]\nconfidence|0.8");

            Assert.Empty(fields.GetLineItems());
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
            var total = Assert.Single(document.Records, record => record.RecordType == ExtractedDocFieldNames.TotalAmount);
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
