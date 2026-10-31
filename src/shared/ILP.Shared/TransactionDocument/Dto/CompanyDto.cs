using System;
using System.Collections.Generic;
using System.Text;

namespace ILP.Shared.TransactionDocument.Dto
{
    public class CompanyDto
    {
        public long Id { get; private set; }
        public string UEN { get; private set; }
        public string Name { get; private set; }
        public string Address { get; private set; }
        public string TaxRegistrationNumber { get; set; }
        public string Email { get; private set; } = string.Empty;

        public CompanyDto()
        {

        }

        public CompanyDto(string name, string uen, string address = "", string taxRegistrationNumber = "", long id = 0, string email = "")
        {
            Id = id;
            Name = name;
            UEN = uen;
            Address = address;
            TaxRegistrationNumber = taxRegistrationNumber;
            Email = email;
        }
    }
}
