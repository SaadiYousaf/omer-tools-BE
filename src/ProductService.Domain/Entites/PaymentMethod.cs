using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace ProductService.Domain.Entites
{
    public class PaymentMethod : BaseEntity
    {
        public string UserId { get; set; }
        public string PaymentType { get; set; } // CreditCard, PayPal, etc.
        public string Provider { get; set; } // Visa, MasterCard, etc.
        public string Last4Digits { get; set; }
        public string ExpiryMonth { get; set; }
        public string ExpiryYear { get; set; }
        public bool IsDefault { get; set; }
        public string PaymentMethodId { get; set; } // From payment processor

        public User User { get; set; }
    }
}
