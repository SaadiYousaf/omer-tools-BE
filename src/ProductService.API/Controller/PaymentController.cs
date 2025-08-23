using Microsoft.AspNetCore.Mvc;
using ProductService.API.RequestHelper;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entities;
using System;
using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;

namespace ProductService.API.Controller
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentController : ControllerBase
    {
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly IOrderService _orderService;
        private readonly IEmailService _emailService;
        private readonly ILogger<PaymentController> _logger;

        public PaymentController(
            IPaymentProcessor paymentProcessor,
            IOrderService orderService,
            IEmailService emailService,
            ILogger<PaymentController> logger)
        {
            _paymentProcessor = paymentProcessor;
            _orderService = orderService;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("process")]
        public async Task<IActionResult> ProcessPayment([FromBody] PaymentRequest request)
        {
            try
            {
                // Validate request
                var validationResult = ValidateRequest(request);
                if (!validationResult.IsValid)
                {
                    _logger.LogWarning("Payment validation failed: {Errors}", string.Join(", ", validationResult.Errors));
                    return BadRequest(new { Errors = validationResult.Errors });
                }

                // Process payment
                // In the ProcessPayment method:
                var paymentResult = await _paymentProcessor.ProcessPaymentAsync(
                    new PaymentInfo(
                        paymentMethod: request.PaymentMethod,
                        cardData: request.CardData,
                        amount: request.Amount,
                        currency: request.Currency,
                        customerEmail: request.UserEmail,   // Added
                        orderId: Guid.NewGuid().ToString(), // Generate temporary ID
                        paymentMethodId: request.PaymentMethod // Use method as ID
                    )
                );

                if (!paymentResult.IsSuccess)
                {
                    _logger.LogWarning("Payment failed: {Error}", paymentResult.ErrorMessage);
                    return BadRequest(new PaymentResponse
                    {
                        Success = false,
                        ErrorCode = paymentResult.ErrorCode,
                        Message = paymentResult.ErrorMessage
                    });
                }

                // Create order
                var order = await _orderService.CreateOrderAsync(
                    request.UserId,
                    request.SessionId,
                    paymentResult.TransactionId,
                    request.OrderItems
                );

                // Send confirmation email (fire-and-forget)
                if (!string.IsNullOrEmpty(request.UserEmail))
                {
                    _ = _emailService.SendOrderConfirmationAsync(order, request.UserEmail);
                }

                _logger.LogInformation("Order {OrderId} created successfully", order.Id);

                return Ok(new PaymentResponse
                {
                    Success = true,
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    TransactionId = paymentResult.TransactionId,
                    Message = "Payment processed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment processing error");
                return StatusCode(500, new PaymentResponse
                {
                    Success = false,
                    Message = "Internal server error",
                    ErrorCode = "SYSTEM_ERROR"
                });
            }
        }

        private CustomValidationResult ValidateRequest(PaymentRequest request)
        {
            var result = new CustomValidationResult();

            if (request.PaymentMethod == "credit")
            {
                if (request.CardData == null)
                    result.AddError("Card data is required for credit card payments");

                if (!CardValidator.IsValidCardNumber(request.CardData?.Number))
                    result.AddError("Invalid card number");

                if (!CardValidator.IsValidExpiry(request.CardData?.Expiry))
                    result.AddError("Invalid expiry date");
            }

            if (request.Amount <= 0)
                result.AddError("Invalid payment amount");

            if (string.IsNullOrEmpty(request.UserId))
                result.AddError("User ID is required");

            return result;
        }
    }

    // Request/Response Models
    public class PaymentRequest
    {
        [Required] public string PaymentMethod { get; set; } = string.Empty;
        public CardData? CardData { get; set; }
        [Required] public string UserId { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;
        [Required] public decimal Amount { get; set; }
        [Required] public string Currency { get; set; } = "USD";
        [Required] public List<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
        [EmailAddress] public string UserEmail { get; set; } = string.Empty;
    }

 

    public class PaymentResponse
    {
        public bool Success { get; set; }
        public string OrderId { get; set; } = string.Empty;
        public string OrderNumber { get; set; } = string.Empty;
        public string TransactionId { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public string ErrorCode { get; set; } = string.Empty;
    }

}