using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.Interfaces;
using ProductService.Domain.Entites.EmailsModel;
using System.ComponentModel.DataAnnotations;

namespace ProductService.API.Controller
{
	/// <summary>
	/// Email Controller for sending various types of emails
	/// </summary>
	[ApiController]
	[Route("api/[controller]")]
	[Produces("application/json")]
	public class EmailController : ControllerBase
	{
		private readonly IMailkitEmailService _emailService;
		private readonly ILogger<EmailController> _logger;

		public EmailController(
			IMailkitEmailService emailService,
			ILogger<EmailController> logger)
		{
			_emailService = emailService;
			_logger = logger;
		}

		/// <summary>
		/// Send a simple test email
		/// </summary>
		/// <param name="request">Test email request</param>
		/// <returns>Result of the email send operation</returns>
		[HttpPost("send-test")]
		[ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(object), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(object), StatusCodes.Status500InternalServerError)]
		public async Task<IActionResult> SendTestEmail([FromBody] TestEmailRequest request)
		{
			try
			{
				// Validate model
				if (!ModelState.IsValid)
				{
					return BadRequest(new
					{
						success = false,
						message = "Invalid request",
						errors = ModelState.Values
							.SelectMany(v => v.Errors)
							.Select(e => e.ErrorMessage)
					});
				}

				_logger.LogInformation("Received test email request for {Email}", request.Email);

				var result = await _emailService.SendTestEmailAsync(request.Email);

				if (result)
				{
					return Ok(new
					{
						success = true,
						message = $"Test email sent to {request.Email}",
						timestamp = DateTime.UtcNow
					});
				}

				return BadRequest(new
				{
					success = false,
					message = $"Failed to send test email to {request.Email}"
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending test email to {Email}", request.Email);
				return StatusCode(500, new
				{
					success = false,
					message = "Internal server error",
					error = ex.Message,
					requestId = HttpContext.TraceIdentifier
				});
			}
		}

		/// <summary>
		/// Send a welcome email to new customers
		/// </summary>
		[HttpPost("send-welcome")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(StatusCodes.Status400BadRequest)]
		public async Task<IActionResult> SendWelcomeEmail([FromBody] WelcomeEmailRequest request)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					return BadRequest(new
					{
						success = false,
						errors = ModelState.Values
							.SelectMany(v => v.Errors)
							.Select(e => e.ErrorMessage)
					});
				}

				_logger.LogInformation("Sending welcome email to {Email} for {Name}",
					request.Email, request.Name);

				var result = await _emailService.SendWelcomeEmailAsync(request.Email, request.Name);

				return Ok(new
				{
					success = result,
					message = result
						? $"Welcome email sent to {request.Email}"
						: $"Failed to send welcome email",
					email = request.Email,
					timestamp = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending welcome email to {Email}", request.Email);
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		/// <summary>
		/// Send an order confirmation email
		/// </summary>
		[HttpPost("send-order")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> SendOrderEmail([FromBody] TestOrderRequest request)
		{
			try
			{
				if (!ModelState.IsValid)
				{
					return BadRequest(new { errors = ModelState });
				}

				var order = new OrderTestModel
				{
					CustomerName = request.CustomerName ?? "Test Customer",
					CustomerEmail = request.Email,
					OrderNumber = request.OrderNumber ?? $"ORD-{DateTime.Now:yyyyMMdd-HHmmss}",
					OrderDate = request.OrderDate ?? DateTime.Now,
					TotalAmount = request.TotalAmount ?? 99.99m,
					ShippingAddress = request.ShippingAddress ?? "123 Test St, Test City, TC 12345",
					Items = request.Items?.Select(i => new OrderItemTest
					{
						Name = i.Name ?? "Test Product",
						Quantity = i.Quantity ?? 1,
						Price = i.Price ?? 29.99m
					}).ToList() ?? new List<OrderItemTest>
					{
						new() { Name = "Sample Product 1", Quantity = 2, Price = 49.99m },
						new() { Name = "Sample Product 2", Quantity = 1, Price = 29.99m }
					}
				};

				_logger.LogInformation("Sending order confirmation #{OrderNumber} to {Email}",
					order.OrderNumber, order.CustomerEmail);

				var result = await _emailService.SendOrderConfirmationAsync(order);

				return Ok(new
				{
					success = result,
					message = result
						? $"Order confirmation sent to {request.Email}"
						: $"Failed to send order confirmation",
					orderNumber = order.OrderNumber,
					customerEmail = order.CustomerEmail,
					totalAmount = order.TotalAmount,
					timestamp = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending order email to {Email}", request.Email);
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		/// <summary>
		/// Send a claim notification email
		/// </summary>
		[HttpPost("send-claim")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> SendClaimEmail([FromBody] TestClaimRequest request)
		{
			try
			{
				var claim = new ClaimTestModel
				{
					CustomerName = request.CustomerName ?? "Test Customer",
					CustomerEmail = request.Email,
					ClaimId = request.ClaimId ?? $"CLM-{DateTime.Now:yyyyMMdd-HHmmss}",
					ClaimType = request.ClaimType ?? "Return",
					OrderNumber = request.OrderNumber ?? "ORD-001",
					ClaimDescription = request.Description ?? "Test claim notification",
					NextSteps = request.NextSteps ?? "This is a test. No action required."
				};

				_logger.LogInformation("Sending claim notification #{ClaimId} to {Email}",
					claim.ClaimId, claim.CustomerEmail);

				var result = await _emailService.SendClaimNotificationAsync(claim);

				return Ok(new
				{
					success = result,
					message = result
						? $"Claim notification sent to {request.Email}"
						: $"Failed to send claim notification",
					claimId = claim.ClaimId,
					claimType = claim.ClaimType,
					timestamp = DateTime.UtcNow
				});
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error sending claim email to {Email}", request.Email);
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		/// <summary>
		/// Quick test endpoint for various email types
		/// </summary>
		/// <param name="email">Recipient email address</param>
		/// <param name="type">Email type: basic, welcome, order, claim</param>
		[HttpGet("quick-test/{email}")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public async Task<IActionResult> QuickTest(
			[EmailAddress] string email,
			[FromQuery] string type = "basic")
		{
			try
			{
				// Since IMailkitEmailService doesn't have SendQuickTestEmail,
				// we need to implement it here or add it to the interface

				_logger.LogInformation("Quick test requested for {Email} with type {Type}", email, type);

				switch (type.ToLower())
				{
					case "welcome":
						var welcomeResult = await _emailService.SendWelcomeEmailAsync(email, "Test Customer");
						return Ok(new
						{
							success = welcomeResult,
							message = welcomeResult
								? $"Welcome test email sent to {email}"
								: "Failed to send welcome email",
							type = "welcome"
						});

					case "order":
						var order = new OrderTestModel
						{
							CustomerName = "Test Customer",
							CustomerEmail = email,
							OrderNumber = $"TEST-ORD-{DateTime.Now:yyyyMMdd-HHmmss}",
							TotalAmount = 149.99m,
							ShippingAddress = "123 Test Street, Test City, TC 12345",
							Items = new List<OrderItemTest>
							{
								new() { Name = "Test Product 1", Quantity = 2, Price = 49.99m },
								new() { Name = "Test Product 2", Quantity = 1, Price = 29.99m }
							}
						};
						var orderResult = await _emailService.SendOrderConfirmationAsync(order);
						return Ok(new
						{
							success = orderResult,
							message = orderResult
								? $"Order test email sent to {email}"
								: "Failed to send order email",
							type = "order",
							orderNumber = order.OrderNumber
						});

					case "claim":
						var claim = new ClaimTestModel
						{
							CustomerName = "Test Customer",
							CustomerEmail = email,
							ClaimId = $"TEST-CLM-{DateTime.Now:yyyyMMdd}",
							ClaimType = "Return Request",
							OrderNumber = "TEST-001",
							ClaimDescription = "Testing claim notification system",
							NextSteps = "This is a test claim. No action required."
						};
						var claimResult = await _emailService.SendClaimNotificationAsync(claim);
						return Ok(new
						{
							success = claimResult,
							message = claimResult
								? $"Claim test email sent to {email}"
								: "Failed to send claim email",
							type = "claim",
							claimId = claim.ClaimId
						});

					default: // "basic"
						var basicResult = await _emailService.SendTestEmailAsync(email);
						return Ok(new
						{
							success = basicResult,
							message = basicResult
								? $"Basic test email sent to {email}"
								: "Failed to send test email",
							type = "basic"
						});
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error in quick test for {Email}", email);
				return StatusCode(500, new
				{
					success = false,
					message = ex.Message
				});
			}
		}

		/// <summary>
		/// Health check endpoint for email service
		/// </summary>
		[HttpGet("health")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		public IActionResult HealthCheck()
		{
			return Ok(new
			{
				status = "healthy",
				service = "EmailService",
				timestamp = DateTime.UtcNow,
				environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"
			});
		}
	}

	// Request models with validation
	public class TestEmailRequest
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email address")]
		public string Email { get; set; } = string.Empty;
	}

	public class WelcomeEmailRequest
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email address")]
		public string Email { get; set; } = string.Empty;

		[Required(ErrorMessage = "Name is required")]
		[MinLength(2, ErrorMessage = "Name must be at least 2 characters")]
		public string Name { get; set; } = "Customer";
	}

	public class TestOrderRequest
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email address")]
		public string Email { get; set; } = string.Empty;

		public string? CustomerName { get; set; }

		[StringLength(50, ErrorMessage = "Order number cannot exceed 50 characters")]
		public string? OrderNumber { get; set; }

		public DateTime? OrderDate { get; set; }

		[Range(0.01, 999999.99, ErrorMessage = "Total amount must be between 0.01 and 999999.99")]
		public decimal? TotalAmount { get; set; }

		[StringLength(200, ErrorMessage = "Shipping address cannot exceed 200 characters")]
		public string? ShippingAddress { get; set; }

		public List<TestOrderItem>? Items { get; set; }
	}

	public class TestOrderItem
	{
		[Required(ErrorMessage = "Product name is required")]
		public string? Name { get; set; }

		[Range(1, 1000, ErrorMessage = "Quantity must be between 1 and 1000")]
		public int? Quantity { get; set; }

		[Range(0.01, 99999.99, ErrorMessage = "Price must be between 0.01 and 99999.99")]
		public decimal? Price { get; set; }
	}

	public class TestClaimRequest
	{
		[Required(ErrorMessage = "Email is required")]
		[EmailAddress(ErrorMessage = "Invalid email address")]
		public string Email { get; set; } = string.Empty;

		public string? CustomerName { get; set; }

		public string? ClaimId { get; set; }

		public string? ClaimType { get; set; }

		public string? OrderNumber { get; set; }

		[StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
		public string? Description { get; set; }

		[StringLength(500, ErrorMessage = "Next steps cannot exceed 500 characters")]
		public string? NextSteps { get; set; }
	}
}