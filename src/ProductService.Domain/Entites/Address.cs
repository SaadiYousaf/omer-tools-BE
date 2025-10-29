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
        public string AddressType { get; set; } = string.Empty; // Home, Work, etc. 
        public string FullName { get; set; } = string.Empty;
        public string AddressLine1 { get; set; } = string.Empty;
        public string AddressLine2 { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public string State { get; set; } = string.Empty;
        public string PostalCode { get; set; } = string.Empty;
        public string Country { get; set; } = string.Empty;
        public bool IsDefault { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;

        public User User { get; set; }
    }
}
