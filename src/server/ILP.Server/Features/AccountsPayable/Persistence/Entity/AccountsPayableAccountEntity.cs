namespace ILP.Server.Features.AccountsPayable.Persistence.Entity;

public sealed class AccountsPayableAccountEntity
{
    public string Code { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public string NormalBalance { get; set; } = "";
    public string ApRole { get; set; } = "";
    public string UseWhen { get; set; } = "";
    public bool Enabled { get; set; }
    public string Sector { get; set; } = "";
    public string AiSuggestion { get; set; } = "";
    public string Notes { get; set; } = "";
}
