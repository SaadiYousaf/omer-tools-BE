using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using Stripe;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class StripePaymentProcessor : IPaymentProcessor
    {
        private readonly ILogger<StripePaymentProcessor> _logger;
        private readonly PaymentIntentService _paymentIntentService;
        private readonly Stripe.PaymentMethodService _stripePaymentMethodService;
        private readonly RefundService _refundService;

        public StripePaymentProcessor(
            ILogger<StripePaymentProcessor> logger,
            PaymentIntentService paymentIntentService,
            Stripe.PaymentMethodService stripePaymentMethodService,
            RefundService refundService)
        {
            _logger = logger;
            _paymentIntentService = paymentIntentService;
            _stripePaymentMethodService = stripePaymentMethodService;
            _refundService = refundService;
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentInfo paymentInfo)
        {
            try
            {
                _logger.LogInformation("Processing Stripe payment for order: {OrderId}, amount: {Amount}",
                    paymentInfo.OrderId, paymentInfo.Amount);

                // Create payment method first if card data is provided
                string paymentMethodId = paymentInfo.PaymentMethodId;

                //if (paymentInfo.CardData != null && string.IsNullOrEmpty(paymentMethodId))
                //{
                //    var paymentMethodOptions = new PaymentMethodCreateOptions
                //    {
                //        Type = "card",
                //        Card = new PaymentMethodCardOptions
                //        {
                //            Number = paymentInfo.CardData.Number,
                //            ExpMonth = long.Parse(paymentInfo.CardData.Expiry.Split('/')[0]),
                //            ExpYear = long.Parse(paymentInfo.CardData.Expiry.Split('/')[1]),
                //            Cvc = paymentInfo.CardData.Cvc
                //        },
                //        BillingDetails = new PaymentMethodBillingDetailsOptions
                //        {
                //            Name = paymentInfo.CardData.Name,
                //            Email = paymentInfo.CustomerEmail
                //        }
                //    };

                //    var createdPaymentMethod = await _stripePaymentMethodService.CreateAsync(paymentMethodOptions);
                //    paymentMethodId = createdPaymentMethod.Id;

                //    // Add test for specific declining card numbers
                //    if (IsTestCardThatShouldFail(paymentInfo.CardData.Number))
                //    {
                //        _logger.LogWarning("Test card that should fail was used: {CardNumber}",
                //            MaskCardNumber(paymentInfo.CardData.Number));
                //        return PaymentResult.CreateFailed(
                //            "card_declined",
                //            "Your card was declined. Please use a different payment method."
                //        );
                //    }
                //}
                // Validate that we have a payment method ID
                if (string.IsNullOrEmpty(paymentMethodId))
                {
                    _logger.LogError("Payment method ID is required");
                    return PaymentResult.CreateFailed(
                        "payment_method_required",
                        "Payment method is required"
                    );
                }
                // Create payment intent with the provided PaymentMethod ID
                var options = new PaymentIntentCreateOptions
                {
                    Amount = (long)(paymentInfo.Amount * 100),
                    Currency = paymentInfo.Currency?.ToLower() ?? "aud",
                    PaymentMethod = paymentMethodId, // ✅ Only use the Stripe PaymentMethod ID
                    Confirm = true,
                    ConfirmationMethod = "automatic",
                    Description = $"Payment for order #{paymentInfo.OrderId}",
                    Metadata = new Dictionary<string, string>
            {
                { "order_id", paymentInfo.OrderId },
                { "customer_email", paymentInfo.CustomerEmail }
            },
                    ReceiptEmail = paymentInfo.CustomerEmail,
                    ReturnUrl = "https://omertools.com.au/payment/return" // Update this to your actual return URL
                };

                _logger.LogInformation("Creating payment intent with options: {@Options}", options);

                var paymentIntent = await _paymentIntentService.CreateAsync(options);
                PaymentMethod retrievedPaymentMethod = null;

                if (!string.IsNullOrEmpty(paymentMethodId))
                {
                    retrievedPaymentMethod = await _stripePaymentMethodService.GetAsync(paymentMethodId);
                }

                _logger.LogInformation("Payment intent created with status: {Status}", paymentIntent.Status);

                // Handle different payment intent statuses
                switch (paymentIntent.Status)
                {
                    case "succeeded":
                        _logger.LogInformation("Payment succeeded: {PaymentId}", paymentIntent.Id);
                        return PaymentResult.CreateSuccess(
                            transactionId: paymentIntent.Id,
                            last4: retrievedPaymentMethod?.Card?.Last4,
                            brand: retrievedPaymentMethod?.Card?.Brand,
                            expMonth: (int?)retrievedPaymentMethod?.Card?.ExpMonth,
                            expYear: (int?)retrievedPaymentMethod?.Card?.ExpYear
                        );

                    case "requires_action":
                        _logger.LogInformation("Payment requires additional action: 3D Secure");
                        return PaymentResult.CreateRequiresAction(paymentIntent.ClientSecret);

                    case "requires_payment_method":
                        _logger.LogWarning("Payment requires payment method: {ErrorMessage}",
                            paymentIntent.LastPaymentError?.Message);
                        return PaymentResult.CreateFailed(
                            "payment_method_required",
                            paymentIntent.LastPaymentError?.Message ?? "Payment method is required"
                        );

                    case "canceled":
                        _logger.LogWarning("Payment was canceled: {ErrorMessage}",
                            paymentIntent.LastPaymentError?.Message);
                        return PaymentResult.CreateFailed(
                            "payment_canceled",
                            paymentIntent.LastPaymentError?.Message ?? "Payment was canceled"
                        );

                    default:
                        _logger.LogWarning("Payment failed with status: {Status}, {Error}",
                            paymentIntent.Status, paymentIntent.LastPaymentError?.Message);
                        return PaymentResult.CreateFailed(
                            paymentIntent.LastPaymentError?.Code ?? "payment_failed",
                            paymentIntent.LastPaymentError?.Message ?? $"Payment processing failed with status: {paymentIntent.Status}"
                        );
                }
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

        private bool IsTestCardThatShouldFail(string cardNumber)
        {
            // Remove any spaces or non-digit characters
            var cleanCardNumber = cardNumber?.Replace(" ", "").Replace("-", "") ?? "";

            // Test cards that should be declined
            var failingTestCards = new List<string>
            {
                "4000000000000002", // Generic decline
                "4000000000009995", // Insufficient funds
                "4100000000000019", // Processing error
                "4000000000000127", // Incorrect CVC
                "4000000000000069", // Expired card
                "4000000000000119"  // Incorrect number
            };

            return failingTestCards.Contains(cleanCardNumber);
        }

        private string MaskCardNumber(string cardNumber)
        {
            if (string.IsNullOrEmpty(cardNumber) || cardNumber.Length < 4)
                return "****";

            return $"**** **** **** {cardNumber.Substring(cardNumber.Length - 4)}";
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