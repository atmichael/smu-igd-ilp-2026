using System.Globalization;
using System.Text;

namespace ILP.Server.Features.EvidenceStorage.Endpoints;

/// <summary>Reduces a client-supplied file name to safe display text; the result is never used for storage or type detection.</summary>
public static class OriginalFileNameSanitizer
{
    public const int MaxLength = 255;

    public static string? Sanitize(string? fileName)
    {
        if (string.IsNullOrEmpty(fileName))
        {
            return null;
        }

        var segment = fileName[(fileName.LastIndexOfAny(['/', '\\']) + 1)..];
        var cleaned = new StringBuilder(segment.Length);
        foreach (var character in segment)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) is UnicodeCategory.Control or UnicodeCategory.Format)
            {
                continue;
            }

            cleaned.Append(character);
        }

        var result = cleaned.ToString().Trim();
        if (result.Length > MaxLength)
        {
            // Avoid cutting a surrogate pair in half.
            var length = char.IsHighSurrogate(result[MaxLength - 1]) ? MaxLength - 1 : MaxLength;
            result = result[..length].TrimEnd();
        }

        return result.Length == 0 ? null : result;
    }
}
