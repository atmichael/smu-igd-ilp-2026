using ILP.Shared.TransactionDocument.Dto;
using System.Globalization;

namespace ILP.Shared.InfoExtraction.Parser
{

    public static class ExtractedDocumentParser
    {
        /// <summary>Parses the prompt's sectioned output; "null" or blank values become null and unknown keys are ignored.</summary>
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

                    document.SetHeaderField(key, ParseValue(trimmedLine[(separator + 1)..]));
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
    }
}
