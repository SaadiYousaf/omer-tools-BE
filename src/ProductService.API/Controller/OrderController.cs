using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using System;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ProductService.API.Controllers
{
    [ApiController]
    [Route("api/orders")]
    [Authorize]
    public class OrderController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly IPaymentService _paymentService;
        private readonly IEmailService _emailService;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<OrderController> _logger;

        public OrderController(
            IOrderService orderService,
            IPaymentService paymentService,
            IEmailService emailService,
            IUserRepository userRepository,
            ILogger<OrderController> logger)
        {
            _orderService = orderService;
            _paymentService = paymentService;
            _emailService = emailService;
            _userRepository = userRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] OrderCreationRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(new { errors = ModelState });

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = User.FindFirstValue(ClaimTypes.Email) ?? request.UserEmail;

            if (string.IsNullOrWhiteSpace(userId))
                return Unauthorized(new { message = "User not authenticated" });

            if (string.IsNullOrWhiteSpace(userEmail))
                return BadRequest(new { message = "Email address is required" });

            try
            {
                // Check if userId is an email (old format) and resolve to GUID if needed
                if (userId.Contains("@"))
                {
                    // userId is an email, we need to find the actual user ID
                    var user = await _userRepository.GetUserByEmailAsync(userId);
                    if (user == null)
                    {
                        return Unauthorized(new { message = "User not found" });
                    }
                    userId = user.Id;
                }

                // Map request items (DTO) to domain items
                var items = request.OrderItems.Select(i => new OrderItem
                {
                    Id = Guid.NewGuid().ToString(),
                    ProductId = i.ProductId,
                    ProductName = i.ProductName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice,
                    ImageUrl = i.ImageUrl
                }).ToList();

                // Calculate total
                var total = items.Sum(i => i.UnitPrice * i.Quantity);

                // Process payment first (without order reference)
                var paymentInfo = new PaymentInfo(
                    paymentMethod: request.PaymentMethod,
                    cardData: request.CardData,
                    amount: total,
                    currency: "USD",
                    customerEmail: userEmail,
                    orderId: null, // Don't pass an order ID yet
                    paymentMethodId: request.PaymentMethodId
                );

                var paymentResult = await _paymentService.ProcessPaymentAsync(paymentInfo);

                if (paymentResult.RequiresAction)
                {
                    return Ok(new OrderCreationResponse
                    {
                        Status = "requires_action",
                        ClientSecret = paymentResult.ClientSecret,
                        Message = "Additional authentication required"
                    });
                }

                if (!paymentResult.IsSuccess)
                {
                    return BadRequest(new OrderCreationResponse
                    {
                        Status = "payment_failed",
                        ErrorCode = paymentResult.ErrorCode,
                        Message = paymentResult.ErrorMessage
                    });
                }

                // Payment succeeded, now create order
                var order = await _orderService.CreateOrderAsync(
                    userId,
                    request.SessionId,
                    paymentResult.TransactionId,
                    items
                );

                // Update shipping address directly without tracking issues
                await _orderService.UpdateOrderShippingAddressAsync(
                    order.Id,
                    new ShippingAddress
                    {
                        FullName = request.ShippingAddress.FullName,
                        AddressLine1 = request.ShippingAddress.AddressLine1,
                        AddressLine2 = request.ShippingAddress.AddressLine2,
                        City = request.ShippingAddress.City,
                        State = request.ShippingAddress.State,
                        PostalCode = request.ShippingAddress.PostalCode,
                        Country = request.ShippingAddress.Country
                    }
                );

                // Update payment record with the actual order ID
                await _paymentService.UpdatePaymentOrderIdAsync(paymentResult.TransactionId, order.Id);

                // Update order status to succeeded
                await _orderService.UpdateOrderStatusAsync(order.Id, "Succeeded");

                // Send confirmation email with proper exception handling
                try
                {
                    await _emailService.SendOrderConfirmationAsync(order, userEmail);
                    _logger.LogInformation("Order confirmation email sent successfully for order {OrderId}", order.Id);
                }
                catch (Exception emailEx)
                {
                    // Log email failure but don't fail the order
                    _logger.LogError(emailEx, "Failed to send order confirmation email for order {OrderId}", order.Id);

                    // You could also save this to a database table for retry later
                    // await _failedEmailService.QueueFailedEmail(order, userEmail, emailEx.Message);
                }

                return Ok(new OrderCreationResponse
                {
                    Status = "succeeded",
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    TransactionId = paymentResult.TransactionId,
                    Message = "Order created successfully. Email notification may be delayed."
                });
            }
            catch (InvalidOperationException ex) when (ex.Message.Contains("Insufficient stock"))
            {
                _logger.LogWarning(ex, "Insufficient stock for order");
                return BadRequest(new OrderCreationResponse
                {
                    Status = "insufficient_stock",
                    Message = "One or more products are out of stock"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Order creation failed");
                return StatusCode(500, new { Error = "Internal server error", Details = ex.Message });
            }
        }

        [HttpGet("user/{userId}")]
        public async Task<IActionResult> GetOrdersByUser(string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (currentUserId != userId) return Forbid();

            var orders = await _orderService.GetOrdersByUserAsync(userId);
            return Ok(orders);
        }

        [HttpGet("{orderId}")]
        public async Task<IActionResult> GetOrderById(string orderId)
        {
            try
            {
                var order = await _orderService.GetOrderByIdAsync(orderId);
                if (order == null)
                    return NotFound(new { message = "Order not found" });

                // Check if the current user has access to this order
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (order.UserId != currentUserId)
                    return Forbid();

                return Ok(order);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", orderId);
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }

        [HttpPost("{orderId}/refund")]
        public async Task<IActionResult> RefundOrder(string orderId, [FromBody] RefundRequest request)
        {
            try
            {
                var result = await _paymentService.RefundPaymentAsync(orderId, request.Amount);
                if (!result.IsSuccess)
                    return BadRequest(new { ErrorCode = result.ErrorCode, Message = result.ErrorMessage });

                return Ok(new { RefundId = result.TransactionId, Message = "Refund processed successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Refund failed for order {orderId}");
                return StatusCode(500, new { Error = "Internal server error" });
            }
        }
    }

    // DTO classes
    public class OrderItemRequest
    {
        [Required] public string ProductId { get; set; }
        [Required] public string ProductName { get; set; }
        [Range(1, int.MaxValue)] public int Quantity { get; set; }
        [Range(0.01, double.MaxValue)] public decimal UnitPrice { get; set; }
        public string ImageUrl { get; set; }
    }

    public class OrderCreationRequest
    {
        public string SessionId { get; set; }
        public string UserEmail { get; set; }

        [Required]
        public List<OrderItemRequest> OrderItems { get; set; } = new();

        [Required]
        public string PaymentMethod { get; set; }

        public string PaymentMethodId { get; set; }
        public CardData CardData { get; set; }

        [Required]
        public ShippingAddressRequest ShippingAddress { get; set; }
    }

    public class ShippingAddressRequest
    {
        [Required]
        public string FullName { get; set; }

        [Required]
        public string AddressLine1 { get; set; }

        public string AddressLine2 { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string State { get; set; }

        [Required]
        public string PostalCode { get; set; }

        [Required]
        public string Country { get; set; }
    }

    public class OrderCreationResponse
    {
        public string Status { get; set; }
        public string OrderId { get; set; }
        public string OrderNumber { get; set; }
        public string TransactionId { get; set; }
        public string ClientSecret { get; set; }
        public string ErrorCode { get; set; }
        public string Message { get; set; }
    }

    public class RefundRequest
    {
        [Range(0.01, double.MaxValue)]
        public decimal Amount { get; set; }
    }
}