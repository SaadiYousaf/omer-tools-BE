using ProductService.Domain.Entites.EmailsModel;
using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.Interfaces
{
	public interface IMailkitEmailService
	{
		Task<bool> SendEmailAsync(EmailMessage message);
		Task<bool> SendWelcomeEmailAsync(string toEmail, string customerName);
		Task<bool> SendOrderConfirmationAsync(OrderTestModel order);
		Task<bool> SendClaimNotificationAsync(ClaimTestModel claim);
		Task<bool> SendTestEmailAsync(string toEmail);

		Task SendQuickTestEmail(string toEmail, string testType = "basic");

		Task<bool> SendWarrantyClaimConfirmationAsync(string toEmail, WarrantyClaim claim);
		Task<bool> SendClaimStatusUpdateEmailAsync(
string toEmail,
WarrantyClaim claim,
string oldStatus,
string newStatus);
	
}

	public class EmailResult
	{
		public bool Success { get; set; }
		public string Message { get; set; } = string.Empty;
		public string ErrorDetails { get; set; } = string.Empty;

		public static EmailResult SuccessResult(string message = "Email sent successfully")
			=> new() { Success = true, Message = message };

		public static EmailResult ErrorResult(string message, string errorDetails = "")
			=> new() { Success = false, Message = message, ErrorDetails = errorDetails };
	}
}
