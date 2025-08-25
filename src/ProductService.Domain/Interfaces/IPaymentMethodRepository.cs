// ProductService.Domain/Interfaces/IPaymentMethodRepository.cs
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Domain.Interfaces
{
    public interface IPaymentMethodRepository
    {
        Task<PaymentMethod> GetByIdAsync(string id);
        Task<IEnumerable<PaymentMethod>> GetByUserIdAsync(string userId);
        Task<PaymentMethod> GetDefaultPaymentMethodAsync(string userId);
        Task CreateAsync(PaymentMethod paymentMethod);
        Task UpdateAsync(PaymentMethod paymentMethod);
        Task<bool> DeleteAsync(string id);
        Task SetDefaultPaymentMethodAsync(string userId, string paymentMethodId);
    }
}