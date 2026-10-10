using ILP.Shared.TransactionDocument.Constant;
using System.Collections;
using System.Globalization;

namespace ILP.Shared.TransactionDocument.Dto
{
    /// <summary>Keys and values returned by the ExtractDocumentContent prompt.</summary>
    public class ExtractedDocumentDto : IReadOnlyDictionary<string, string?>
    {

        public static IReadOnlyList<string> All { get; } =
        [
            ExtractedDocFieldNames.CompanyName,
            ExtractedDocFieldNames.CompanyUen,
            ExtractedDocFieldNames.CompanyTaxRegistrationNumber,
            ExtractedDocFieldNames.DocumentNumber,
            ExtractedDocFieldNames.DocumentType,
            ExtractedDocFieldNames.DocumentDate,
            ExtractedDocFieldNames.RelatedDocumentNumbers,
            ExtractedDocFieldNames.Subtotal,
            ExtractedDocFieldNames.SubtotalTaxRate,
            ExtractedDocFieldNames.SubtotalTaxAmount,
            ExtractedDocFieldNames.TotalAmount
        ];

        private readonly Dictionary<string, string?> _headerFields = new(StringComparer.OrdinalIgnoreCase);

        public decimal? Confidence { get; internal set; }
        public List<DocumentItemDto> LineItems { get; } = [];

        public string? this[string key] => _headerFields[key];
        public IEnumerable<string> Keys => _headerFields.Keys;
        public IEnumerable<string?> Values => _headerFields.Values;
        public int Count => _headerFields.Count;

        public CompanyDto GetCompany() => new(
            GetHeaderField(ExtractedDocFieldNames.CompanyName) ?? string.Empty,
            GetHeaderField(ExtractedDocFieldNames.CompanyUen) ?? string.Empty,
            taxRegistrationNumber: GetHeaderField(ExtractedDocFieldNames.CompanyTaxRegistrationNumber) ?? string.Empty);

        public List<DocumentItemDto> GetLineItems() => LineItems;

        public DocumentDto GetDocumentInfo()
        {
            DateTime.TryParseExact(
                GetHeaderField(ExtractedDocFieldNames.DocumentDate),
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var sentDate);

            return new DocumentDto(
                GetHeaderField(ExtractedDocFieldNames.DocumentNumber) ?? string.Empty,
                GetHeaderField(ExtractedDocFieldNames.DocumentType) ?? string.Empty,
                sentDate,
                ParseDecimal(GetHeaderField(ExtractedDocFieldNames.TotalAmount)) ?? 0m,
                documentItems: LineItems,
                relatedDocumentNumbers: GetHeaderField(ExtractedDocFieldNames.RelatedDocumentNumbers),
                subtotal: ParseDecimal(GetHeaderField(ExtractedDocFieldNames.Subtotal)) ?? 0m,
                subtotalTaxRate: ParseDecimal(GetHeaderField(ExtractedDocFieldNames.SubtotalTaxRate)) ?? 0m,
                subtotalTaxAmount: ParseDecimal(GetHeaderField(ExtractedDocFieldNames.SubtotalTaxAmount)) ?? 0m,
                company: GetCompany());
        }

        internal void SetHeaderField(string key, string? value) =>
            _headerFields[key] = value;

        private string? GetHeaderField(string key) =>
            _headerFields.TryGetValue(key, out var value) ? value : null;

        internal static decimal? ParseDecimal(string? value) =>
            decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var number) ? number : null;

        public bool ContainsKey(string key) => _headerFields.ContainsKey(key);
        public bool TryGetValue(string key, out string? value) => _headerFields.TryGetValue(key, out value);
        public IEnumerator<KeyValuePair<string, string?>> GetEnumerator() => _headerFields.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
