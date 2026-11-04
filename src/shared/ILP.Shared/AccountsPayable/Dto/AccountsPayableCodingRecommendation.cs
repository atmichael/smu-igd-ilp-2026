namespace ILP.Shared.AccountsPayable.Dto;

public sealed record AccountsPayableCodingRecommendation
{
    public decimal Confidence { get; init; }
    public string GstCode { get; init; } = "";
    public IReadOnlyList<AccountsPayableLineSuggestion> CoaSuggestions { get; init; } = [];
    public IReadOnlyList<string> ReviewReasons { get; init; } = [];
}

public sealed record DocumentAPJournalDto(
    decimal? Confidence,
    string GstCode,
    IReadOnlyList<AccountsPayableLineSuggestion> CoaSuggestions,
    IReadOnlyList<string> VerificationNotes);

public sealed record AccountsPayableLineSuggestion(
    int LineIndex,
    string AccountCode,
    string Reason,
    bool RequiresReview = false);

public sealed record AccountsPayableAccountOption(
    string Code,
    string Name,
    string Type,
    string ApRole,
    bool Enabled,
    string UseWhen = "",
    string Sector = "",
    string AiSuggestion = "",
    string Notes = "");

public sealed record AccountsPayableGstOption(
    string Code,
    string Description,
    string AutoProcessing = "",
    string ReviewConditions = "");
