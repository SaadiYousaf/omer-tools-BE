using FluentEmail.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entities;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Net.Mail;
using System.Threading.Tasks;

namespace ProductService.Business.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly ISendGridClient _sendGridClient;
        private readonly IConfiguration _configuration;
        private readonly Interfaces.ITemplateRenderer _templateRenderer;
        private readonly ILogger<SendGridEmailService> _logger;

        public SendGridEmailService(
            ISendGridClient sendGridClient,
            IConfiguration configuration,
            Interfaces.ITemplateRenderer templateRenderer,
            ILogger<SendGridEmailService> logger)
        {
            _sendGridClient = sendGridClient;
            _configuration = configuration;
            _templateRenderer = templateRenderer;
            _logger = logger;
        }

        public async Task SendOrderConfirmationAsync(Order order, string email)
        {
            try
            {
                var templateModel = new OrderConfirmationTemplateModel
                {
                    Order = order,
                    ContactEmail = _configuration["EmailSettings:ContactEmail"],
                    SupportPhone = _configuration["EmailSettings:SupportPhone"],
                    OrderDate = order.CreatedAt.ToString("f")
                };

                var htmlContent = await _templateRenderer.RenderTemplateAsync("OrderConfirmation", templateModel);
                var plainTextContent = $"Thank you for your order #{order.OrderNumber}";

                var from = new EmailAddress(
                    _configuration["EmailSettings:FromAddress"],
                    _configuration["EmailSettings:FromName"]);
                var to = new EmailAddress(email);
                var subject = $"Order Confirmation - #{order.OrderNumber}";
                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);

                var response = await _sendGridClient.SendEmailAsync(msg);

                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Body.ReadAsStringAsync();
                    _logger.LogError("Failed to send order confirmation email. Status: {Status}, Body: {Body}",
                        response.StatusCode, body);
                }
                else
                {
                    _logger.LogInformation("Order confirmation email sent for order #{OrderNumber}", order.OrderNumber);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order confirmation email");
            }
        }

        public async Task SendPaymentFailedNotificationAsync(Order order, string email, string error)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["EmailSettings:FromAddress"],
                    _configuration["EmailSettings:FromName"]);
                var to = new EmailAddress(email);
                var subject = $"Payment Failed for Order #{order.OrderNumber}";
                var plainTextContent = $"We were unable to process your payment for order #{order.OrderNumber}. Error: {error}";
                var htmlContent = $"<p>We were unable to process your payment for order <strong>#{order.OrderNumber}</strong>.</p>" +
                                  $"<p>Error: {error}</p>" +
                                  "<p>Please update your payment information to complete your order.</p>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                await _sendGridClient.SendEmailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending payment failure email");
            }
        }

        public async Task SendOrderShippedNotificationAsync(Order order, string trackingNumber, string email)
        {
            try
            {
                var from = new EmailAddress(
                    _configuration["EmailSettings:FromAddress"],
                    _configuration["EmailSettings:FromName"]);
                var to = new EmailAddress(email);
                var subject = $"Your Order #{order.OrderNumber} Has Shipped!";
                var plainTextContent = $"Your order #{order.OrderNumber} has shipped. Tracking number: {trackingNumber}";
                var htmlContent = $"<p>Your order <strong>#{order.OrderNumber}</strong> has shipped!</p>" +
                                  $"<p>Tracking number: {trackingNumber}</p>";

                var msg = MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
                await _sendGridClient.SendEmailAsync(msg);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending order shipped notification");
            }
        }
    }
}