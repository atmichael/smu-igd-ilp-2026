using System;
using System.Collections.Generic;
using System.Text;

namespace ILP.Shared.Model.Dto
{
    public class CompanyDto
    {
        public long Id { get; private set; }
        public string UEN { get; private set; }
        public string Name { get; private set; }
        public string Address { get; private set; }
        public string TaxRegistrationNumber { get; set; }
    }
}
