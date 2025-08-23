using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entities;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class EmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<EmailService> _logger;

        public EmailService(
            ISendGridClient sendGridClient,
            IConfiguration configuration,
            ILogger<EmailService> logger)
        {
            _sendGridClient = sendGridClient;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(Order order, string email)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["EmailSettings:FromAddress"] ?? "noreply@example.com",
                    _configuration["EmailSettings:FromName"] ?? "Your Store");

                var to = new EmailAddress(email);
                var subject = $"Order Confirmation - #{order.OrderNumber}";

                var plainTextContent = GenerateOrderConfirmationPlainText(order);
                var htmlContent = GenerateOrderConfirmationHtml(order);

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                var response = await _sendGridClient.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send order confirmation email. Status: {Status}, Body: {Body}",
                        response.StatusCode, body);
                    throw new Exception($"Email sending failed with status {response.StatusCode}");
                }

                _logger.LogInformation("Order confirmation email sent for order #{OrderNumber}", order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email");
                throw;
            }
        }

        private string GenerateOrderConfirmationPlainText(Order order)
        {
            return $@"
Thank you for your order!

ORDER SUMMARY
-------------
Order Number: {order.OrderNumber}
Order Date: {order.CreatedAt.ToString("f")}

ITEMS:
{string.Join("\n", order.Items.Select(item => $"- {item.ProductName} (x{item.Quantity}) - ${item.UnitPrice.ToString("N2")}"))}

Subtotal: ${order.TotalAmount.ToString("N2")}
Total: ${order.TotalAmount.ToString("N2")}

SHIPPING INFORMATION:
{order.ShippingAddress?.FullName}
{order.ShippingAddress?.AddressLine1}
{order.ShippingAddress?.City}, {order.ShippingAddress?.State} {order.ShippingAddress?.PostalCode}
{order.ShippingAddress?.Country}

If you have any questions, please contact our support team at {_configuration["EmailSettings:SupportEmail"] ?? "support@example.com"}.

Thank you for shopping with us!
";
        }

        private string GenerateOrderConfirmationHtml(Order order)
        {
            var itemsHtml = string.Join("", order.Items.Select(item =>
                $@"
                <tr>
                    <td style='padding: 8px; border-bottom: 1px solid #eee;'>{item.ProductName}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #eee; text-align: center;'>{item.Quantity}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #eee; text-align: right;'>${item.UnitPrice.ToString("N2")}</td>
                    <td style='padding: 8px; border-bottom: 1px solid #eee; text-align: right;'>${(item.Quantity * item.UnitPrice).ToString("N2")}</td>
                </tr>
                "));

            return $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Order Confirmation</title>
    <style>
        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; margin: 0; padding: 20px; }}
        .container {{ max-width: 600px; margin: 0 auto; background: #f9f9f9; padding: 20px; border-radius: 5px; }}
        .header {{ background-color: #4CAF50; color: white; padding: 20px; text-align: center; border-radius: 5px 5px 0 0; }}
        .content {{ background: white; padding: 20px; }}
        table {{ width: 100%; border-collapse: collapse; margin: 20px 0; }}
        th {{ background-color: #f2f2f2; padding: 12px; text-align: left; }}
        .total-row {{ font-weight: bold; font-size: 1.1em; }}
        .footer {{ margin-top: 30px; padding-top: 20px; border-top: 1px solid #eee; font-size: 0.9em; color: #666; }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>Order Confirmation</h1>
            <p>Thank you for your purchase!</p>
        </div>
        
        <div class='content'>
            <h2>Order #{order.OrderNumber}</h2>
            <p><strong>Order Date:</strong> {order.CreatedAt.ToString("f")}</p>
            
            <h3>Order Details</h3>
            <table>
                <tr>
                    <th style='padding: 12px; text-align: left;'>Product</th>
                    <th style='padding: 12px; text-align: center;'>Quantity</th>
                    <th style='padding: 12px; text-align: right;'>Price</th>
                    <th style='padding: 12px; text-align: right;'>Total</th>
                </tr>
                {itemsHtml}
                <tr class='total-row'>
                    <td colspan='3' style='padding: 12px; text-align: right;'>Total:</td>
                    <td style='padding: 12px; text-align: right;'>${order.TotalAmount.ToString("N2")}</td>
                </tr>
            </table>
            
            <h3>Shipping Information</h3>
            <p>
                {order.ShippingAddress?.FullName}<br>
                {order.ShippingAddress?.AddressLine1}<br>
                {order.ShippingAddress?.City}, {order.ShippingAddress?.State} {order.ShippingAddress?.PostalCode}<br>
                {order.ShippingAddress?.Country}
            </p>
        </div>
        
        <div class='footer'>
            <p>If you have any questions, please contact our support team at 
            <a href='mailto:{_configuration["EmailSettings:SupportEmail"] ?? "support@example.com"}'>
                {_configuration["EmailSettings:SupportEmail"] ?? "support@example.com"}
            </a>.</p>
            <p>Thank you for shopping with us!</p>
        </div>
    </div>
</body>
</html>
";
        }

        public async Task SendPaymentFailedNotificationAsync(Order order, string email, string error)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["EmailSettings:FromAddress"] ?? "noreply@example.com",
                    _configuration["EmailSettings:FromName"] ?? "Your Store");

                var to = new EmailAddress(email);
                var subject = $"Payment Failed for Order #{order.OrderNumber}";

                var plainTextContent = $"We were unable to process your payment for order #{order.OrderNumber}. Error: {error}";
                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Payment Failed</title>
</head>
<body>
    <h2>Payment Failed</h2>
    <p>We were unable to process your payment for order <strong>#{order.OrderNumber}</strong>.</p>
    <p>Error: {error}</p>
    <p>Please update your payment information to complete your order.</p>
    <p>If you need assistance, please contact our support team.</p>
</body>
</html>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                await _sendGridClient.SendEmailAsync(msg);

                _logger.LogInformation("Payment failure email sent for order #{OrderNumber}", order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment failure email");
                throw;
            }
        }

        public async Task SendOrderShippedNotificationAsync(Order order, string trackingNumber, string email)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["EmailSettings:FromAddress"] ?? "noreply@example.com",
                    _configuration["EmailSettings:FromName"] ?? "Your Store");

                var to = new EmailAddress(email);
                var subject = $"Your Order #{order.OrderNumber} Has Shipped!";

                var plainTextContent = $"Your order #{order.OrderNumber} has shipped. Tracking number: {trackingNumber}";
                var htmlContent = $@"
<!DOCTYPE html>
<html>
<head>
    <meta charset='utf-8'>
    <title>Order Shipped</title>
</head>
<body>
    <h2>Your Order Has Shipped!</h2>
    <p>Your order <strong>#{order.OrderNumber}</strong> has shipped and is on its way to you.</p>
    <p>Tracking number: <strong>{trackingNumber}</strong></p>
    <p>You can track your package using the tracking number above.</p>
    <p>Thank you for shopping with us!</p>
</body>
</html>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                await _sendGridClient.SendEmailAsync(msg);

                _logger.LogInformation("Order shipped notification sent for order #{OrderNumber}", order.OrderNumber);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order shipped notification");
                throw;
            }
        }
    }
}