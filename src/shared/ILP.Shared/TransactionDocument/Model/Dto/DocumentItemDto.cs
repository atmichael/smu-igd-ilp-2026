namespace ILP.Shared.TransactionDocument.Model.Dto
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
        public int? SerialNumber { get; private set; }
        public decimal? Amount { get; private set; }
        public decimal? TaxAmount { get; private set; }

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
            decimal taxRate = 0.0m,
            int? serialNumber = null,
            decimal? amount = null,
            decimal? taxAmount = null)
        {
            Id = id;
            Quantity = quantity;
            UnitPrice = unitPrice;
            CurrencyCode = currencyCode;
            TaxName = taxName;
            TaxRate = taxRate;
            Description = description;
            SerialNumber = serialNumber;
            Amount = amount;
            TaxAmount = taxAmount;
        }

        public decimal GetBasePrice()
        {
            var basePrice = Amount ?? Quantity * UnitPrice;
            return Math.Round(basePrice, 2, MidpointRounding.ToEven);
        }

        public decimal GetTaxAmount()
        {
            var basePrice = GetBasePrice();
            return TaxAmount ?? Math.Round(basePrice * TaxRate, 2, MidpointRounding.ToEven);
        }

        public decimal GetTotalAmount()
        {
            var basePrice = GetBasePrice();
            var taxAmount = GetTaxAmount();
            return Math.Round(basePrice + taxAmount, 2, MidpointRounding.ToEven);
        }
    }
}