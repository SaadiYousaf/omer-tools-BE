// In IPaymentService.cs
using ProductService.Business.DTOs;

public interface IPaymentService
{
    Task<PaymentResult> ProcessPaymentAsync(PaymentInfo paymentInfo);

    Task<PaymentResult> ProcessPayPalPaymentAsync(PaymentInfo paymentInfo);
    Task<PaymentResult> RefundPaymentAsync(string orderId, decimal amount);
    Task UpdatePaymentOrderIdAsync(string transactionId, string orderId);
}