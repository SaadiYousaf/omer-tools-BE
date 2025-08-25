// ProductService.Business/Interfaces/IPaymentMethodService.cs
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IPaymentMethodService
    {
        Task<PaymentMethodDto> GetPaymentMethodByIdAsync(string paymentMethodId);
        Task<IEnumerable<PaymentMethodDto>> GetUserPaymentMethodsAsync(string userId);
        Task<PaymentMethodDto> CreatePaymentMethodAsync(string userId, CreatePaymentMethodDto paymentMethodDto);
        Task<PaymentMethodDto> UpdatePaymentMethodAsync(string userId, string paymentMethodId, UpdatePaymentMethodDto paymentMethodDto);
        Task<bool> DeletePaymentMethodAsync(string userId, string paymentMethodId);
        Task SetDefaultPaymentMethodAsync(string userId, string paymentMethodId);
    }
}
