namespace ILP.Shared.TransactionDocument.Model.Dto
{
    /// <summary>Keys returned by the ExtractDocumentHeaderInfo prompt; evidence records use them as their record type.</summary>
    public static class ExtractedDocumentHeaderDto
    {
        public const string CompanyName = "company-name";
        public const string CompanyUen = "company-uen";
        public const string CompanyTaxRegistrationNumber = "company-tax-registration-number";
        public const string DocumentNumber = "document-number";
        public const string DocumentType = "document-type";
        public const string RelatedDocumentNumbers = "related-document-numbers";
        public const string Subtotal = "subtotal";
        public const string SubtotalTaxRate = "subtotal-tax-rate";
        public const string SubtotalTaxAmount = "subtotal-tax-amount";
        public const string TotalAmount = "total-amount";

        public static IReadOnlyList<string> All { get; } =
        [
            CompanyName,
            CompanyUen,
            CompanyTaxRegistrationNumber,
            DocumentNumber,
            DocumentType,
            RelatedDocumentNumbers,
            Subtotal,
            SubtotalTaxRate,
            SubtotalTaxAmount,
            TotalAmount
        ];
    }
}
