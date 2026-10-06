using System;
using System.Collections.Generic;
using System.Text;

namespace ILP.Shared.TransactionDocument.Model.Dto
{
    public class DocumentDto
    {
        public long Id { get; private set; }
        public string RefNumber { get; private set; }
        public string TypeCode { get; private set; }
        public DateTime SentDate { get; private set; }
        public decimal TotalAmount { get; private set; }
        public string? RelatedDocumentNumbers { get; private set; }
        public decimal Subtotal { get; private set; }
        public decimal SubtotalTaxRate { get; private set; }
        public decimal SubtotalTaxAmount { get; private set; }

        public List<DocumentItemDto> DocumentItems { get; private set; } = [];

        public DocumentDto()
        {

        }

        public DocumentDto(
            string refNumber,
            string typeCode,
            DateTime sentDate,
            decimal totalAmount,
            IEnumerable<DocumentItemDto>? documentItems = null,
            long id = 0,
            string? relatedDocumentNumbers = null,
            decimal subtotal = 0m,
            decimal subtotalTaxRate = 0m,
            decimal subtotalTaxAmount = 0m)
        {
            Id = id;
            RefNumber = refNumber;
            TypeCode = typeCode;
            SentDate = sentDate;
            TotalAmount = totalAmount;
            RelatedDocumentNumbers = relatedDocumentNumbers;
            Subtotal = subtotal;
            SubtotalTaxRate = subtotalTaxRate;
            SubtotalTaxAmount = subtotalTaxAmount;
            DocumentItems = documentItems?.ToList() ?? [];
        }
    }
}