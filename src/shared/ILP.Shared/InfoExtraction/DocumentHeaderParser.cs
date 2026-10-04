namespace ILP.Shared.InfoExtraction
{
    /// <summary>Keys returned by the ExtractDocumentHeaderInfo prompt; evidence records use them as their record type.</summary>
    public static class DocumentHeaderFields
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

    public static class DocumentHeaderParser
    {
        /// <summary>Parses the prompt's "key| value" lines; "null" or blank values become null and unknown keys are ignored.</summary>
        public static IReadOnlyDictionary<string, string?> Parse(string? modelOutput)
        {
            var fields = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
            if (string.IsNullOrWhiteSpace(modelOutput))
            {
                return fields;
            }

            foreach (var line in modelOutput.Split('\n'))
            {
                var separator = line.IndexOf('|');
                if (separator <= 0)
                {
                    continue;
                }

                var key = line[..separator].Trim().ToLowerInvariant();
                if (!DocumentHeaderFields.All.Contains(key))
                {
                    continue;
                }

                var value = line[(separator + 1)..].Trim();
                fields[key] = value.Length == 0 || value.Equals("null", StringComparison.OrdinalIgnoreCase) ? null : value;
            }

            return fields;
        }
    }
}
