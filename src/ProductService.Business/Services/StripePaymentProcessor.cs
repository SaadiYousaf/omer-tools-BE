using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using Stripe;
using System;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class StripePaymentProcessor : IPaymentProcessor
    {
        private readonly ILogger<StripePaymentProcessor> _logger;
        private readonly PaymentIntentService _paymentIntentService;
        private readonly PaymentMethodService _paymentMethodService;
        private readonly RefundService _refundService; // Added this field

        public StripePaymentProcessor(
            ILogger<StripePaymentProcessor> logger,
            PaymentIntentService paymentIntentService,
            PaymentMethodService paymentMethodService,
            RefundService refundService) // Added this parameter
        {
            _logger = logger;
            _paymentIntentService = paymentIntentService;
            _paymentMethodService = paymentMethodService;
            _refundService = refundService; // Initialize the field
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentInfo paymentInfo)
        {
            try
            {
                _logger.LogInformation("Processing Stripe payment for order: {OrderId}, amount: {Amount}",
                    paymentInfo.OrderId, paymentInfo.Amount);

                // Create payment method first if card data is provided
                string paymentMethodId = paymentInfo.PaymentMethodId;

                if (paymentInfo.CardData != null && string.IsNullOrEmpty(paymentMethodId))
                {
                    var paymentMethodOptions = new PaymentMethodCreateOptions
                    {
                        Type = "card",
                        Card = new PaymentMethodCardOptions
                        {
                            Number = paymentInfo.CardData.Number,
                            ExpMonth = long.Parse(paymentInfo.CardData.Expiry.Split('/')[0]),
                            ExpYear = long.Parse(paymentInfo.CardData.Expiry.Split('/')[1]),
                            Cvc = paymentInfo.CardData.Cvc
                        },
                        BillingDetails = new PaymentMethodBillingDetailsOptions
                        {
                            Name = paymentInfo.CardData.Name,
                            Email = paymentInfo.CustomerEmail
                        }
                    };

                    var createdPaymentMethod = await _paymentMethodService.CreateAsync(paymentMethodOptions); // Renamed variable
                    paymentMethodId = createdPaymentMethod.Id;
                }

                // Create payment intent
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(paymentInfo.Amount * 100),
                    Currency = paymentInfo.Currency?.ToLower() ?? "usd",
                    PaymentMethod = paymentMethodId,
                    Confirm = true,
                    ConfirmationMethod = "automatic",
                    Description = $"Payment for order #{paymentInfo.OrderId}",
                    Metadata = new System.Collections.Generic.Dictionary<string, string>
                    {
                        { "order_id", paymentInfo.OrderId },
                        { "customer_email", paymentInfo.CustomerEmail }
                    },
                    ReceiptEmail = paymentInfo.CustomerEmail,
                    ReturnUrl = "https://your-app.com/payment/return"
                };

                _logger.LogInformation("Creating payment intent with options: {@Options}", options);

                var paymentIntent = await _paymentIntentService.CreateAsync(options);
                PaymentMethod retrievedPaymentMethod = null; // Renamed variable

                if (!string.IsNullOrEmpty(paymentMethodId))
                {
                    retrievedPaymentMethod = await _paymentMethodService.GetAsync(paymentMethodId);
                }

                _logger.LogInformation("Payment intent created with status: {Status}", paymentIntent.Status);

                if (paymentIntent.Status == "succeeded")
                {
                    _logger.LogInformation("Payment succeeded: {PaymentId}", paymentIntent.Id);
                    return PaymentResult.CreateSuccess(
                        transactionId: paymentIntent.Id,
                        last4: retrievedPaymentMethod?.Card?.Last4, // Updated variable name
                        brand: retrievedPaymentMethod?.Card?.Brand, // Updated variable name
                        expMonth: (int?)retrievedPaymentMethod?.Card?.ExpMonth, // Updated variable name
                        expYear: (int?)retrievedPaymentMethod?.Card?.ExpYear // Updated variable name
                    );
                }

                if (paymentIntent.Status == "requires_action")
                {
                    _logger.LogInformation("Payment requires additional action: 3D Secure");
                    return PaymentResult.CreateRequiresAction(paymentIntent.ClientSecret);
                }

                if (paymentIntent.Status == "requires_payment_method")
                {
                    _logger.LogWarning("Payment requires payment method: {ErrorMessage}",
                        paymentIntent.LastPaymentError?.Message);
                    return PaymentResult.CreateFailed(
                        "payment_method_required",
                        paymentIntent.LastPaymentError?.Message ?? "Payment method is required"
                    );
                }

                _logger.LogWarning("Payment failed: {Status}, {Error}",
                    paymentIntent.Status, paymentIntent.LastPaymentError?.Message);

                return PaymentResult.CreateFailed(
                    paymentIntent.LastPaymentError?.Code ?? "payment_failed",
                    paymentIntent.LastPaymentError?.Message ?? "Payment processing failed"
                );
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe payment processing error: {Message}", ex.Message);
                _logger.LogError("Stripe error details: {StripeError}", ex.StripeError?.ToString());

                return ex.StripeError?.Type switch
                {
                    "card_error" => PaymentResult.CreateFailed(
                        ex.StripeError.Code,
                        ex.StripeError.Message
                    ),
                    _ => PaymentResult.CreateFailed(
                        "stripe_error",
                        ex.Message
                    )
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during payment processing");
                return PaymentResult.CreateFailed(
                    "system_error",
                    "Payment processing failed"
                );
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
        {
            try
            {
                _logger.LogInformation("Processing Stripe refund for: {TransactionId}, amount: {Amount}",
                    transactionId, amount);

                var options = new RefundCreateOptions
                {
                    PaymentIntent = transactionId,
                    Amount = (long)(amount * 100),
                    Reason = "requested_by_customer"
                };

                var refund = await _refundService.CreateAsync(options); // Using the field now

                if (refund.Status == "succeeded")
                {
                    _logger.LogInformation("Refund succeeded: {RefundId}", refund.Id);
                    return PaymentResult.CreateSuccess(refund.Id);
                }

                _logger.LogWarning("Refund failed: {Status}", refund.Status);
                return PaymentResult.CreateFailed("refund_failed", $"Refund status: {refund.Status}");
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe refund error: {Message}", ex.Message);
                return PaymentResult.CreateFailed(
                    ex.StripeError?.Code ?? "stripe_error",
                    ex.StripeError?.Message ?? ex.Message
                );
            }
        }
    }
}