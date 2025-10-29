using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ProductService.Business.Interfaces;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entites;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UserService.Domain.Entities;

namespace ProductService.Business.Services
{
    public class OrderService : IOrderService
    {
        private readonly IUserRepository _userRepository;
        private readonly ProductDbContext _context;
        private readonly ILogger<OrderService> _logger;

        public OrderService(ProductDbContext context, ILogger<OrderService> logger, IUserRepository userRepository)
        {
          
            _userRepository = userRepository;
            _context = context;
            _logger = logger;
        }

        public async Task UpdateOrderShippingAddressAsync(string orderId,string userId,string userEmail, ShippingAddress shippingAddress, string phoneNumber)
        {
            try
            {
                // First check if the order exists
                var orderExists = await _context.Orders.AnyAsync(o => o.Id == orderId);
                if (!orderExists)
                {
                    throw new ArgumentException($"Order with ID {orderId} not found");
                }

                // Check if shipping address already exists for this order
                var existingAddress = await _context.ShippingAddresses
                    .FirstOrDefaultAsync(sa => sa.OrderId == orderId);
                var existingUser = await _context.Users
                   .FirstOrDefaultAsync(sa => sa.Email == userEmail);
              
                if (existingUser != null)
                {
                    var existingUserAddress = _userRepository.GetByIdAsync(existingUser.Id).Result.Addresses.FirstOrDefault();
                //    var existingUserAddress = await _context.Addresses
                //.FirstOrDefaultAsync(sa => sa.UserId == existingUser.Id);
                    if (existingUserAddress !=null)
                    {
                        existingUserAddress.AddressLine1 = shippingAddress.AddressLine1;
                        existingUserAddress.AddressLine2 = shippingAddress.AddressLine2;
                        existingUserAddress.City = shippingAddress.City;
                        existingUserAddress.State = shippingAddress.State;
                        existingUserAddress.PostalCode = shippingAddress.PostalCode;
                        existingUserAddress.Country = shippingAddress.Country;
                        existingUserAddress.PhoneNumber = phoneNumber;
                        existingUserAddress.UpdatedAt = DateTime.UtcNow;

                        _context.Update(existingUserAddress);
                    }
                    else
                    {
                        var userAddress = new Address
                        {
                            Id=Guid.NewGuid().ToString(),
                            UserId = userId,
                            AddressLine1 = shippingAddress.AddressLine1,
                            AddressLine2 = shippingAddress.AddressLine2,
                            City = shippingAddress.City,
                            State = shippingAddress.State,
                            PostalCode = shippingAddress.PostalCode,
                            Country = shippingAddress.Country,
                            PhoneNumber = phoneNumber,
                            AddressType = "Home",
                            IsDefault = true,
                        };
                        await _context.Addresses.AddAsync(userAddress);
                    }
                    //    existingUser.PhoneNumber = phoneNumber;
                    //_context.Users.Update(existingUser);
                }
                if (existingAddress != null)
                {
                    // Update existing address
                    existingAddress.FullName = shippingAddress.FullName;
                    existingAddress.AddressLine1 = shippingAddress.AddressLine1;
                    existingAddress.AddressLine2 = shippingAddress.AddressLine2;
                    existingAddress.City = shippingAddress.City;
                    existingAddress.State = shippingAddress.State;
                    existingAddress.PostalCode = shippingAddress.PostalCode;
                    existingAddress.Country = shippingAddress.Country;
                    existingAddress.PhoneNumber = phoneNumber;

                    _context.ShippingAddresses.Update(existingAddress);
                }
                else
                {
                    // Create new shipping address
                    shippingAddress.Id = Guid.NewGuid().ToString();
                    shippingAddress.OrderId = orderId;
                    shippingAddress.PhoneNumber=phoneNumber;
                    await _context.ShippingAddresses.AddAsync(shippingAddress);
                }

                // Update the order's UpdatedAt field
                await _context.Orders
                    .Where(o => o.Id == orderId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order {OrderId} shipping address updated successfully", orderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order shipping address {OrderId}", orderId);
                throw;
            }
        }

        // The rest of your OrderService methods remain the same...
        public async Task<Order> CreateOrderAsync(string userId, string sessionId, string transactionId, List<OrderItem> items, decimal shippingCost, bool isConfirmAndCollect = false, bool isGuestOrder = false,string guestFullName = "", string guestEmail = "", string phoneNumber="")
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    string finalUserId = userId;
                    if (isGuestOrder)
                    {
                        var guestUserExists = await _context.Users.FirstOrDefaultAsync(u => u.Email == guestEmail && u.LastName.Equals("Guest"));
                        if (guestUserExists != null)
                        {
                            finalUserId = guestUserExists.Id;
                        }
                        else
                        {
                            // Create a guest user
                            var guestUser = new User
                            {
                                Id = userId,
                                Email = guestEmail, // guest email
                                FirstName = guestFullName,
                                PhoneNumber = phoneNumber,
                                LastName = "Guest",
                                PasswordHash = "AQAAAAEAACcQAAAAEKyuwL7rPj6N8xIkI1b7cRkzJ1ZprJ2TkKjXh8Y5VqK1aB8nLmNzXpQwOyD4rK1A==",
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsGuest = true
                            };

                            _context.Users.Add(guestUser);
                            await _context.SaveChangesAsync(); // Save the user first
                            _logger.LogInformation("Created guest user: {UserId}", userId);
                        }
                    }
                    else
                    {
                        // For regular users, validate they exist
                        var userExists = await _context.Users.AnyAsync(u => u.Id == userId);
                        if (!userExists)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError("User {UserId} does not exist", userId);
                            throw new ArgumentException($"User {userId} does not exist");
                        }
                       
                    }

