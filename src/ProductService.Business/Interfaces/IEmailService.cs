using ProductService.Domain.Entities;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
    public interface IEmailService
    {
        Task SendOrderConfirmationAsync(Order order, string email);
        Task SendPaymentFailedNotificationAsync(Order order, string email, string error);
        Task SendOrderShippedNotificationAsync(Order order, string trackingNumber, string email);
    }
}