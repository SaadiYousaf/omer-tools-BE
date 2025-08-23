// ProductService/Business/Interfaces/IPaymentProcessor.cs
using ProductService.Business.DTOs;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IPaymentProcessor
    {
        Task<PaymentResult> ProcessPaymentAsync(PaymentInfo paymentInfo);
        Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount);
    }
}