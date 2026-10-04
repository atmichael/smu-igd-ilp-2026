namespace ILP.Shared.Model.Dto
{
    public class DocumentItemDto
    {
        public long? Id { get; private set; }
        public string Description { get; private set; }
        public decimal Quantity { get; private set; }
        public string CurrencyCode { get; private set; } = "SGD";
        public decimal UnitPrice { get; private set; }
        public string TaxName { get; private set; } = "GST";
        public decimal TaxRate { get; private set; } = 0.0m;

        public DocumentItemDto()
        {

        }

        public DocumentItemDto(
            decimal quantity,
            decimal unitPrice,
            long? id = null,
            string description = "",
            string currencyCode = "SGD",
            string taxName = "GST",
            decimal taxRate = 0.0m)
        {
            Quantity = quantity;
            UnitPrice = unitPrice;
            TaxRate = taxRate;
            Description = description;
        }

        public decimal GetBasePrice()
        {
            var basePrice = Quantity * UnitPrice;
            return Math.Round(basePrice, 2, MidpointRounding.ToEven);
        }

        public decimal GetTaxAmount()
        {
            var basePrice = GetBasePrice();
            return Math.Round(basePrice * TaxRate, 2, MidpointRounding.ToEven);
        }

        public decimal GetTotalAmount()
        {
            var basePrice = GetBasePrice();
            var taxAmount = GetTaxAmount();
            return Math.Round(basePrice + taxAmount, 2, MidpointRounding.ToEven);
        }
    }
}