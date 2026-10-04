using System.Collections;
using System.Globalization;

namespace ILP.Shared.TransactionDocument.Model.Dto
{
    /// <summary>Keys and values returned by the ExtractDocumentContent prompt.</summary>
    public class ExtractedDocumentDto : IReadOnlyDictionary<string, string?>
    {
        public static class FieldNames
        {
            public const string CompanyName = "company-name";
            public const string CompanyUen = "company-uen";
            public const string CompanyTaxRegistrationNumber = "company-tax-registration-number";
            public const string DocumentNumber = "document-number";
            public const string DocumentType = "document-type";
            public const string DocumentDate = "document-date";
            public const string RelatedDocumentNumbers = "related-document-numbers";
            public const string Subtotal = "subtotal";
            public const string SubtotalTaxRate = "subtotal-tax-rate";
            public const string SubtotalTaxAmount = "subtotal-tax-amount";
            public const string TotalAmount = "total-amount";
        }

        public static IReadOnlyList<string> All { get; } =
        [
            FieldNames.CompanyName,
            FieldNames.CompanyUen,
            FieldNames.CompanyTaxRegistrationNumber,
            FieldNames.DocumentNumber,
            FieldNames.DocumentType,
            FieldNames.DocumentDate,
            FieldNames.RelatedDocumentNumbers,
            FieldNames.Subtotal,
            FieldNames.SubtotalTaxRate,
            FieldNames.SubtotalTaxAmount,
            FieldNames.TotalAmount
        ];

        private readonly Dictionary<string, string?> _headerFields = new(StringComparer.OrdinalIgnoreCase);

        public string? CompanyName { get; internal set; }
        public string? CompanyUen { get; internal set; }
        public string? CompanyTaxRegistrationNumber { get; internal set; }
        public string? DocumentNumber { get; internal set; }
        public string? DocumentType { get; internal set; }
        public string? DocumentDate { get; internal set; }
        public string? RelatedDocumentNumbers { get; internal set; }
        public string? Subtotal { get; internal set; }
        public string? SubtotalTaxRate { get; internal set; }
        public string? SubtotalTaxAmount { get; internal set; }
        public string? TotalAmount { get; internal set; }
        public decimal? Confidence { get; internal set; }
        public List<DocumentItemDto> LineItems { get; } = [];

        public string? this[string key] => _headerFields[key];
        public IEnumerable<string> Keys => _headerFields.Keys;
        public IEnumerable<string?> Values => _headerFields.Values;
        public int Count => _headerFields.Count;

        public CompanyDto GetCompany() => new(
            CompanyName ?? string.Empty,
            CompanyUen ?? string.Empty,
            taxRegistrationNumber: CompanyTaxRegistrationNumber ?? string.Empty);

        public List<DocumentItemDto> GetLineItems() => LineItems;

        public DocumentDto GetDocumentInfo()
        {
            DateTime.TryParseExact(
                DocumentDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var sentDate);

            return new DocumentDto(
                DocumentNumber ?? string.Empty,
                DocumentType ?? string.Empty,
                sentDate,
                ParseDecimal(TotalAmount) ?? 0m,
                documentItems: LineItems,
                relatedDocumentNumbers: RelatedDocumentNumbers,
                subtotal: ParseDecimal(Subtotal) ?? 0m,
                subtotalTaxRate: ParseDecimal(SubtotalTaxRate) ?? 0m,
                subtotalTaxAmount: ParseDecimal(SubtotalTaxAmount) ?? 0m);
        }

        internal void SetHeaderField(string key, string? value)
        {
            _headerFields[key] = value;
            switch (key)
            {
                case FieldNames.CompanyName:
                    CompanyName = value;
                    break;
                case FieldNames.CompanyUen:
                    CompanyUen = value;
                    break;
                case FieldNames.CompanyTaxRegistrationNumber:
                    CompanyTaxRegistrationNumber = value;
                    break;
                case FieldNames.DocumentNumber:
                    DocumentNumber = value;
                    break;
                case FieldNames.DocumentType:
                    DocumentType = value;
                    break;
                case FieldNames.DocumentDate:
                    DocumentDate = value;
                    break;
                case FieldNames.RelatedDocumentNumbers:
                    RelatedDocumentNumbers = value;
                    break;
                case FieldNames.Subtotal:
                    Subtotal = value;
                    break;
                case FieldNames.SubtotalTaxRate:
                    SubtotalTaxRate = value;
                    break;
                case FieldNames.SubtotalTaxAmount:
                    SubtotalTaxAmount = value;
                    break;
                case FieldNames.TotalAmount:
                    TotalAmount = value;
                    break;
            }
        }

        internal static decimal? ParseDecimal(string? value) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

        public bool ContainsKey(string key) => _headerFields.ContainsKey(key);
        public bool TryGetValue(string key, out string? value) => _headerFields.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => _headerFields.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
