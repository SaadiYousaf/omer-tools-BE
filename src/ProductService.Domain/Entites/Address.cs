using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace ProductService.Domain.Entites
{
    public class Address : BaseEntity
    {
        public string UserId { get; set; }
        public string AddressType { get; set; } // Home, Work, etc.
        public string FullName { get; set; }
        public string AddressLine1 { get; set; }
        public string AddressLine2 { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string PostalCode { get; set; }
        public string Country { get; set; }
        public bool IsDefault { get; set; }
        public string PhoneNumber { get; set; }

        public User User { get; set; }
    }
}
