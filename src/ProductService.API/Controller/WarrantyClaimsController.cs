using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using ProductService.Business.DTOs;
using ProductService.Business.Interfaces;
using ProductService.DataAccess.Data;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Superpower.Model;
using System.Reflection.Metadata;
using Document = QuestPDF.Fluent.Document;


namespace ProductService.API.Controller
{
	[EnableCors("AllowAll")]
	[ApiController]
	[Route("api/[controller]")]
	public class WarrantyClaimsController : ControllerBase
	{
		private readonly IWarrantyService _warrantyService;
		private readonly ILogger<WarrantyClaimsController> _logger;
		
		public WarrantyClaimsController(
			IWarrantyService warrantyService,
			ILogger<WarrantyClaimsController> logger
			)
		{
			_warrantyService = warrantyService ?? throw new ArgumentNullException(nameof(warrantyService));
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			
		}

		[HttpGet]
		public async Task<IActionResult> GetWarrantyClaims([FromQuery] WarrantyClaimQueryParameters parameters)
		{
			try
			{
				var result = await _warrantyService.GetWarrantyClaimsAsync(parameters);
				return Ok(result);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting warranty claims");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("{id}")]
		public async Task<IActionResult> GetWarrantyClaimById(string id)
		{
			try
			{
				var claim = await _warrantyService.GetWarrantyClaimByIdAsync(id);
				return claim == null ? NotFound() : Ok(claim);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting warranty claim with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("number/{claimNumber}")]
		public async Task<IActionResult> GetWarrantyClaimByNumber(string claimNumber)
		{
			try
			{
				var parameters = new WarrantyClaimQueryParameters
				{
					Search = claimNumber,
					Limit = 1
				};

				var result = await _warrantyService.GetWarrantyClaimsAsync(parameters);
				var claim = result.Data.FirstOrDefault();

				if (claim == null)
					return NotFound();

				return Ok(claim);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting warranty claim with number {claimNumber}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPost]
		public async Task<IActionResult> CreateWarrantyClaim([FromBody] CreateWarrantyClaimDto claimDto)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var createdClaim = await _warrantyService.CreateWarrantyClaimAsync(claimDto);
				return CreatedAtAction(
					nameof(GetWarrantyClaimById),
					new { id = createdClaim.Id },
					createdClaim
				);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error creating warranty claim");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPut("{id}/status")]
		public async Task<IActionResult> UpdateWarrantyClaimStatus(string id, [FromBody] UpdateWarrantyClaimStatusDto statusDto)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				await _warrantyService.UpdateWarrantyClaimStatusAsync(id, statusDto);
				return Ok(new { success = true, message = "Claim status updated successfully" });
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Warranty claim with ID {id} not found");
				return NotFound();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating warranty claim status with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpDelete("{id}")]
		public async Task<IActionResult> DeleteWarrantyClaim(string id)
		{
			try
			{
				await _warrantyService.DeleteWarrantyClaimAsync(id);
				return NoContent();
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Warranty claim with ID {id} not found");
				return NotFound();
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error deleting warranty claim with ID {id}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("dashboard/stats")]
		public async Task<IActionResult> GetDashboardStats()
		{
			try
			{
				var stats = await _warrantyService.GetDashboardStatsAsync();
				return Ok(stats);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting dashboard stats");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("recent")]
		public async Task<IActionResult> GetRecentClaims([FromQuery] int? count = null)
		{
			try
			{
				var recentClaims = await _warrantyService.GetRecentClaimsAsync(count ?? 10);
				return Ok(recentClaims);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error getting recent claims");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpGet("status/{status}/count")]
		public async Task<IActionResult> GetClaimCountByStatus(string status)
		{
			try
			{
				var count = await _warrantyService.GetClaimCountByStatusAsync(status);
				return Ok(new { status = status, count = count });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error getting claim count for status {status}");
				return StatusCode(500, "Internal server error");
			}
		}

		[HttpPost("{id}/upload-proof")]
		public async Task<IActionResult> UploadProofOfPurchase(string id, [FromForm] IFormFile file)
		{
			try
			{
				if (file == null || file.Length == 0)
					return BadRequest(new { success = false, message = "No file uploaded" });

				var filePath = await _warrantyService.UploadProofOfPurchaseAsync(id, file);

				return Ok(new
				{
					success = true,
					filePath = filePath,
					message = "Proof of purchase uploaded successfully"
				});
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Claim with ID {id} not found");
				return NotFound(new { success = false, message = "Claim not found" });
			}
			catch (ArgumentException ex)
			{
				_logger.LogWarning(ex, $"Invalid file for claim {id}");
				return BadRequest(new { success = false, message = ex.Message });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error uploading proof of purchase for claim {id}");
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		[HttpPost("{id}/upload-images")]
		public async Task<IActionResult> UploadFaultImages(string id, [FromForm] List<IFormFile> files)
		{
			try
			{
				if (files == null || files.Count == 0)
					return BadRequest(new { success = false, message = "No files uploaded" });

				var uploadedImages = await _warrantyService.UploadFaultImagesAsync(id, files);

				return Ok(new
				{
					success = true,
					uploadedCount = uploadedImages.Count(),
					images = uploadedImages,
					message = "Fault images uploaded successfully"
				});
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Claim with ID {id} not found");
				return NotFound(new { success = false, message = "Claim not found" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error uploading fault images for claim {id}");
				return StatusCode(500, new { success = false, message = ex.Message });
			}
		}

		[HttpGet("{id}/print")]
		public async Task<IActionResult> GeneratePrintableClaim(string id)
		{
			try
			{
				var claim = await _warrantyService.GetWarrantyClaimByIdAsync(id);
				if (claim == null)
					return NotFound();

				// Set QuestPDF license
				QuestPDF.Settings.License = LicenseType.Community;

				// Generate PDF
				var document = Document.Create(container =>
				{
					container.Page(page =>
					{
						// Page settings
						page.Size(PageSizes.A4);
						page.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
						page.PageColor(Colors.White);
						page.DefaultTextStyle(x => x.FontSize(11));

						// Header with Logo, Address, and Phone
						page.Header()
							.Column(headerColumn =>
							{
								// Logo Image
								headerColumn.Item()
									.AlignCenter()
									.Image(GetLogoImageBytes())
									.FitWidth();

								headerColumn.Item().Padding(10);

								// Company Address and Contact
								headerColumn.Item()
									.Row(row =>
									{
										row.RelativeItem()
											.AlignLeft()
											.Column(addressColumn =>
											{
												addressColumn.Item().Text("OMER TOOLS").Bold();
												addressColumn.Item().Text("1126 CANTERBURY RD,");
												addressColumn.Item().Text("ROSELANDS NSW 2196");
												addressColumn.Item().Text("Australia");
											});

										row.RelativeItem()
											.AlignRight()
											.Column(contactColumn =>
											{
												contactColumn.Item().Text("Contact Info:").Bold();
												contactColumn.Item().Text("Phone: (02) 9759-8833");
												contactColumn.Item().Text("Email: Info@omertools.com.au");
												contactColumn.Item().Text("Website: omertools.com.au");
											});
									});

								// Separator line
								headerColumn.Item()
									.PaddingVertical(10)
									.LineHorizontal(1)
									.LineColor(Colors.Grey.Medium);

								// Form Title
								headerColumn.Item()
									.PaddingTop(10)
									.AlignCenter()
									.Text("OMER TOOLS WARRANTY CLAIM FORM")
									.FontSize(20)
									.Bold()
									.FontColor(Colors.Blue.Darken3);
							});

						// Content
						page.Content()
							.PaddingVertical(1, QuestPDF.Infrastructure.Unit.Centimetre)
							.Column(column =>
							{
								// Claim Number
								column.Item()
									.Text($"Claim Number: {claim.ClaimNumber}")
									.FontSize(16)
									.SemiBold();

								column.Item().PaddingTop(10);

								// Customer Info
								column.Item()
									.Text("CUSTOMER INFORMATION")
									.FontSize(14)
									.Bold()
									.FontColor(Colors.Blue.Darken2);

								column.Item()
									.Background(Colors.Grey.Lighten3)
									.Padding(10)
									.Column(infoColumn =>
									{
										infoColumn.Item().Text($"Name: {claim.FullName}");
										infoColumn.Item().Text($"Email: {claim.Email}");
										infoColumn.Item().Text($"Phone: {claim.PhoneNumber}");
										infoColumn.Item().Text($"Address: {claim.Address}");
									});

								column.Item().PaddingTop(10);

								// Product Info Section
								column.Item()
									.Text("PRODUCT INFORMATION")
									.FontSize(14)
									.Bold()
									.FontColor(Colors.Blue.Darken2);

								// Check if we have multiple products
								if (claim.Products != null && claim.Products.Any())
								{
									// Show product count
									column.Item()
										.Background(Colors.Grey.Lighten3)
										.Padding(10)
										.Row(row =>
										{
											row.RelativeItem().Text($"Total Products: {claim.Products.Count()}");
											row.RelativeItem().Text($"Claim Type: {FormatClaimType(claim.ClaimType)}");
										});

									column.Item().PaddingTop(5);

									// Display each product
									foreach (var product in claim.Products)
									{
										column.Item()
											.Border(1)
											.BorderColor(Colors.Grey.Lighten1)
											.Padding(10)
											.Column(productColumn =>
											{
												productColumn.Item().Row(row =>
												{
													row.RelativeItem().Text($"Model: {product.ModelNumber}");
													row.RelativeItem().Text($"Serial: {product.SerialNumber ?? "N/A"}");
												});

												if (!string.IsNullOrEmpty(product.FaultDescription))
												{
													productColumn.Item().PaddingTop(5);
													productColumn.Item()
														.Text($"Fault: {product.FaultDescription}")
														.FontSize(11);
												}
											});

										column.Item().PaddingTop(5);
									}
								}
								else
								{
									// Backward compatibility: single product
									column.Item()
										.Background(Colors.Grey.Lighten3)
										.Padding(10)
										.Column(productColumn =>
										{
											productColumn.Item().Text($"Model: {claim.ModelNumber}");
											productColumn.Item().Text($"Serial: {claim.SerialNumber ?? "N/A"}");
											productColumn.Item().Text($"Type: {FormatClaimType(claim.ClaimType)}");
										});

									column.Item()
								.Text("FAULT DESCRIPTION")
								.FontSize(14)
								.Bold()
								.FontColor(Colors.Blue.Darken2);

									column.Item()
										.Border(1)
										.BorderColor(Colors.Grey.Lighten1)
										.Padding(10)
										.MinHeight(100)
										.Text(claim.FaultDescription);

									column.Item().PaddingTop(10);
								}

								column.Item().PaddingTop(10);

								// Fault Description
								
								if (!string.IsNullOrEmpty(claim.CommonFaultDescription))
								{
									column.Item()
									.Text("COMMON FAULT DESCRIPTION")
									.FontSize(14)
									.Bold()
									.FontColor(Colors.Blue.Darken2);
									// Use common description if available, otherwise use first product's or single description
									var faultDescription = !string.IsNullOrEmpty(claim.CommonFaultDescription)
										? claim.CommonFaultDescription
										: (claim.Products?.FirstOrDefault()?.FaultDescription ?? claim.FaultDescription);

									column.Item()
										.Border(1)
										.BorderColor(Colors.Grey.Lighten1)
										.Padding(10)
										.MinHeight(100)
										.Text(faultDescription);

									column.Item().PaddingTop(10);
								}
								// Claim Status and Dates
								//column.Item()
								//	.Text("CLAIM STATUS")
								//	.FontSize(14)
								//	.Bold()
								//	.FontColor(Colors.Blue.Darken2);

								//column.Item()
								//	.Background(Colors.Grey.Lighten3)
								//	.Padding(10)
								//	.Column(statusColumn =>
								//	{
								//		statusColumn.Item().Text($"Status: {FormatStatus(claim.Status)}");
								//		statusColumn.Item().Text($"Submitted: {claim.SubmittedAt:dd MMMM yyyy HH:mm}");

								//		if (claim.ReviewedAt.HasValue)
								//			statusColumn.Item().Text($"Reviewed: {claim.ReviewedAt.Value:dd MMMM yyyy HH:mm}");

								//		if (claim.CompletedAt.HasValue)
								//			statusColumn.Item().Text($"Completed: {claim.CompletedAt.Value:dd MMMM yyyy HH:mm}");
								//	});
							});

						// Footer with page numbers
						page.Footer()
							.AlignCenter()
							.Text(x =>
							{
								x.Span("Page ");
								x.CurrentPageNumber();
								x.Span(" of ");
								x.TotalPages();
							});
					});
				});

				// Generate PDF bytes
				var pdfBytes = document.GeneratePdf();

				// Return as PDF file
				return File(pdfBytes, "application/pdf", $"warranty-claim-{claim.ClaimNumber}.pdf");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error generating printable claim for ID {id}");
				return StatusCode(500, new
				{
					success = false,
					message = "Failed to generate PDF",
					error = ex.Message
				});
			}
		}

		// Helper method to load logo image
		private byte[] GetLogoImageBytes()
		{
			// Option 1: From file system
			var imagePath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "images", "OTLogo.png");
			return System.IO.File.ReadAllBytes(imagePath);

			// Option 2: From embedded resource
			// var assembly = Assembly.GetExecutingAssembly();
			// using var stream = assembly.GetManifestResourceStream("YourNamespace.Images.logo.png");
			// using var ms = new MemoryStream();
			// stream.CopyTo(ms);
			// return ms.ToArray();

			// Option 3: From URL (async)
			// using var httpClient = new HttpClient();
			// return await httpClient.GetByteArrayAsync("https://yourdomain.com/logo.png");
		}

		private string FormatClaimType(string claimType)
		{
			return claimType switch
			{
				"warranty-inspection" => "Warranty Inspection",
				"service-repair" => "Service Repair",
				"firstup-failure" => "Firstup Failure",
				_ => claimType
			};
		}

		private string FormatStatus(string status)
		{
			return status switch
			{
				"submitted" => "Submitted",
				"under_review" => "Under Review",
				"approved" => "Approved",
				"rejected" => "Rejected",
				"completed" => "Completed",
				_ => status
			};
		}
		[HttpPut("{id}")]
		public async Task<IActionResult> UpdateWarrantyClaim(string id, [FromBody] UpdateWarrantyClaimDto claimDto)
		{
			try
			{
				if (!ModelState.IsValid)
					return BadRequest(ModelState);

				var updatedClaim = await _warrantyService.UpdateWarrantyClaimAsync(id, claimDto);
				return Ok(updatedClaim);
			}
			catch (KeyNotFoundException ex)
			{
				_logger.LogWarning(ex, $"Warranty claim with ID {id} not found");
				return NotFound(new { success = false, message = "Claim not found" });
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error updating warranty claim with ID {id}");
				return StatusCode(500, new { success = false, message = "Failed to update claim" });
			}
		}
		private string GetStatusDisplay(string status)
		{
			return status switch
			{
				"submitted" => "Submitted",
				"under_review" => "Under Review",
				"approved" => "Approved",
				"rejected" => "Rejected",
				"completed" => "Completed",
				_ => status
			};
		}

		[HttpGet("fault/{filename}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetFaultImage(string filename)
		{
			try
			{
				var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				var filePath = Path.Combine(webRootPath, "uploads", "warranty", "fault-images", filename);

				if (!System.IO.File.Exists(filePath))
				{
					_logger.LogWarning($"Image not found: {filename}");
					return NotFound(new { message = "Image not found" });
				}

				var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
				var contentType = GetContentType(filename);

				return File(fileBytes, contentType);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error serving image {filename}");
				return StatusCode(500, new { message = "Error serving image" });
			}
		}

		[HttpGet("proof/{filename}")]
		[AllowAnonymous]
		public async Task<IActionResult> GetProofImage(string filename)
		{
			try
			{
				var webRootPath =  Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				var filePath = Path.Combine(webRootPath, "uploads", "warranty", "proofs", filename);

				if (!System.IO.File.Exists(filePath))
				{
					_logger.LogWarning($"Proof file not found: {filename}");
					return NotFound(new { message = "File not found" });
				}

				var fileBytes = await System.IO.File.ReadAllBytesAsync(filePath);
				var contentType = GetContentType(filename);

				return File(fileBytes, contentType);
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error serving proof file {filename}");
				return StatusCode(500, new { message = "Error serving file" });
			}
		}

		private string GetContentType(string filename)
		{
			var extension = Path.GetExtension(filename).ToLowerInvariant();
			return extension switch
			{
				".jpg" or ".jpeg" => "image/jpeg",
				".png" => "image/png",
				".gif" => "image/gif",
				".pdf" => "application/pdf",
				_ => "application/octet-stream"
			};
		}
	
}
}