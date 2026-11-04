using ILP.Shared.AccountsPayable.Dto;
using System.Globalization;
using System.Text;

namespace ILP.Shared.AccountsPayable.Parser;

public static class AccountsPayableCodingParser
{
    public static AccountsPayableCodingRecommendation Parse(string? modelOutput)
    {
        if (string.IsNullOrWhiteSpace(modelOutput))
        {
            throw new FormatException("The AP coding response was empty.");
        }

        string? gstCode = null;
        var suggestions = new List<AccountsPayableLineSuggestion>();
        var reviewReasons = new List<string>();
        foreach (var line in modelOutput.Split('\n', StringSplitOptions.RemoveEmptyEntries))
        {
            var fields = ParseRecord(line.TrimEnd('\r'));
            switch (fields[0])
            {
                case "gst_code":
                    if (fields.Count != 2 || gstCode is not null || string.IsNullOrWhiteSpace(fields[1]))
                    {
                        throw new FormatException("The response must contain exactly one non-empty GST code record.");
                    }

                    gstCode = fields[1];
                    break;

                case "coa_suggestion":
                    if (fields.Count != 5
                        || !int.TryParse(fields[1], NumberStyles.None, CultureInfo.InvariantCulture, out var lineIndex)
                        || lineIndex <= 0
                        || string.IsNullOrWhiteSpace(fields[2])
                        || string.IsNullOrWhiteSpace(fields[3])
                        || !bool.TryParse(fields[4], out var requiresReview))
                    {
                        throw new FormatException(
                            "A COA suggestion must include a positive line index, account code, reason, and boolean review flag.");
                    }

                    suggestions.Add(new AccountsPayableLineSuggestion(
                        lineIndex, fields[2], fields[3], requiresReview));
                    break;

                case "review_reason":
                    if (fields.Count != 2 || string.IsNullOrWhiteSpace(fields[1]))
                    {
                        throw new FormatException("Each review reason must contain a non-empty reason.");
                    }

                    reviewReasons.Add(fields[1]);
                    break;

                default:
                    throw new FormatException($"Unknown AP coding record identifier '{fields[0]}'.");
            }
        }

        if (gstCode is null)
        {
            throw new FormatException("The AP coding response must include one GST code record.");
        }

        if (suggestions.Count == 0)
        {
            throw new FormatException("The AP coding response must include at least one COA suggestion.");
        }

        if (suggestions.Select(suggestion => suggestion.LineIndex).Distinct().Count() != suggestions.Count)
        {
            throw new FormatException("The AP coding response contains duplicate line suggestions.");
        }

        return new AccountsPayableCodingRecommendation
        {
            GstCode = gstCode,
            CoaSuggestions = suggestions,
            ReviewReasons = reviewReasons
        };
    }

    private static IReadOnlyList<string> ParseRecord(string line)
    {
        var fields = new List<string>();
        var index = 0;
        while (index < line.Length)
        {
            if (line[index] != '"')
            {
                throw new FormatException("Every pipe-delimited field must be quoted.");
            }

            index++;
            var value = new StringBuilder();
            bool closed = false;
            while (index < line.Length)
            {
                if (line[index] != '"')
                {
                    value.Append(line[index++]);
                    continue;
                }

                if (index + 1 < line.Length && line[index + 1] == '"')
                {
                    value.Append('"');
                    index += 2;
                    continue;
                }

                closed = true;
                index++;
                break;
            }

            if (!closed)
            {
                throw new FormatException("A quoted pipe-delimited field was not terminated.");
            }

            fields.Add(value.ToString());
            if (index == line.Length)
            {
                break;
            }

            if (line[index] != '|')
            {
                throw new FormatException("Quoted fields must be separated by a pipe.");
            }

            index++;
            if (index == line.Length)
            {
                throw new FormatException("The pipe-delimited record contains an empty unquoted field.");
            }
        }

        if (fields.Count == 0)
        {
            throw new FormatException("The pipe-delimited record is empty.");
        }

        return fields;
    }
}
