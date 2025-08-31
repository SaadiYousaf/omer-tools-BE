using Microsoft.Extensions.Logging;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using System;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class MockPaymentProcessor : IPaymentProcessor
    {
        private readonly ILogger<MockPaymentProcessor> _logger;
        private readonly Random _random = new Random();

        public MockPaymentProcessor(ILogger<MockPaymentProcessor> logger)
        {
            _logger = logger;
        }

        public Task<PaymentResult> ProcessPaymentAsync(PaymentInfo paymentInfo)
        {
            _logger.LogInformation("Mock payment processing for amount: {Amount} {Currency}",
                paymentInfo.Amount, paymentInfo.Currency);

            // For testing purposes only - don't process real payments
            _logger.LogWarning("Mock processor is being used. This should only happen in test environments.");

            // Check if test card number is provided (like Stripe test cards)
            if (paymentInfo.CardData != null && IsStripeTestCard(paymentInfo.CardData.Number))
            {
                _logger.LogWarning("Stripe test card detected but using mock processor. " +
                    "Switch to StripePaymentProcessor for development with test cards.");
            }

            // Simulate different payment outcomes
            int outcome = _random.Next(0, 10);

            if (outcome < 7) // 70% success
            {
                _logger.LogInformation("Mock payment succeeded");
                return Task.FromResult(PaymentResult.CreateSuccess(
                    transactionId: $"MOCK_{Guid.NewGuid()}",
                    last4: "4242",
                    brand: "Visa",
                    expMonth: 12,
                    expYear: 2025
                ));
            }
            else if (outcome < 9) // 20% requires action (3D Secure)
            {
                _logger.LogInformation("Mock payment requires action");
                return Task.FromResult(PaymentResult.CreateRequiresAction(
                    clientSecret: $"mock_client_secret_{Guid.NewGuid()}"
                ));
            }
            else // 10% failure
            {
                _logger.LogWarning("Mock payment declined");
                return Task.FromResult(PaymentResult.CreateFailed(
                    "mock_decline",
                    "Payment declined by mock processor"
                ));
            }
        }

        public Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
        {
            _logger.LogInformation("Mock refund for transaction: {TransactionId}, amount: {Amount}",
                transactionId, amount);

            return Task.FromResult(PaymentResult.CreateSuccess(
                transactionId: $"REFUND_{Guid.NewGuid()}"
            ));
        }

        private bool IsStripeTestCard(string cardNumber)
        {
            // Common Stripe test card numbers
            var testCards = new[]
            {
                "4242424242424242", // Visa (success)
                "4000002500003155", // Visa (requires authentication)
                "4000000000009995", // Visa (declined)
                "5555555555554444", // Mastercard (success)
                "5200828282828210", // Mastercard (requires authentication)
                "4000000000000028", // Mastercard (declined)
            };

            var cleanCardNumber = cardNumber?.Replace(" ", "").Replace("-", "");
            return Array.Exists(testCards, testCard => testCard == cleanCardNumber);
        }
    }
}