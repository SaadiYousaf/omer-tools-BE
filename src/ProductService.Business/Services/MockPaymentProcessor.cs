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

            // Simulate successful payment 90% of the time
            if (_random.Next(0, 10) < 9)
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

            _logger.LogWarning("Mock payment declined");
            return Task.FromResult(PaymentResult.CreateFailed(
                "mock_decline",
                "Payment declined by mock processor"
            ));
        }

        public Task<PaymentResult> RefundPaymentAsync(string transactionId, decimal amount)
        {
            _logger.LogInformation("Mock refund for transaction: {TransactionId}, amount: {Amount}",
                transactionId, amount);

            return Task.FromResult(PaymentResult.CreateSuccess(
                transactionId: $"REFUND_{Guid.NewGuid()}"
            ));
        }
    }
}