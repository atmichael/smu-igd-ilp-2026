namespace ILP.Server.Features.AccountsPayable;

internal static class AccountsPayableDocumentType
{
    public static bool IsInvoiceOrCreditNote(string? documentType)
    {
        var normalizedType = documentType?.Trim().Replace('-', ' ').Replace('_', ' ');
        return string.Equals(normalizedType, "Invoice", StringComparison.OrdinalIgnoreCase)
            || string.Equals(normalizedType, "Credit Note", StringComparison.OrdinalIgnoreCase);
    }
}
