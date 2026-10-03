using System;
using System.Collections.Generic;
using System.Text;

namespace ILP.Shared.Model.Dto
{
    public class DocumentDto
    {
        public long Id { get; private set; }
        public string RefNumber { get; private set; }
        public string TypeCode { get; private set; }
        public DateTime SentDate { get; private set; }
        public decimal TotalAmount { get; private set; }

        public List<DocumentItemDto> DocumentItems { get; private set; } = [];
    }
}