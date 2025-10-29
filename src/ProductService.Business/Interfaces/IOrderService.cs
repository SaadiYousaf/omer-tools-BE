// In IOrderService.cs
using ProductService.Domain.Entities;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string userId, string sessionId, string transactionId, List<OrderItem> orderItems ,decimal shippingCost, bool isConfirmAndCollect = false, bool isGuestOrder = false,string fullName="", string email = "",string phoneNumber="");
    Task UpdateOrderShippingAddressAsync(string orderId,string userId,string userEmail, ShippingAddress shippingAddress,string phoneNumber);
    Task<bool> UpdateOrderStatusAsync(string orderId, string status);
    Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId);
    Task<Order> GetOrderByIdAsync(string orderId);
    Task<Order> CreateOrderAsync(Order order);
    Task UpdateOrderAsync(Order order);
    Task<List<Order>> GetAllOrdersAsync(string status = null);

}