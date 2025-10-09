using System.ComponentModel.DataAnnotations;

namespace ProductService.Business.DTOs
{
    public class PaymentInfo
    {
        public PaymentInfo(string paymentMethod, string paymentMethodId, CardData cardData,
                          decimal amount, string currency, string customerEmail, string orderId)
        {
            PaymentMethod = paymentMethod;
            PaymentMethodId = paymentMethodId;
            CardData = cardData;
            Amount = amount;
            Currency = currency;
            CustomerEmail = customerEmail;
            OrderId = orderId;
        }

        public string PaymentMethod { get; }
        public string PaymentMethodId { get; }
        public CardData CardData { get; }
        public decimal Amount { get; }
        public string Currency { get; }
        public string CustomerEmail { get; }
        public string OrderId { get; }
    }

    public class CardData
    {
        public string Number { get; set; }

        public string Expiry { get; set; }

        public string Cvc { get; set; }

        public string Name { get; set; }
    }
}