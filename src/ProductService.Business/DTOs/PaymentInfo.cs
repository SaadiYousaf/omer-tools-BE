// ProductService/Business/DTOs/PaymentInfo.cs
using System.ComponentModel.DataAnnotations;

public class PaymentInfo
{
    public string PaymentMethod { get; set; }
    public CardData CardData { get; set; }
    public decimal Amount { get; set; }
    public string Currency { get; set; }
    public string CustomerEmail { get; set; }
    public string OrderId { get; set; }
    public string PaymentMethodId { get; set; }

    public PaymentInfo(string paymentMethod, CardData cardData, decimal amount, string currency,
                      string customerEmail, string orderId, string paymentMethodId)
    {
        PaymentMethod = paymentMethod;
        CardData = cardData;
        Amount = amount;
        Currency = currency;
        CustomerEmail = customerEmail;
        OrderId = orderId;
        PaymentMethodId = paymentMethodId;
    }
}

public class CardData
{
    [Required]
    public string Number { get; set; }

    [Required]
    public string Expiry { get; set; }

    [Required]
    public string Cvc { get; set; }

    [Required]
    public string Name { get; set; }
}
