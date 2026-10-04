using ILP.Shared.TransactionDocument.Model.Dto;

namespace ILP.Shared.InfoExtraction.Parser
{

    public static class ExtractedDocumentParser
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
                if (!ExtractedDocumentDto.All.Contains(key))
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
