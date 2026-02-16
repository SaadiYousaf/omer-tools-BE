using MailKit.Net.Smtp;     
using MailKit.Security;      
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;              
using ProductService.Business.Helper;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites.EmailsModel;
using ProductService.Domain.Entities;
using System.Globalization;
using System.Text;

namespace ProductService.Business.Services
{
	public class MailkitEmailService : IMailkitEmailService
	{
		private readonly EmailSettings _settings;
		private readonly ILogger<MailkitEmailService> _logger;

		public MailkitEmailService(
			IOptions<EmailSettings> emailSettings,
			ILogger<MailkitEmailService> logger)
		{
			_settings = emailSettings.Value;
			_logger = logger;
		}

		public async Task<bool> SendClaimStatusUpdateEmailAsync(
	string toEmail,
	WarrantyClaim claim,
	string oldStatus,
	string newStatus)
		{
			try
			{
				_logger.LogInformation("Sending claim status update email to {Email} for claim #{ClaimNumber}",
					toEmail, claim.ClaimNumber);

				// Skip if status didn't actually change
				if (oldStatus == newStatus)
				{
					_logger.LogInformation("Status unchanged for claim #{ClaimNumber}, skipping email",
						claim.ClaimNumber);
					return true;
				}

				// Load template
				var template = await TemplateHelper.LoadTemplate("ClaimStatusUpdate");

				if (string.IsNullOrEmpty(template))
				{
					_logger.LogError("ClaimStatusUpdate template not found");
					template = await GetDefaultStatusUpdateTemplate();
				}

				// Build placeholders
				var placeholders = BuildStatusUpdatePlaceholders(claim, oldStatus, newStatus);

				// Add template content to placeholders
				placeholders["Content"] = template;

				// Create message
				var message = new EmailMessage
				{
					ToEmail = toEmail,
					Subject = $"[{claim.ClaimNumber}] Warranty Claim Status Updated to {FormatStatusForDisplay(newStatus)}",
					Body = template,
					Placeholders = placeholders
				};

				return await SendEmailAsync(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send claim status update email to {Email}", toEmail);
				return false;
			}
		}

		private Dictionary<string, string> BuildStatusUpdatePlaceholders(
			WarrantyClaim claim, string oldStatus, string newStatus)
		{
			// Get status colors
			var statusColors = GetStatusColors(newStatus);

			var placeholders = new Dictionary<string, string>
			{
				// Customer and Claim Info
				["CustomerName"] = claim.FullName,
				["ClaimNumber"] = claim.ClaimNumber,
				["OldStatus"] = FormatStatusForDisplay(oldStatus),
				["NewStatus"] = FormatStatusForDisplay(newStatus),
				["ClaimType"] = FormatClaimType(claim.ClaimType),
				["SubmissionDate"] = claim.SubmittedAt.ToString("dddd, MMMM dd, yyyy"),
				["UpdatedDate"] = DateTime.UtcNow.ToString("dddd, MMMM dd, yyyy 'at' hh:mm tt"),

				// Status Colors for Template
				["StatusColor"] = statusColors.Background,
				["StatusBorderColor"] = statusColors.Border,

				// Store Information (from EmailSettings)
				["StoreName"] = _settings.StoreName,
				["StorePhone"] = " (02) 9759 8833",
				["StoreEmail"] = _settings.SupportEmail ?? "info@omertools.com.au",
				["StoreAddress"] = "1126 CANTERBURY RD, ROSELANDS NSW 2196, Australia",
				["WebsiteUrl"] = _settings.WebsiteUrl ?? "https://omertools.com.au",
				["SupportUrl"] = _settings.SupportUrl ?? "https://omertools.com.au/contact",
				["CurrentYear"] = DateTime.UtcNow.Year.ToString(),
				["Subject"] = $"[{claim.ClaimNumber}] Warranty Claim Status Updated to {FormatStatusForDisplay(newStatus)}",
				["StatusNotes"] = claim.StatusNotes ?? string.Empty
			};

			// Common Fault Description - only add if not empty
			if (!string.IsNullOrWhiteSpace(claim.CommonFaultDescription))
			{
				placeholders["CommonFaultDescription"] = claim.CommonFaultDescription;
			}

			// Product Information
			if (claim.ProductClaims != null && claim.ProductClaims.Any())
			{
				// Remove single product placeholders - they conflict with array processing
				// placeholders["ModelNumber"] = firstProduct.ModelNumber;
				// placeholders["SerialNumber"] = firstProduct.SerialNumber ?? string.Empty;
				// placeholders["FaultDescription"] = firstProduct.FaultDescription ?? string.Empty;

				// Add array placeholders for multiple products
				for (int i = 0; i < claim.ProductClaims.Count; i++)
				{
					var product = claim.ProductClaims.ElementAt(i);
					placeholders[$"Products[{i}].ModelNumber"] = product.ModelNumber;
					placeholders[$"Products[{i}].SerialNumber"] = product.SerialNumber ?? string.Empty;
					placeholders[$"Products[{i}].FaultDescription"] = product.FaultDescription ?? string.Empty;
					placeholders[$"Products[{i}].IncrementIndex"] = (i + 1).ToString();
				}
			}
			else
			{
				// For old single-product claims, add as array item [0]
				placeholders["Products[0].ModelNumber"] = claim.ModelNumber;
				placeholders["Products[0].SerialNumber"] = claim.SerialNumber ?? string.Empty;
				placeholders["Products[0].FaultDescription"] = claim.FaultDescription ?? string.Empty;
				placeholders["Products[0].IncrementIndex"] = "1";
			}

			// Add next steps based on new status
			placeholders["NextSteps"] = GetNextStepsForStatus(newStatus);

			return placeholders;
		}

		private (string Background, string Border) GetStatusColors(string status)
		{
			return status?.ToLower() switch
			{
				"submitted" => ("#3498db", "#2980b9"),        // Blue
				"picked_up" => ("#27ae60", "#229954"),       // Green
				"sent" => ("#27ae60", "#229954"),           // Green
				"approved" => ("#2ecc71", "#27ae60"),       // Light Green
				"rejected" => ("#e74c3c", "#c0392b"),       // Red
				"completed" => ("#9b59b6", "#8e44ad"),      // Purple
				"under_review" => ("#f1c40f", "#f39c12"),   // Yellow
				_ => ("#95a5a6", "#7f8c8d")                 // Gray (default)
			};
		}

		private string FormatStatusForDisplay(string status)
		{
			if (string.IsNullOrEmpty(status))
				return "Unknown";

			var lowerStatus = status.ToLower();

			return lowerStatus switch
			{
				"submitted" => "Submitted",
				"picked_up" => "Picked Up",
				"sent" => "Sent",
				"approved" => "Approved",
				"rejected" => "Rejected",
				"completed" => "Completed",
				"under_review" => "Under Review",
				_ => ToTitleCaseManual(status.Replace("_", " "))
			};
		}

		private string FormatClaimType(string claimType)
		{
			if (string.IsNullOrEmpty(claimType))
				return "Warranty Claim";

			return claimType switch
			{
				"warranty-inspection" => "Warranty Inspection",
				"service-repair" => "Service Repair",
				"firstup-failure" => "Firstup Failure",
				_ => ToTitleCaseManual(claimType.Replace("-", " "))
			};
		}

		private string GetNextStepsForStatus(string status)
		{
			return status?.ToLower() switch
			{
				"submitted" =>
					"Your claim has been successfully submitted and is now in our system. " +
					"Our warranty team will review it within 1-2 business days. " +
					"You'll receive another update once it's been reviewed.",

				"picked_up" =>
					"Your product has been picked",
					

				"sent" =>
					"Your product/service has been processed and sent. " +
					"You should receive it within 5-7 business days. " +
					"Tracking information will be provided if available. " +
					"Please contact us if you don't receive it within 10 business days.",

				"rejected" =>
					"We regret to inform you that your warranty claim has been rejected. " +
					"This decision was made because: [Reason will be in Status Notes]. " +
					"If you believe this is an error or would like to appeal, please contact our support team " +
					"within 14 days with any additional information.",

				"completed" =>
					"Your warranty claim has been successfully completed. " +
					"All necessary actions have been taken, and your case is now closed. " +
					"Thank you for choosing us. We hope to serve you again in the future.",

				_ =>
					"Our team will contact you if any further information is needed. " +
					"You can also check your claim status anytime through our customer portal."
			};
		}

		private string ToTitleCaseManual(string input)
		{
			if (string.IsNullOrEmpty(input))
				return input;

			var textInfo = CultureInfo.CurrentCulture.TextInfo;
			return textInfo.ToTitleCase(input.ToLower());
		}

		private async Task<string> GetDefaultStatusUpdateTemplate()
		{
			return @"
<!DOCTYPE html>
<html>
<head>
    <style>
        body { font-family: Arial, sans-serif; }
        .status-box { padding: 20px; border-radius: 5px; }
    </style>
</head>
<body>
    <h2>Claim Status Update</h2>
    <p>Dear {{CustomerName}},</p>
    <p>Your warranty claim status has been updated:</p>
    <div class='status-box' style='background-color: {{StatusColor}}; border: 2px solid {{StatusBorderColor}};'>
        <strong>Claim Number:</strong> {{ClaimNumber}}<br>
        <strong>Old Status:</strong> {{OldStatus}}<br>
        <strong>New Status:</strong> {{NewStatus}}<br>
        <strong>Updated On:</strong> {{UpdatedDate}}
    </div>
    {{#if StatusNotes}}<p><strong>Notes:</strong> {{StatusNotes}}</p>{{/if}}
    <p>{{NextSteps}}</p>
    <p>Best regards,<br>The {{StoreName}} Team</p>
</body>
</html>";
		}

		public async Task<bool> SendEmailAsync(EmailMessage message)
		{
			try
			{
				_logger.LogInformation("Preparing to send email to {Email}", message.ToEmail);

				//var baseLayout = string.Empty;
				//// Load base layout
				//if (string.IsNullOrEmpty(message.Body))
				//{
				//	baseLayout = await TemplateHelper.LoadTemplate("BaseLayout");
				//}
				//else
				//{
				//	baseLayout = await TemplateHelper.LoadTemplate(message.Body);
				//}

				//// Extract subject from template if not provided
				//var subject = message.Subject;
				//if (string.IsNullOrEmpty(subject) && message.Placeholders.TryGetValue("Subject", out var templateSubject))
				//{
				//	subject = templateSubject;
				//}

				// Add common placeholders
				//var allPlaceholders = new Dictionary<string, string>(message.Placeholders)
				//{
				//	["Subject"] = subject,
				//	["StoreName"] = _settings.StoreName,
				//	["WebsiteUrl"] = _settings.WebsiteUrl,
				//	["SupportUrl"] = _settings.SupportUrl,
				//	["CurrentYear"] = DateTime.Now.Year.ToString()
				//};

				// Replace placeholders in content
				var content = TemplateHelper.ReplaceComplexPlaceholders(message.Body, message.Placeholders);

				// Replace placeholders in base layout
				//allPlaceholders["Content"] = content;
				//var fullHtml = TemplateHelper.ReplacePlaceholders(baseLayout, allPlaceholders);

				// Create MIME message
				var mimeMessage = new MimeMessage();
				mimeMessage.From.Add(new MailboxAddress(_settings.SenderName, _settings.SenderEmail));
				mimeMessage.To.Add(MailboxAddress.Parse(message.ToEmail));
				mimeMessage.Subject = message.Subject ?? "Notification from Store";
				mimeMessage.Body = new TextPart(MimeKit.Text.TextFormat.Html)
				{
					Text = content
				};

				// Send via SMTP
				await SendViaSmtpAsync(mimeMessage);

				_logger.LogInformation("Email sent successfully to {Email}", message.ToEmail);
				return true;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send email to {Email}", message.ToEmail);
				return false;
			}
		}

		public async Task<bool> SendWelcomeEmailAsync(string toEmail, string customerName)
		{
			try
			{
				_logger.LogInformation("Sending welcome email to {Email}", toEmail);

				// Load welcome template
				var template = await TemplateHelper.LoadTemplate("WelcomeEmail");

				// Build placeholders
				var placeholders = TemplateHelper.BuildWelcomePlaceholders(
					customerName,
					toEmail,
					_settings);

				// Create message
				var message = new EmailMessage
				{
					ToEmail = toEmail,
					Placeholders = placeholders
				};

				return await SendEmailAsync(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send welcome email to {Email}", toEmail);
				return false;
			}
		}

		public async Task<bool> SendOrderConfirmationAsync(OrderTestModel order)
		{
			try
			{
				_logger.LogInformation("Sending order confirmation for order #{OrderNumber} to {Email}",
					order.OrderNumber, order.CustomerEmail);

				// Load order template
				var template = await TemplateHelper.LoadTemplate("OrderConfirmation");

				// Build placeholders
				var placeholders = TemplateHelper.BuildOrderPlaceholders(order, _settings);

				// Add template content to placeholders
				placeholders["Content"] = template;

				// Create message
				var message = new EmailMessage
				{
					ToEmail = order.CustomerEmail,
					Placeholders = placeholders
				};

				return await SendEmailAsync(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send order confirmation to {Email}", order.CustomerEmail);
				return false;
			}
		}

		public async Task<bool> SendClaimNotificationAsync(ClaimTestModel claim)
		{
			try
			{
				_logger.LogInformation("Sending claim notification for claim #{ClaimId} to {Email}",
					claim.ClaimId, claim.CustomerEmail);

				// Load claim template
				var template = await TemplateHelper.LoadTemplate("ClaimsNotifications");

				// Build placeholders
				var placeholders = TemplateHelper.BuildClaimPlaceholders(claim, _settings);

				// Add template content to placeholders
				placeholders["Content"] = template;

				// Create message
				var message = new EmailMessage
				{
					ToEmail = claim.CustomerEmail,
					Placeholders = placeholders
				};

				return await SendEmailAsync(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send claim notification to {Email}", claim.CustomerEmail);
				return false;
			}
		}

		public async Task<bool> SendTestEmailAsync(string toEmail)
		{
			try
			{
				_logger.LogInformation("Sending test email to {Email}", toEmail);

				var testHtml = @"
<h2>Test Email from Your Store</h2>
<p>If you're receiving this email, your email configuration is working correctly!</p>
<div class='highlight-box'>
    <p><strong>Test Details:</strong></p>
    <p>Time: {{CurrentTime}}</p>
    <p>Status: <span style='color: green;'>✓ Working</span></p>
</div>
<p>You can now send order confirmations and claim notifications from your store.</p>
";

				var message = new EmailMessage
				{
					ToEmail = toEmail,
					Subject = "Email Test - Store Backend",
					Body = testHtml,
					Placeholders = new Dictionary<string, string>
					{
						["CurrentTime"] = DateTime.Now.ToString("F"),
						["StoreName"] = _settings.StoreName
					}
				};

				return await SendEmailAsync(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send test email to {Email}", toEmail);
				return false;
			}
		}

		private async Task SendViaSmtpAsync(MimeMessage mimeMessage)
		{
			using var smtp = new MailKit.Net.Smtp.SmtpClient();

			try
			{
				// Connect to SMTP server
				_logger.LogDebug("Connecting to SMTP server {Server}:{Port}",
					_settings.SmtpServer, _settings.Port);

				await smtp.ConnectAsync(
					_settings.SmtpServer,
					_settings.Port,
					_settings.EnableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None);

				// Authenticate if credentials provided
				if (!string.IsNullOrEmpty(_settings.Username) &&
					!string.IsNullOrEmpty(_settings.Password))
				{
					_logger.LogDebug("Authenticating with SMTP server");
					await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
				}

				// Send email
				_logger.LogDebug("Sending email message");
				await smtp.SendAsync(mimeMessage);
			}
			finally
			{
				// Disconnect
				if (smtp.IsConnected)
				{
					await smtp.DisconnectAsync(true);
				}
			}
		}

		// Helper method for quick testing
		public async Task SendQuickTestEmail(string toEmail, string testType = "basic")
		{
			switch (testType.ToLower())
			{
				case "welcome":
					await SendWelcomeEmailAsync(toEmail, "Test Customer");
					break;

				case "order":
					var order = new OrderTestModel
					{
						CustomerName = "Test Customer",
						CustomerEmail = toEmail,
						OrderNumber = $"TEST-{DateTime.Now:yyyyMMdd-HHmmss}",
						TotalAmount = 149.99m,
						ShippingAddress = "123 Test Street, Test City, TC 12345",
						Items = new List<OrderItemTest>
						{
							new() { Name = "Test Product 1", Quantity = 2, Price = 49.99m },
							new() { Name = "Test Product 2", Quantity = 1, Price = 29.99m }
						}
					};
					await SendOrderConfirmationAsync(order);
					break;

				case "claim":
					var claim = new ClaimTestModel
					{
						CustomerName = "Test Customer",
						CustomerEmail = toEmail,
						ClaimId = $"TEST-CLM-{DateTime.Now:yyyyMMdd}",
						ClaimType = "Return Request",
						OrderNumber = "TEST-001",
						ClaimDescription = "Testing claim notification system",
						NextSteps = "This is a test claim. No action required."
					};
					await SendClaimNotificationAsync(claim);
					break;

				default:
					await SendTestEmailAsync(toEmail);
					break;
			}
		}
		public async Task<bool> SendWarrantyClaimConfirmationAsync(string toEmail, WarrantyClaim claim)
		{
			try
			{
				_logger.LogInformation("Sending warranty claim confirmation for claim #{ClaimNumber} to {Email}",
					claim.ClaimNumber, toEmail);

				// Load claim template - using the "ClaimsNotifications" template
				var template = await TemplateHelper.LoadTemplate("ClaimsNotifications");

				if (string.IsNullOrEmpty(template))
				{
					_logger.LogError("ClaimsNotifications template not found");
					template = await GetDefaultClaimConfirmationTemplate();
				}

				// Build placeholders matching your template exactly
				var placeholders = new Dictionary<string, string>
				{
					// Subject - matches {{Subject}}
					["Subject"] = $"Warranty Claim Submitted: {claim.ClaimNumber}",

					// Store Information - matches {{StoreName}}
					["StoreName"] = _settings.StoreName,

					// Header - matches {{HeaderText}}
					["HeaderText"] = "Claim Submission Confirmation",

					// Customer - matches {{CustomerName}}
					["CustomerName"] = claim.FullName,

					// Claim Details - matches {{ClaimId}}, {{ClaimDate}}, {{ClaimStatus}}, {{ClaimType}}
					["ClaimId"] = claim.ClaimNumber,
					["ClaimDate"] = claim.SubmittedAt.ToString("MMMM dd, yyyy"),
					["ClaimStatus"] = FormatStatusForDisplay(claim.Status ?? "submitted"),
					["ClaimType"] = FormatClaimType(claim.ClaimType),

					// Product/Order - matches {{OrderNumber}} (conditional in template)
					["OrderNumber"] = GetProductInfo(claim),

					// Description - matches {{ClaimDescription}}
					["ClaimDescription"] = GetClaimDescription(claim),

					// Next Steps - matches {{NextSteps}}
					["NextSteps"] = GetNextStepsForClaimType(claim.ClaimType),

					// Processing Time - matches {{ProcessingTime}}
					["ProcessingTime"] = GetProcessingTime(claim.ClaimType),

					// Additional common placeholders that might be in base layout
					["WebsiteUrl"] = _settings.WebsiteUrl ?? "https://omertools.com.au",
					["SupportUrl"] = _settings.SupportUrl ?? "https://omertools.com.au/contact",
					["CurrentYear"] = DateTime.UtcNow.Year.ToString(),
					["StoreEmail"] = _settings.SupportEmail ?? "info@omertools.com.au",
					["StorePhone"] =  "(02) 9759 8833"
				};

				// Add OrderNumber only if it exists (for the {{#if OrderNumber}} conditional)
				// Your template already handles the conditional with {{#if OrderNumber}}

				// Create message
				var message = new EmailMessage
				{
					ToEmail = toEmail,
					Subject = placeholders["Subject"],
					Body = template,
					Placeholders = placeholders
				};

				return await SendEmailAsync(message);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Failed to send warranty claim confirmation to {Email}", toEmail);
				return false;
			}
		}

		// Helper method to get product info for OrderNumber placeholder
		private string GetProductInfo(WarrantyClaim claim)
		{
			if (claim.ProductClaims != null && claim.ProductClaims.Any())
			{
				var products = claim.ProductClaims.Select(p => p.ModelNumber).Distinct();
				return string.Join(", ", products);
			}

			return claim.ModelNumber ?? "N/A";
		}

		// Helper method to get claim description
		private string GetClaimDescription(WarrantyClaim claim)
		{
			if (!string.IsNullOrEmpty(claim.CommonFaultDescription))
				return claim.CommonFaultDescription;

			if (!string.IsNullOrEmpty(claim.FaultDescription))
				return claim.FaultDescription;

			if (claim.ProductClaims != null && claim.ProductClaims.Any())
			{
				var descriptions = claim.ProductClaims
					.Where(p => !string.IsNullOrEmpty(p.FaultDescription))
					.Select(p => p.FaultDescription)
					.Distinct();

				if (descriptions.Any())
					return string.Join("; ", descriptions);
			}

			return "Warranty claim submitted";
		}

		// Helper method to get processing time based on claim type
		private string GetProcessingTime(string claimType)
		{
			return claimType?.ToLower() switch
			{
				"warranty-inspection" => "7-14",
				"service-repair" => "7-14",
				"firstup-failure" => "1-7",
				_ => "7-14"
			};
		}

		private string GetNextStepsForClaimType(string claimType)
		{
			return claimType?.ToLower() switch
			{
				
				"warranty-inspection" => "Our warranty team will review your claim within 7-14 business days. " +
									   "You'll receive an update via email once the review is complete.",
				"service-repair" => "Our service team will assess your product within 7-14 business days. " +
								  "We'll contact you with a service estimate and timeline.",
				"firstup-failure" => "Our technical team will investigate the firstup failure immediately. " +
								   "You'll receive updates within 24-48 hours.",
				_ => "Our warranty team will review your claim within 7-14 business days. " +
					"You'll receive an update via email once the review is complete."
			};
		}




		private async Task<string> GetDefaultClaimConfirmationTemplate()
		{
			return @"
{{Subject}} = Warranty Claim Submitted: {{ClaimId}}
{{StoreName}} = {{StoreName}}
{{HeaderText}} = Claim Submission Confirmation

<h2>Dear {{CustomerName}},</h2>
<p>Thank you for submitting your warranty claim. Here are the details:</p>

<div class='highlight-box'>
    <p><strong>Claim Details</strong></p>
    <p><strong>Claim ID:</strong> {{ClaimId}}</p>
    <p><strong>Date Submitted:</strong> {{ClaimDate}}</p>
    <p><strong>Status:</strong> <span style='color: #667eea; font-weight: bold;'>{{ClaimStatus}}</span></p>
    <p><strong>Type:</strong> {{ClaimType}}</p>
    {{#if OrderNumber}}
    <p><strong>Product:</strong> {{OrderNumber}}</p>
    {{/if}}
</div>

<div class='highlight-box'>
    <p><strong>Description:</strong></p>
    <p>{{ClaimDescription}}</p>
</div>

<div class='highlight-box' style='background: #e7f4e4; border-left-color: #28a745;'>
    <p><strong>📋 Next Steps:</strong></p>
    <p>{{NextSteps}}</p>
    <p><em>⏱️ Estimated processing time: {{ProcessingTime}} business days</em></p>
</div>

<p>You can track your claim status anytime by contacting our support team.</p>

<p>Thank you,<br><strong>Warranty Support Team</strong><br>{{StoreName}}</p>";
		}
	}
}
