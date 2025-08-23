// In IOrderService.cs
using ProductService.Domain.Entities;

public interface IOrderService
{
    Task<Order> CreateOrderAsync(string userId, string sessionId, string transactionId, List<OrderItem> orderItems);
    Task UpdateOrderShippingAddressAsync(string orderId, ShippingAddress shippingAddress);
    Task<bool> UpdateOrderStatusAsync(string orderId, string status);
    Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId);
    Task<Order> GetOrderByIdAsync(string orderId);
    Task<Order> CreateOrderAsync(Order order);
    Task UpdateOrderAsync(Order order);
}