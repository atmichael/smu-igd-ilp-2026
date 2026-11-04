using ILP.Shared.AccountsPayable.Dto;
using System.Text.Json;

namespace ILP.Shared.AccountsPayable.Parser;

public static class AccountsPayableCodingParser
{
    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public static AccountsPayableCodingRecommendation Parse(string? modelOutput)
    {
        var json = ExtractJson(modelOutput);
        AccountsPayableCodingRecommendation? result;
        try
        {
            result = JsonSerializer.Deserialize<AccountsPayableCodingRecommendation>(json, SerializerOptions);
        }
        catch (JsonException exception)
        {
            throw new FormatException("The AP coding response was not valid JSON.", exception);
        }

        if (result is null
            || string.IsNullOrWhiteSpace(result.GstCode)
            || result.CoaSuggestions is null
            || result.ReviewReasons is null)
        {
            throw new FormatException("The AP coding response must include a GST code and COA suggestions.");
        }

        if (result.CoaSuggestions.Any(suggestion => suggestion is null
                || suggestion.LineIndex <= 0
                || string.IsNullOrWhiteSpace(suggestion.AccountCode)
                || suggestion.Reason is null))
        {
            throw new FormatException("Each AP coding suggestion must include a positive line index, account code, and reason.");
        }

        return result;
    }

    private static string ExtractJson(string? output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            throw new FormatException("The AP coding response was empty.");
        }

        var trimmed = output.Trim();
        if (trimmed.StartsWith("```", StringComparison.Ordinal))
        {
            var newline = trimmed.IndexOf('\n');
            var closingFence = trimmed.LastIndexOf("```", StringComparison.Ordinal);
            if (newline >= 0 && closingFence > newline)
            {
                trimmed = trimmed[(newline + 1)..closingFence].Trim();
            }
        }
        else
        {
            var start = trimmed.IndexOf('{');
            var end = trimmed.LastIndexOf('}');
            if (start >= 0 && end > start)
            {
                trimmed = trimmed[start..(end + 1)];
            }
        }

        return trimmed;
    }
}
