namespace ILP.Server.Features.AccountsPayable.Model;

public sealed record AccountsPayableReferenceAccount(
    string Code,
    string Name,
    string Type,
    string NormalBalance,
    string ApRole,
    bool Enabled,
    string UseWhen = "",
    string Sector = "",
    string AiSuggestion = "",
    string Notes = "");

public sealed record AccountsPayableReferenceGstCode(
    string Code,
    string Description,
    bool CanAutoProcess,
    string ReviewConditions = "");

public sealed record AccountsPayableCodingResult(
    string DocumentId,
    string VendorName,
    string InvoiceNumber,
    string Currency,
    IReadOnlyList<string> SourceEvidence,
    string GstCode,
    IReadOnlyList<AccountsPayableCoaSuggestion> CoaSuggestions,
    IReadOnlyList<AccountsPayableJournalLine> Journal,
    IReadOnlyList<AccountsPayableValidation> Validation,
    IReadOnlyList<string> ReviewReasons,
    string Status,
    bool Posted);

public sealed record AccountsPayableCoaSuggestion(
    int LineIndex,
    string AccountCode,
    string AccountName,
    decimal NetAmount,
    string Reason);

public sealed record AccountsPayableJournalLine(
    string AccountCode,
    decimal Debit,
    decimal Credit);

public sealed record AccountsPayableValidation(string Check, bool Passed);

public sealed record AccountsPayableReferenceData(
    IReadOnlyList<AccountsPayableReferenceAccount> Accounts,
    IReadOnlyList<AccountsPayableReferenceGstCode> GstCodes);
