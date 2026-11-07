namespace ILP.Server.Features.AccountsPayable.Persistence.Entity;

public sealed class AccountsPayableGstTaxCodeEntity
{
    public string Code { get; set; } = "";
    public string Description { get; set; } = "";
    public string SuggestedTreatment { get; set; } = "";
    public string InputGstAccount { get; set; } = "";
    public string AutoProcessing { get; set; } = "";
    public string ReviewConditions { get; set; } = "";
}