                    // Rest of your order creation code...
                    foreach (var item in items)
                    {
                        var stockReserved = await UpdateStockAsync(item.ProductId, -item.Quantity);
                        if (!stockReserved)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError("Failed to reserve stock for product {ProductId}", item.ProductId);
                            throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");
                        }
                    }

                    // Create order
                    var order = new Order
                    {
                        Id = Guid.NewGuid().ToString(),
                        UserId = finalUserId,
                        SessionId = sessionId,
                        TransactionId = transactionId,
                        OrderNumber = GenerateOrderNumber(),
                        Items = items,
                        TotalAmount = items.Sum(i => i.UnitPrice * i.Quantity) + shippingCost,
                        ShippingCost = shippingCost, // ✅ Save shipping cost
                        Status = "Pending",
                        PaymentStatus = "Pending",
                        CreatedAt = DateTime.UtcNow,
                         IsConfirmAndCollect = isConfirmAndCollect,
                        IsGuestOrder = isGuestOrder // ✅ ADD THIS FLAG TO YOUR ORDER ENTITY
                    };

                    await _context.Orders.AddAsync(order);
                    await _context.SaveChangesAsync();

                    // Commit transaction
                    await transaction.CommitAsync();

                    _logger.LogInformation("Order {OrderId} created successfully", order.Id);
                    return order;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to create order");
                    throw;
                }
            });
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Reserve stock for all items
                    foreach (var item in order.Items)
                    {
                        var stockReserved = await UpdateStockAsync(item.ProductId, -item.Quantity);
                        if (!stockReserved)
                        {
                            await transaction.RollbackAsync();
                            _logger.LogError("Failed to reserve stock for product {ProductId}", item.ProductId);
                            throw new InvalidOperationException($"Insufficient stock for product {item.ProductId}");
                        }
                    }

                    await _context.Orders.AddAsync(order);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Order {OrderId} created successfully for user {UserId}", order.Id, order.UserId);
                    return order;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to create order");
                    throw;
                }
            });
        }

        public async Task UpdateOrderAsync(Order order)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // Check if the order is already being tracked
                    var local = _context.Orders.Local.FirstOrDefault(o => o.Id == order.Id);
                    if (local != null)
                    {
                        // If it's already tracked, detach it
                        _context.Entry(local).State = EntityState.Detached;
                    }

                    // Attach the order and mark it as modified
                    _context.Orders.Attach(order);
                    _context.Entry(order).State = EntityState.Modified;

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Order {OrderId} updated successfully", order.Id);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Failed to update order {OrderId}", order.Id);
                    throw;
                }
            });
        }

        // In OrderService.cs
        public async Task<bool> UpdateOrderStatusAsync(string orderId, string status)
        {
            try
            {
                // Use ExecuteUpdate to avoid tracking issues
                var rowsUpdated = await _context.Orders
                    .Where(o => o.Id == orderId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(o => o.Status, status)
                        .SetProperty(o => o.PaymentStatus, status) // Update both status fields
                        .SetProperty(o => o.UpdatedAt, DateTime.UtcNow));

                if (rowsUpdated == 0)
                {
                    _logger.LogWarning("Order {OrderId} not found for status update", orderId);
                    return false;
                }

                _logger.LogInformation("Order {OrderId} status updated to {Status}", orderId, status);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update order {OrderId} status", orderId);
                return false;
            }
        }
        public async Task<List<Order>> GetAllOrdersAsync(string status = null)
        {
            try
            {
                var query = _context.Orders
                    .Include(o => o.Items)
                    .Include(o => o.ShippingAddress)
                    .Include(o => o.Payments)
                    .AsQueryable();

                if (!string.IsNullOrEmpty(status) && status.ToLower() != "all")
                {
                    query = query.Where(o => o.Status.ToLower() == status.ToLower());
                }

                return await query.OrderByDescending(o => o.CreatedAt).ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving orders with status filter: {Status}", status);
                throw;
            }
        }
        public async Task<IEnumerable<Order>> GetOrdersByUserAsync(string userId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.CreatedAt)
                .AsNoTracking()
                .ToListAsync();
        }

        public async Task<Order> GetOrderByIdAsync(string orderId)
        {
            return await _context.Orders
                .Include(o => o.Items)
                .AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == orderId);
        }

        private async Task<bool> UpdateStockAsync(string productId, int quantityChange)
        {
            try
            {
                var product = await _context.Products.FindAsync(productId);
                if (product == null)
                {
                    _logger.LogWarning("Product {ProductId} not found for stock update", productId);
                    return false;
                }

                if (product.StockQuantity + quantityChange < 0)
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId}. Current: {CurrentStock}, Requested: {RequestedChange}",
                        productId, product.StockQuantity, -quantityChange);
                    return false;
                }

                product.StockQuantity += quantityChange;

                // Don't call SaveChangesAsync here - it will be handled by the transaction
                // The change tracking will mark this entity as modified
                _context.Products.Update(product);

                _logger.LogInformation("Stock updated for product {ProductId}. New quantity: {NewQuantity}",
                    productId, product.StockQuantity);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating stock for product {ProductId}", productId);
                return false;
            }
        }
        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }
}