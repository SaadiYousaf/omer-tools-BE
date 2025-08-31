using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Polly;
using Polly.Retry;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entities;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class PaymentService : IPaymentService
    {
        private readonly IPaymentProcessor _paymentProcessor;
        private readonly ILogger<PaymentService> _logger;
        private readonly ProductDbContext _context;
        private readonly AsyncRetryPolicy<PaymentResult> _retryPolicy;

        public PaymentService(
            IPaymentProcessor paymentProcessor,
            ILogger<PaymentService> logger,
            ProductDbContext context)
        {
            _paymentProcessor = paymentProcessor;
            _logger = logger;
            _context = context;

            _retryPolicy = Policy<PaymentResult>
                .Handle<Exception>()
                .OrResult(r => r.Status == PaymentStatus.TransientError)
                .WaitAndRetryAsync(3, retryAttempt =>
                    TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                    onRetry: (result, timeSpan, retryCount, context) =>
                    {
                        _logger.LogWarning("Retry {RetryCount} for payment. Error: {Error}",
                            retryCount, result.Result?.ErrorMessage ?? result.Exception?.Message);
                    });
        }

        public async Task<PaymentResult> ProcessPaymentAsync(PaymentInfo paymentInfo)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var result = await _paymentProcessor.ProcessPaymentAsync(paymentInfo);

                    // Save payment record without order reference initially
                    var payment = new Payment
                    {
                        Id = Guid.NewGuid().ToString(),
                        OrderId = null, // Set to null initially
                        Amount = paymentInfo.Amount,
                        Currency = paymentInfo.Currency,
                        PaymentMethod = paymentInfo.PaymentMethod,
                        Status = result.Status.ToString(),
                        TransactionId = result.TransactionId,
                        Last4Digits = result.Last4Digits,
                        CardBrand = result.Brand,
                        ExpiryMonth = result.ExpiryMonth,
                        ExpiryYear = result.ExpiryYear,
                        CustomerEmail = paymentInfo.CustomerEmail,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _context.Payments.AddAsync(payment);
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Payment processed successfully, TransactionId: {TransactionId}", result.TransactionId);
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Payment processing failed");
                    return PaymentResult.CreateFailed("system_error", "Payment processing failed");
                }
            });
        }

        public async Task UpdatePaymentOrderIdAsync(string transactionId, string orderId)
        {
            try
            {
                // Use ExecuteUpdate to avoid tracking issues
                var rowsUpdated = await _context.Payments
                    .Where(p => p.TransactionId == transactionId)
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(p => p.OrderId, orderId)
                        .SetProperty(p => p.UpdatedAt, DateTime.UtcNow));

                if (rowsUpdated == 0)
                {
                    _logger.LogWarning("No payment record found with TransactionId: {TransactionId}", transactionId);
                }
                else
                {
                    _logger.LogInformation("Updated payment record {TransactionId} with OrderId: {OrderId}",
                        transactionId, orderId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update payment record {TransactionId} with OrderId: {OrderId}",
                    transactionId, orderId);
                throw;
            }
        }

        public async Task<PaymentResult> RefundPaymentAsync(string orderId, decimal amount)
        {
            var executionStrategy = _context.Database.CreateExecutionStrategy();

            return await executionStrategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var order = await _context.Orders
                        .FirstOrDefaultAsync(o => o.Id == orderId);

                    if (order == null)
                        return PaymentResult.CreateFailed("not_found", "Order not found");

                    var result = await _retryPolicy.ExecuteAsync(() =>
                        _paymentProcessor.RefundPaymentAsync(order.TransactionId, amount));

                    // Save refund record
                    var refund = new Refund
                    {
                        Id = Guid.NewGuid().ToString(),
                        PaymentId = order.TransactionId,
                        Amount = amount,
                        Currency = "USD",
                        Reason = "Customer refund request",
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    await _context.Refunds.AddAsync(refund);

                    // Update order status
                    order.PaymentStatus = "Refunded";

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    _logger.LogInformation("Refund processed successfully for order {OrderId}", orderId);
                    return result;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "Refund processing failed for order {OrderId}", orderId);
                    return PaymentResult.CreateFailed("system_error", "Refund processing failed");
                }
            });
        }
    }
}