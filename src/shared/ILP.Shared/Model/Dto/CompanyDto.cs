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

        public CompanyDto()
        {

        }

        public CompanyDto(string name, string uen, string address = "", string taxRegistrationNumber = "", long id = 0)
        {
            Id = id;
            Name = name;
            UEN = uen;
            Address = address;
            TaxRegistrationNumber = taxRegistrationNumber;
        }
    }
}
