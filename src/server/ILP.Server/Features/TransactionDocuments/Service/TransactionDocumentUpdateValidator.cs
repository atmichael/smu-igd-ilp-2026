using ILP.Server.Features.TransactionDocuments.Model;

namespace ILP.Server.Features.TransactionDocuments.Service;

public static class TransactionDocumentUpdateValidator
{
    private const decimal Tolerance = 0.01m;

    public static IDictionary<string, string[]> Validate(UpdateTransactionDocumentRequest? request)
    {
        var errors = new Dictionary<string, string[]>();
        if (request is null)
        {
            errors["request"] = ["A request body is required."];
            return errors;
        }

        if (string.IsNullOrWhiteSpace(request.RefNumber))
        {
            errors["refNumber"] = ["The document number is required."];
        }

        if (request.SentDate == default)
        {
            errors["sentDate"] = ["The posting date is required."];
        }

        if (string.IsNullOrWhiteSpace(request.CompanyUen) && string.IsNullOrWhiteSpace(request.CompanyName))
        {
            errors["companyName"] = ["The company name is required when the company UEN is not provided."];
        }

        if (request.SubtotalTaxAmount < 0)
        {
            errors["subtotalTaxAmount"] = ["The tax amount cannot be negative."];
        }

        var items = request.DocumentItems ?? [];
        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            if (string.IsNullOrWhiteSpace(item.Description))
            {
                errors[$"documentItems[{index}].description"] = ["The item description is required."];
            }

            if (item.Quantity <= 0)
            {
                errors[$"documentItems[{index}].quantity"] = ["The item quantity must be greater than zero."];
            }

            if (item.UnitPrice < 0)
            {
                errors[$"documentItems[{index}].unitPrice"] = ["The item unit price cannot be negative."];
            }
        }

        if (errors.Count > 0)
        {
            return errors;
        }

        var itemsTotal = items.Sum(GetItemAmount);
        if (Math.Abs(request.Subtotal - itemsTotal) > Tolerance)
        {
            errors["subtotal"] = ["The subtotal does not tally with the line items."];
        }

        if (Math.Abs(request.TotalAmount - (request.Subtotal + request.SubtotalTaxAmount)) > Tolerance)
        {
            errors["totalAmount"] = ["The total amount does not tally with the subtotal and tax amount."];
        }

        return errors;
    }

    public static decimal GetItemAmount(UpdateTransactionDocumentItem item) =>
        item.Amount ?? item.Quantity * item.UnitPrice;
}
