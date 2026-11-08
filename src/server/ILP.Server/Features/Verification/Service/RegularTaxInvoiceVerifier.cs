using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.Verification.Service;

public static class RegularTaxInvoiceVerifier
{
    public static IReadOnlyList<string> RequiredFields(DocumentEntity document) => VerificationRuleModels.ForVariant("FullTaxInvoice").Select(field => field.Identifier).ToArray();
}
