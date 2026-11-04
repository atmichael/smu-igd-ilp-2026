using ILP.Shared.InfoExtraction.Model.Dto;
using ILP.Shared.TransactionDocument.Constant;
using ILP.Shared.TransactionDocument.Dto;
using System.Globalization;

namespace ILP.Shared.InfoExtraction.Parser
{

    public static class ExtractedDocumentParser
    {
        private const string DocumentMarker = "[DOCUMENT]";
        private static readonly string[] DocumentTypes =
        [
            "Invoice",
            "Purchase Order",
            "Delivery Order",
            "Service Order",
            "Sales Order",
            "Statement of Account"
        ];

        /// <summary>Parses model output holding one or more documents, each optionally introduced by a [DOCUMENT] marker; always returns at least one document.</summary>
        public static IReadOnlyList<ExtractedDocumentDto> ParseAll(string? modelOutput)
        {
            var documents = new List<ExtractedDocumentDto>();
            if (!string.IsNullOrWhiteSpace(modelOutput))
            {
                var current = new List<string>();
                foreach (var line in modelOutput.Split('\n'))
                {
                    if (line.Trim().Equals(DocumentMarker, StringComparison.OrdinalIgnoreCase))
                    {
                        AddSegment(documents, current);
                        current.Clear();
                        continue;
                    }

                    current.Add(line);
                }

                AddSegment(documents, current);
            }

            if (documents.Count == 0)
            {
                documents.Add(new ExtractedDocumentDto());
            }

            // Documents without their own confidence inherit the one declared in the same response.
            var inherited = documents.Select(d => d.Confidence).LastOrDefault(c => c.HasValue);
            foreach (var document in documents.Where(d => !d.Confidence.HasValue))
            {
                document.Confidence = inherited;
            }

            return documents;
        }

        private static void AddSegment(List<ExtractedDocumentDto> documents, List<string> lines)
        {
            if (lines.Any(line => !string.IsNullOrWhiteSpace(line)))
            {
                documents.Add(Parse(string.Join('\n', lines)));
            }
        }

        /// <summary>Parses the prompt's sectioned output for a single document; "null" or blank values become null and unknown keys are ignored.</summary>
        public static ExtractedDocumentDto Parse(string? modelOutput)
        {
            var document = new ExtractedDocumentDto();
            if (string.IsNullOrWhiteSpace(modelOutput))
            {
                return document;
            }

            var section = "HEADER";
            foreach (var line in modelOutput.Split('\n'))
            {
                var trimmedLine = line.Trim();
                if (trimmedLine.Equals("[HEADER]", StringComparison.OrdinalIgnoreCase))
                {
                    section = "HEADER";
                    continue;
                }

                if (trimmedLine.Equals("[LINE_ITEMS]", StringComparison.OrdinalIgnoreCase))
                {
                    section = "LINE_ITEMS";
                    continue;
                }

                if (trimmedLine.Equals("[CONFIDENCE]", StringComparison.OrdinalIgnoreCase))
                {
                    section = "CONFIDENCE";
                    continue;
                }

                var separator = trimmedLine.IndexOf('|');
                if (separator <= 0)
                {
                    continue;
                }

                if (section == "HEADER")
                {
                    var key = trimmedLine[..separator].Trim().ToLowerInvariant();
                    if (!ExtractedDocumentDto.All.Contains(key))
                    {
                        continue;
                    }

                    var value = ParseValue(trimmedLine[(separator + 1)..]);
                    document.SetHeaderField(
                        key,
                        key == ExtractedDocFieldNames.DocumentType ? NormalizeDocumentType(value) : value);
                    continue;
                }

                if (section == "CONFIDENCE")
                {
                    var key = trimmedLine[..separator].Trim();
                    if (key.Equals("confidence", StringComparison.OrdinalIgnoreCase))
                    {
                        var confidence = ParseValue(trimmedLine[(separator + 1)..]);
                        if (decimal.TryParse(confidence, NumberStyles.Number, CultureInfo.InvariantCulture, out var score))
                        {
                            document.Confidence = score;
                        }
                    }

                    continue;
                }

                ParseLineItem(trimmedLine, document);
            }

            return document;
        }

        private static void ParseLineItem(string line, ExtractedDocumentDto document)
        {
            var columns = line.Split('|');
            if (columns.Length < 2 || columns[0].Trim().Equals("sn", StringComparison.OrdinalIgnoreCase))
            {
                return;
            }

            var serialNumber = int.TryParse(columns[0].Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedSerial)
                ? parsedSerial
                : (int?)null;
            var currencyCode = document.GetHeaderField(ExtractedDocFieldNames.Currency)?.ToUpperInvariant() ?? "SGD";
            var description = ParseValue(columns[1]) ?? string.Empty;
            var quantity = columns.Length > 2 ? ExtractedDocumentDto.ParseDecimal(ParseValue(columns[2])) ?? 0m : 0m;
            var unitPrice = columns.Length > 3 ? ExtractedDocumentDto.ParseDecimal(ParseValue(columns[3])) ?? 0m : 0m;
            var amount = columns.Length > 4 ? ExtractedDocumentDto.ParseDecimal(ParseValue(columns[4])) : null;
            var taxRate = columns.Length > 5 ? ExtractedDocumentDto.ParseDecimal(ParseValue(columns[5])) ?? 0m : 0m;
            var taxAmount = columns.Length > 6 ? ExtractedDocumentDto.ParseDecimal(ParseValue(columns[6])) : null;

            document.LineItems.Add(new DocumentItemDto(
                quantity,
                unitPrice,
                description: description,
                currencyCode: currencyCode,
                taxRate: taxRate,
                serialNumber: serialNumber,
                amount: amount,
                taxAmount: taxAmount));
        }

        private static string? ParseValue(string value)
        {
            var trimmedValue = value.Trim();
            return trimmedValue.Length == 0 || trimmedValue.Equals("null", StringComparison.OrdinalIgnoreCase)
                ? null
                : trimmedValue;
        }

        private static string? NormalizeDocumentType(string? value)
        {
            if (value is null)
            {
                return null;
            }

            var normalizedValue = value.Replace('-', ' ').Replace('_', ' ');
            return DocumentTypes.FirstOrDefault(type =>
                normalizedValue.Contains(type, StringComparison.OrdinalIgnoreCase)) ?? value;
        }
    }
}
