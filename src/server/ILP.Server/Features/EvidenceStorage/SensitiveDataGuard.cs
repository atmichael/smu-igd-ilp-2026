using System.Text.RegularExpressions;
using ILP.Shared.Evidence;

namespace ILP.Server.Features.EvidenceStorage;

/// <summary>Keeps credentials and raw invoice values out of free-text audit fields and ordinary logs.</summary>
public static partial class SensitiveDataGuard
{
    public const string Redacted = "[REDACTED]";
    private const int MinimumValueLengthToRedact = 3;

    [GeneratedRegex(@"(password|passwd|secret|token|api[-_]?key|authorization|credential|bearer)", RegexOptions.IgnoreCase)]
    private static partial Regex CredentialKeyPattern();

    [GeneratedRegex(@"(?i)(password|passwd|secret|token|api[-_]?key|authorization|credential)\s*[:=]\s*\S+|bearer\s+\S+")]
    private static partial Regex CredentialValuePattern();

    public static string SanitizeText(string? text, IReadOnlyCollection<string> protectedValues)
    {
        if (string.IsNullOrEmpty(text))
        {
            return string.Empty;
        }

        var sanitized = CredentialValuePattern().Replace(text, Redacted);
        foreach (var value in protectedValues)
        {
            sanitized = sanitized.Replace(value, Redacted, StringComparison.Ordinal);
        }

        return sanitized;
    }

    public static Dictionary<string, string>? SanitizeMetadata(
        IReadOnlyDictionary<string, string>? metadata,
        IReadOnlyCollection<string> protectedValues)
    {
        if (metadata is null)
        {
            return null;
        }

        return metadata.ToDictionary(
            entry => entry.Key,
            entry => CredentialKeyPattern().IsMatch(entry.Key) ? Redacted : SanitizeText(entry.Value, protectedValues));
    }

    /// <summary>Raw and current record values in the package; these belong in protected evidence fields, not free-text audit narrative.</summary>
    public static IReadOnlyCollection<string> ProtectedValues(EvidencePackage package) =>
        package.Documents
            .SelectMany(document => document.Records)
            .SelectMany(record => new[] { record.RawValue, record.CurrentValue })
            .Where(value => value is { Length: >= MinimumValueLengthToRedact })
            .Select(value => value!)
            .Distinct(StringComparer.Ordinal)
            .OrderByDescending(value => value.Length)
            .ToList();
}
