using ILP.Server.Features.InboxMonitoring.Persistence.Entity;

namespace ILP.Server.Features.Verification.Service;

public static class ItemByItemGstVerifier
{
    public static decimal Calculate(DocumentEntity document) => VerificationCalculationPolicy.ItemByItemGst(
        document.DocumentItems.Select(item => (item.Amount ?? item.Quantity * item.UnitPrice, item.TaxRate)));
}
