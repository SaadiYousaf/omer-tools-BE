using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Polly;
using ProductService.Business.DTOs;
using ProductService.Business.Helper;
using ProductService.Business.Interfaces;
using ProductService.DataAccess.Data;
using ProductService.Domain.Entites;
using ProductService.Domain.Entites.EmailsModel;
using ProductService.Domain.Entities;
using ProductService.Domain.Interfaces;
using Stripe;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using static ProductService.Domain.Entities.WarrantyClaim;

namespace ProductService.Business.Services
{
	public class WarrantyService : IWarrantyService
	{
		private readonly IUnitOfWork _unitOfWork;
		private readonly IMapper _mapper;
		private readonly ILogger<WarrantyService> _logger;
		private readonly ProductDbContext _dbContext;
		private readonly IMailkitEmailService _emailService;

		public WarrantyService(IUnitOfWork unitOfWork, IMapper mapper, ILogger<WarrantyService> logger, ProductDbContext dbContext, IMailkitEmailService emailService)
		{
			_unitOfWork = unitOfWork;
			_mapper = mapper;
			_logger = logger;
			_dbContext = dbContext;
			_emailService = emailService;
		}

		#region Warranty Claim Methods

		public async Task<WarrantyClaimDto> GetWarrantyClaimByIdAsync(string id)
		{
			var claim = await _unitOfWork.WarrantyClaimRepository.GetByIdAsync(id, "FaultImages", "ProductClaims");
			return _mapper.Map<WarrantyClaimDto>(claim);
		}

		public async Task<WarrantyPaginatedResult<WarrantyClaimDto>> GetWarrantyClaimsAsync(WarrantyClaimQueryParameters queryParams)
		{
			var query = _unitOfWork.WarrantyClaimRepository.GetQueryable()
				.Where(w => w.IsActive);

			// Apply filters
			if (!string.IsNullOrEmpty(queryParams.Status))
				query = query.Where(w => w.Status == queryParams.Status);

			if (!string.IsNullOrEmpty(queryParams.ClaimType))
				query = query.Where(w => w.ClaimType == queryParams.ClaimType);

			if (!string.IsNullOrEmpty(queryParams.Search))
			{
				query = query.Where(w =>
					w.FullName.Contains(queryParams.Search) ||
					w.Email.Contains(queryParams.Search) ||
					w.PhoneNumber.Contains(queryParams.Search) ||
					w.ClaimNumber.Contains(queryParams.Search) ||
					w.ModelNumber.Contains(queryParams.Search));
			}

			if (queryParams.FromDate.HasValue)
				query = query.Where(w => w.SubmittedAt >= queryParams.FromDate.Value);

			if (queryParams.ToDate.HasValue)
				query = query.Where(w => w.SubmittedAt <= queryParams.ToDate.Value);

			// Apply sorting
			query = queryParams.SortDescending == true
				? query.OrderByDescending(GetSortProperty(queryParams.SortBy))
				: query.OrderBy(GetSortProperty(queryParams.SortBy));

			// Get total count
			var total = await query.CountAsync();

			// Apply pagination
			var page = queryParams.Page ?? 1;
			var limit = queryParams.Limit ?? 10;
			var skip = (page - 1) * limit;

			var data = await query
				.Skip(skip)
				.Take(limit)
				.Include("FaultImages")
				.Include(w => w.ProductClaims.Where(p => p.IsActive))
				.ToListAsync();

			// Map to DTO
			var claimDtos = _mapper.Map<IEnumerable<WarrantyClaimDto>>(data);

			return new WarrantyPaginatedResult<WarrantyClaimDto>
			{
				Data = claimDtos.ToList(),
				Total = total,
				Page = page,
				PageSize = limit,
				TotalPages = (int)Math.Ceiling(total / (double)limit)
			};
		}

		private System.Linq.Expressions.Expression<Func<WarrantyClaim, object>> GetSortProperty(string sortBy)
		{
			return sortBy?.ToLower() switch
			{
				"fullname" => w => w.FullName,
				"claimnumber" => w => w.ClaimNumber,
				"status" => w => w.Status,
				"submittedat" => w => w.SubmittedAt,
				"modelnumber" => w => w.ModelNumber,
				_ => w => w.SubmittedAt
			};
		}

		public async Task<WarrantyClaimDto> CreateWarrantyClaimAsync(CreateWarrantyClaimDto claimDto)
		{
			// Map DTO to entity

			if ((claimDto.Products == null || claimDto.Products.Count == 0) &&
				  !string.IsNullOrEmpty(claimDto.ModelNumber))
			{
				// Convert single product to array
				claimDto.Products = new List<CreateProductClaimDto>
		{
			new CreateProductClaimDto
			{
				ModelNumber = claimDto.ModelNumber,
				SerialNumber = claimDto.SerialNumber,
				FaultDescription = claimDto.FaultDescription
			}
		};
			}

			// Map main claim
			var claim = _mapper.Map<WarrantyClaim>(claimDto);

			// Generate claim number
			claim.ClaimNumber = await GenerateSequentialClaimNumberAsync();

			// Add products to the claim
			for (int i = 0; i < claimDto.Products.Count; i++)
			{
				var productDto = claimDto.Products[i];

				// IMPORTANT: If ProductClaim is nested, use: new WarrantyClaim.ProductClaim
				// If separate file, use: new ProductClaim
				var productClaim = new ProductClaim // OR new ProductClaim if separate
				{
					Id = Guid.NewGuid().ToString(),
					ModelNumber = productDto.ModelNumber,
					SerialNumber = productDto.SerialNumber,
					FaultDescription = productDto.FaultDescription,
					DisplayOrder = i,
					CreatedAt = DateTime.UtcNow,
					IsActive = true
				};

				claim.ProductClaims.Add(productClaim);

				// For backward compatibility, populate single product fields from FIRST product
				if (i == 0)
				{
					claim.ModelNumber = productDto.ModelNumber;
					claim.SerialNumber = productDto.SerialNumber;

					// Set FaultDescription: use CommonFaultDescription if provided, otherwise use first product's description
					claim.FaultDescription = string.IsNullOrEmpty(claimDto.CommonFaultDescription)
						? productDto.FaultDescription
						: claimDto.CommonFaultDescription;
				}
			}

			// Set common fault description
			claim.CommonFaultDescription = claimDto.CommonFaultDescription;

			// If no products (shouldn't happen with validation), set default values
			if (claimDto.Products.Count == 0)
			{
				claim.FaultDescription = claimDto.FaultDescription;
			}

			await _unitOfWork.WarrantyClaimRepository.AddAsync(claim);
			await _unitOfWork.CompleteAsync();


			_ = Task.Run(async () =>
			{
				try
				{
					await _emailService.SendWarrantyClaimConfirmationAsync(claim.Email, claim);
				}
				catch (Exception emailEx)
				{
					_logger.LogError(emailEx, "Failed to send claim confirmation email for #{ClaimNumber}",
						claim.ClaimNumber);
				}
			});
			return _mapper.Map<WarrantyClaimDto>(claim);
		}
		private async Task SendWarrantyClaimConfirmationEmail(WarrantyClaim claim)
		{
			try
			{
				//var templateContent = await TemplateHelper.LoadTemplate("ClaimsNotification");

				var emailMessage = new EmailMessage
				{
					ToEmail = claim.Email,
					Subject = $"Warranty Claim Submitted: {claim.ClaimNumber}",
					Body = "ClaimsNotifications",
					Placeholders = new Dictionary<string, string>
					{
						["CustomerName"] = claim.FullName,
						["ClaimId"] = claim.ClaimNumber,
						["ClaimDate"] = claim.SubmittedAt.ToString("MMMM dd, yyyy"),
						["ClaimStatus"] = claim.Status ?? "Submitted",
						["ClaimType"] = claim.ClaimType ?? "Warranty Claim",
						["OrderNumber"] = claim.ModelNumber ?? "N/A",
						["ClaimDescription"] = GetClaimDescription(claim),
						["NextSteps"] = GetNextSteps(claim.ClaimType),
						["ProcessingTime"] = "2-3",
						["StoreName"] = "Your Store Name", // You can get this from config
						["Subject"] = $"Warranty Claim Submitted: {claim.ClaimNumber}"
					}
				};

				// Use your existing SendEmailAsync method
				var emailSent = await _emailService.SendEmailAsync(emailMessage);

				if (emailSent)
				{
					_logger.LogInformation("✅ Warranty claim confirmation email sent for #{ClaimNumber} to {Email}",
						claim.ClaimNumber, claim.Email);
				}
				else
				{
					_logger.LogWarning("⚠️ Failed to send claim confirmation email for #{ClaimNumber}",
						claim.ClaimNumber);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error in warranty claim email for #{ClaimNumber}", claim.ClaimNumber);
			}
		}

		private string GetClaimDescription(WarrantyClaim claim)
		{
			if (!string.IsNullOrEmpty(claim.CommonFaultDescription))
				return claim.CommonFaultDescription;

			if (!string.IsNullOrEmpty(claim.FaultDescription))
				return claim.FaultDescription;

			// Get from first product if available
			var firstProduct = claim.ProductClaims.FirstOrDefault();
			if (firstProduct != null && !string.IsNullOrEmpty(firstProduct.FaultDescription))
				return firstProduct.FaultDescription;

			return "Warranty claim submitted";
		}

		private string GetNextSteps(string claimType)
		{
			return claimType?.ToLower() switch
			{
				"repair" => "Our repair team will inspect your product within 3-5 business days. " +
						   "We'll contact you with an assessment and estimated completion time.",
				"replacement" => "Our team will verify your warranty eligibility within 2 business days. " +
							   "If approved, we'll ship your replacement product.",
				"refund" => "Our team will review your refund request within 3 business days. " +
						   "We'll contact you with the next steps.",
				_ => "Our warranty team will review your claim within 2-3 business days. " +
					"You'll receive an update via email once the review is complete."
			};
		}


		public async Task<WarrantyClaimDto> UpdateWarrantyClaimAsync(string id, UpdateWarrantyClaimDto claimDto)
		{
			// Load the claim WITH ProductClaims using DbContext directly
			var existingClaim = await _dbContext.WarrantyClaims
				.Include(wc => wc.ProductClaims)
				.FirstOrDefaultAsync(wc => wc.Id == id);

			if (existingClaim == null)
				throw new KeyNotFoundException($"Warranty claim with ID {id} not found");

			// Update basic information
			existingClaim.FullName = claimDto.FullName;
			existingClaim.Email = claimDto.Email;
			existingClaim.PhoneNumber = claimDto.PhoneNumber;
			existingClaim.Address = claimDto.Address;
			existingClaim.ClaimType = claimDto.ClaimType;
			existingClaim.CommonFaultDescription = claimDto.CommonFaultDescription;
			if(!string.IsNullOrEmpty(claimDto.InvoiceNumber))
			existingClaim.InvoiceNumber = claimDto.InvoiceNumber;
			existingClaim.ProofMethod = claimDto.ProofMethod;
			existingClaim.UpdatedAt = DateTime.UtcNow;

			// Handle products vs legacy fields
			if (claimDto.Products != null && claimDto.Products.Count > 0)
			{
				// Debug log
				_logger.LogInformation($"Updating {claimDto.Products.Count} products for claim {id}");

				var existingProducts = existingClaim.ProductClaims.ToList();
				var productIdsToKeep = new List<string>();

				// Update or create products
				for (int i = 0; i < claimDto.Products.Count; i++)
				{
					var productDto = claimDto.Products[i];

					if (!string.IsNullOrEmpty(productDto.Id))
					{
						// Update existing product
						var existingProduct = existingProducts.FirstOrDefault(p => p.Id == productDto.Id);
						if (existingProduct != null)
						{
							// Update fields
							existingProduct.ModelNumber = productDto.ModelNumber;
							existingProduct.SerialNumber = productDto.SerialNumber;
							existingProduct.FaultDescription = productDto.FaultDescription;
							existingProduct.IsActive = true;
							existingProduct.DisplayOrder = i;
							existingProduct.UpdatedAt = DateTime.UtcNow;

							// KEY: Explicitly mark as modified
							_dbContext.Entry(existingProduct).State = EntityState.Modified;

							productIdsToKeep.Add(existingProduct.Id);
							_logger.LogInformation($"Updated product {existingProduct.Id}: {productDto.ModelNumber}");
						}
						else
						{
							_logger.LogWarning($"Product with ID {productDto.Id} not found, skipping");
						}
					}
					else
					{
						// Add new product
						var newProduct = new ProductClaim
						{
							Id = Guid.NewGuid().ToString(),
							ModelNumber = productDto.ModelNumber,
							SerialNumber = productDto.SerialNumber,
							FaultDescription = productDto.FaultDescription,
							DisplayOrder = i,
							CreatedAt = DateTime.UtcNow,
							IsActive = true,
							WarrantyClaimId = id
						};

						// KEY: Add to DbSet explicitly
						await _dbContext.ProductClaims.AddAsync(newProduct);
						existingClaim.ProductClaims.Add(newProduct);
						productIdsToKeep.Add(newProduct.Id);
						_logger.LogInformation($"Added new product: {productDto.ModelNumber}");
					}
				}

				// Soft delete products not in the update list
				foreach (var existingProduct in existingProducts)
				{
					if (!productIdsToKeep.Contains(existingProduct.Id))
					{
						_dbContext.Entry(existingProduct).State = EntityState.Deleted;
						_logger.LogInformation($"deleted product: {existingProduct.Id}");
						existingClaim.ProductClaims.Remove(existingProduct);
					}
				}

				// Update main claim fields from first active product
				var firstActiveProduct = existingClaim.ProductClaims
					.Where(p => p.IsActive)
					.OrderBy(p => p.DisplayOrder)
					.FirstOrDefault();

				if (firstActiveProduct != null)
				{
					existingClaim.ModelNumber = firstActiveProduct.ModelNumber;
					existingClaim.SerialNumber = firstActiveProduct.SerialNumber;
					existingClaim.FaultDescription = string.IsNullOrEmpty(existingClaim.CommonFaultDescription)
						? firstActiveProduct.FaultDescription
						: existingClaim.CommonFaultDescription;
				}
			}
			else if (!string.IsNullOrEmpty(claimDto.LegacyModelNumber) || !string.IsNullOrEmpty(claimDto.LegacyFaultDescription))
			{
				// Legacy single product update
				_logger.LogInformation($"Updating legacy product for claim {id}");

				// Soft delete all existing products
				foreach (var product in existingClaim.ProductClaims)
				{
					product.IsActive = false;
					product.UpdatedAt = DateTime.UtcNow;
					_dbContext.Entry(product).State = EntityState.Modified;
				}

				// Update single product fields on main claim
				existingClaim.ModelNumber = claimDto.LegacyModelNumber;
				existingClaim.SerialNumber = claimDto.LegacySerialNumber;
				existingClaim.FaultDescription = claimDto.LegacyFaultDescription;

				// Create a product entry for consistency
				var productClaim = new ProductClaim
				{
					Id = Guid.NewGuid().ToString(),
					ModelNumber = claimDto.LegacyModelNumber,
					SerialNumber = claimDto.LegacySerialNumber,
					FaultDescription = claimDto.LegacyFaultDescription,
					DisplayOrder = 0,
					CreatedAt = DateTime.UtcNow,
					IsActive = true,
					WarrantyClaimId = id
				};

				// KEY: Add to DbSet explicitly
				await _dbContext.ProductClaims.AddAsync(productClaim);
				existingClaim.ProductClaims.Add(productClaim);
			}
			else
			{
				// No product data provided - keep existing product data
				_logger.LogInformation($"No product updates for claim {id}, keeping existing data");
			}

			// Save ALL changes through DbContext
			try
			{
				_dbContext.WarrantyClaims.Update(existingClaim);
				await _dbContext.SaveChangesAsync();
				_logger.LogInformation("Successfully saved all changes");
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error saving changes to database");
				throw;
			}

			// Reload the claim with products to ensure we return fresh data
			var updatedClaim = await _dbContext.WarrantyClaims
				.Include(wc => wc.ProductClaims.Where(p => p.IsActive))
				.FirstOrDefaultAsync(wc => wc.Id == id);

			return _mapper.Map<WarrantyClaimDto>(updatedClaim);
		}
		public async Task<string> GenerateSequentialClaimNumberAsync()
		{
			try
			{
				// Get the highest existing claim number
				var existingClaims = await _unitOfWork.WarrantyClaimRepository.GetQueryable()
			.Where(c => c.ClaimNumber.StartsWith("OTW-"))
			.Select(c => c.ClaimNumber)
			.ToListAsync();

				if (!existingClaims.Any())
				{
					return "OTW-500"; // Starting number
				}

				// Parse numbers and find max
				int maxNumber = 499; // Default start - 1
				foreach (var claimNumber in existingClaims)
				{
					var parts = claimNumber.Split('-');
					if (parts.Length >= 2 && int.TryParse(parts[1], out int number))
					{
						maxNumber = Math.Max(maxNumber, number);
					}
				}

				return $"OTW-{maxNumber + 1}";
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "Error generating sequential claim number");
				// Fallback: use timestamp if sequential fails
				var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
				return $"OTW-{timestamp}";
			}
		}

		public async Task UpdateWarrantyClaimStatusAsync(string id, UpdateWarrantyClaimStatusDto statusDto)
		{
			var existingClaim = await _dbContext.WarrantyClaims
				.Include(wc => wc.ProductClaims)
				.FirstOrDefaultAsync(wc => wc.Id == id);
			if (existingClaim == null)
				throw new KeyNotFoundException($"Warranty claim with ID {id} not found");

			var oldStatus = existingClaim.Status;

			// Update status
			existingClaim.Status = statusDto.Status;
			existingClaim.StatusNotes = statusDto.StatusNotes;
			existingClaim.AssignedTo = statusDto.AssignedTo;
			existingClaim.UpdatedAt = DateTime.UtcNow;

			// Update timestamps based on status
			if (statusDto.Status == "picked_up" && !existingClaim.ReviewedAt.HasValue)
				existingClaim.ReviewedAt = DateTime.UtcNow;
			else if ((statusDto.Status == "completed" || statusDto.Status == "rejected") && !existingClaim.CompletedAt.HasValue)
				existingClaim.CompletedAt = DateTime.UtcNow;

			await _unitOfWork.WarrantyClaimRepository.UpdateAsync(existingClaim);
			await _unitOfWork.CompleteAsync();
			if (!existingClaim.Status.Equals("Sent"))
			{

			//Trigger Email 
			_ = Task.Run(async () =>
			{
				try
				{
					await SendStatusUpdateEmail(existingClaim, oldStatus, statusDto.Status);
				}
				catch (Exception emailEx)
				{
					_logger.LogError(emailEx, "Failed to send status update email for claim #{ClaimNumber}",
						existingClaim.ClaimNumber);
				}
			});
			}
		}

		private async Task SendStatusUpdateEmail(WarrantyClaim claim, string oldStatus, string newStatus)
		{
			try
			{
				// Skip if status didn't actually change
				if (oldStatus == newStatus)
				{
					_logger.LogInformation("Status unchanged for claim #{ClaimNumber}, skipping email",
						claim.ClaimNumber);
					return;
				}

				// Load claim with products for email
				//var fullClaim = await _dbContext.WarrantyClaims
				//	.Include(c => c.ProductClaims.Where(p => p.IsActive))
				//	.FirstOrDefaultAsync(c => c.Id == claim.Id);

				//if (fullClaim == null)
				//{
				//	_logger.LogWarning("Claim not found when preparing status email: {ClaimId}", claim.Id);
				//	return;
				//}

				// Use the MailkitEmailService to send the email
				var emailSent = await _emailService.SendClaimStatusUpdateEmailAsync(
					claim.Email,
					claim,
					oldStatus,
					newStatus);

				if (emailSent)
				{
					_logger.LogInformation("✅ Status update email sent for claim #{ClaimNumber} ({OldStatus} → {NewStatus}) to {Email}",
						claim.ClaimNumber, oldStatus, newStatus, claim.Email);
				}
				else
				{
					_logger.LogWarning("⚠️ Failed to send status update email for claim #{ClaimNumber}",
						claim.ClaimNumber);
				}
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, "❌ Error sending status update email for claim #{ClaimNumber}",
					claim.ClaimNumber);
			}
		}

		public async Task DeleteWarrantyClaimAsync(string id)
		{
			var claim = await _unitOfWork.WarrantyClaimRepository.GetByIdAsync(id);
			if (claim == null)
				throw new KeyNotFoundException($"Warranty claim with ID {id} not found");

			// Soft delete
			claim.IsActive = false;
			claim.UpdatedAt = DateTime.UtcNow;

			await _unitOfWork.WarrantyClaimRepository.UpdateAsync(claim);
			await _unitOfWork.CompleteAsync();
		}

		private string GenerateClaimNumber()
		{
			var timestamp ="500";
			var random = new Random().Next(1000, 9999);
			return $"OTW-{timestamp}-{random}";
		}

		#endregion

		#region Dashboard & Reports

		public async Task<WarrantyClaimDashboardDto> GetDashboardStatsAsync()
		{
			var claims = await _unitOfWork.WarrantyClaimRepository.GetAllAsync();
			var activeClaims = claims.Where(c => c.IsActive);

			var dashboard = new WarrantyClaimDashboardDto
			{
				TotalClaims = activeClaims.Count(),
				SubmittedCount = activeClaims.Count(c => c.Status == "submitted"),
				UnderReviewCount = activeClaims.Count(c => c.Status == "picked_up"),
				SentCount = activeClaims.Count(c => c.Status == "sent"),
				RejectedCount = activeClaims.Count(c => c.Status == "rejected"),
				CompletedCount = activeClaims.Count(c => c.Status == "completed")
			};

			// Get monthly stats for last 6 months
			var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6);
			var monthlyClaims = activeClaims
				.Where(c => c.SubmittedAt >= sixMonthsAgo)
				.GroupBy(c => new { c.SubmittedAt.Year, c.SubmittedAt.Month })
				.Select(g => new MonthlyClaimCount
				{
					Year = g.Key.Year,
					Month = g.Key.Month,
					Count = g.Count(),
					MonthName = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy")
				})
				.OrderBy(m => m.Year)
				.ThenBy(m => m.Month)
				.ToList();

			dashboard.MonthlyStats = monthlyClaims;

			return dashboard;
		}

		public async Task<int> GetClaimCountByStatusAsync(string status)
		{
			var claims = await _unitOfWork.WarrantyClaimRepository.GetAllAsync();
			return claims.Count(c => c.IsActive && c.Status == status);
		}

		public async Task<IEnumerable<WarrantyClaimDto>> GetRecentClaimsAsync(int count = 10)
		{
			var claims = await _unitOfWork.WarrantyClaimRepository.GetAllAsync();
			return _mapper.Map<IEnumerable<WarrantyClaimDto>>(
				claims.Where(c => c.IsActive)
					  .OrderByDescending(c => c.SubmittedAt)
					  .Take(count)
			);
		}

		#endregion

		#region Image Methods

		public async Task<WarrantyClaimImageDto> GetWarrantyClaimImageByIdAsync(string id)
		{
			var image = await _unitOfWork.WarrantyClaimImageRepository.GetByIdAsync(id);
			return _mapper.Map<WarrantyClaimImageDto>(image);
		}

		public async Task<IEnumerable<WarrantyClaimImageDto>> GetImagesByClaimAsync(string claimId)
		{
			var images = await _unitOfWork.WarrantyClaimImageRepository.GetAllAsync();
			return _mapper.Map<IEnumerable<WarrantyClaimImageDto>>(
				images.Where(i => i.WarrantyClaimId == claimId)
					  .OrderBy(i => i.DisplayOrder)
			);
		}

		public async Task DeleteWarrantyClaimImageAsync(string id)
		{
			var image = await _unitOfWork.WarrantyClaimImageRepository.GetByIdAsync(id);
			if (image == null)
				throw new KeyNotFoundException($"Warranty claim image with ID {id} not found");

			await _unitOfWork.WarrantyClaimImageRepository.DeleteAsync(image);
			await _unitOfWork.CompleteAsync();
		}

		#endregion

		#region File Upload Methods

		public async Task<string> UploadProofOfPurchaseAsync(string claimId, IFormFile file)
		{
			try
			{
				_logger.LogInformation($"Uploading proof of purchase for claim {claimId}");

				// Validate claim exists
				var claim = await _unitOfWork.WarrantyClaimRepository.GetByIdAsync(claimId);
				if (claim == null)
				{
					throw new KeyNotFoundException($"Warranty claim with ID {claimId} not found");
				}

				// Validate file
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
				var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

				if (string.IsNullOrEmpty(extension) || !allowedExtensions.Contains(extension))
				{
					throw new ArgumentException($"Invalid file type. Allowed: {string.Join(", ", allowedExtensions)}");
				}

				if (file.Length > 5 * 1024 * 1024) // 5MB
				{
					throw new ArgumentException("File size exceeds 5MB limit");
				}

				// Create upload directory
				var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				var uploadsDir = Path.Combine(webRootPath, "uploads", "warranty", "proofs");

				if (!Directory.Exists(uploadsDir))
				{
					Directory.CreateDirectory(uploadsDir);
				}

				// Generate unique filename
				var fileName = $"{Guid.NewGuid()}{extension}";
				var filePath = Path.Combine(uploadsDir, fileName);

				// Save file
				using (var stream = new FileStream(filePath, FileMode.Create))
				{
					await file.CopyToAsync(stream);
				}

				// Update claim with file info
				var relativePath = $"/uploads/warranty/proofs/{fileName}";

				claim.ProofOfPurchasePath = relativePath;
				claim.ProofOfPurchaseFileName = file.FileName;
				claim.UpdatedAt = DateTime.UtcNow;

				await _unitOfWork.WarrantyClaimRepository.UpdateAsync(claim);
				await _unitOfWork.CompleteAsync();

				_logger.LogInformation($"Proof of purchase uploaded successfully for claim {claimId}: {relativePath}");

				return relativePath;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error uploading proof of purchase for claim {claimId}");
				throw;
			}
		}

		public async Task<IEnumerable<WarrantyClaimImageDto>> UploadFaultImagesAsync(string claimId, List<IFormFile> files)
		{
			try
			{
				_logger.LogInformation($"Uploading fault images for claim {claimId}, count: {files?.Count ?? 0}");

				// Validate claim exists
				var claim = await _unitOfWork.WarrantyClaimRepository.GetByIdAsync(claimId);
				if (claim == null)
				{
					throw new KeyNotFoundException($"Warranty claim with ID {claimId} not found");
				}

				var uploadedImages = new List<WarrantyClaimImageDto>();
				var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };

				// Limit to 5 images
				var imagesToUpload = files.Take(5).ToList();

				// Create upload directory
				var webRootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
				var uploadsDir = Path.Combine(webRootPath, "uploads", "warranty", "fault-images");

				if (!Directory.Exists(uploadsDir))
				{
					Directory.CreateDirectory(uploadsDir);
				}

				int displayOrder = 0;
				foreach (var file in imagesToUpload)
				{
					if (file == null || file.Length == 0) continue;

					var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

					// Validate file
					if (!allowedExtensions.Contains(extension))
					{
						_logger.LogWarning($"Invalid file type for image: {file.FileName}");
						continue;
					}

					if (file.Length > 5 * 1024 * 1024) // 5MB
					{
						_logger.LogWarning($"File too large: {file.FileName}");
						continue;
					}

					// Generate unique filename
					var fileName = $"{Guid.NewGuid()}{extension}";
					var filePath = Path.Combine(uploadsDir, fileName);

					// Save file
					using (var stream = new FileStream(filePath, FileMode.Create))
					{
						await file.CopyToAsync(stream);
					}

					// Create image entity
					var image = new WarrantyClaimImage
					{
						Id = Guid.NewGuid().ToString(),
						WarrantyClaimId = claimId,
						ImageUrl = $"/uploads/warranty/fault-images/{fileName}",
						FileName = file.FileName,
						FileType = extension,
						FileSize = file.Length,
						DisplayOrder = displayOrder++,
						CreatedAt = DateTime.UtcNow,
						IsActive = true
					};

					await _unitOfWork.WarrantyClaimImageRepository.AddAsync(image);

					// Map to DTO
					var imageDto = new WarrantyClaimImageDto(
						Id: image.Id,
						WarrantyClaimId: image.WarrantyClaimId,
						ImageUrl: image.ImageUrl,
						FileName: image.FileName,
						FileType: image.FileType,
						FileSize: image.FileSize,
						DisplayOrder: image.DisplayOrder,
						CreatedAt: image.CreatedAt,
						UpdatedAt: null,
						IsActive: image.IsActive
					);

					uploadedImages.Add(imageDto);
					_logger.LogInformation($"Uploaded image for claim {claimId}: {image.ImageUrl}");
				}

				await _unitOfWork.CompleteAsync();

				return uploadedImages;
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Error uploading fault images for claim {claimId}");
				throw;
			}
		}

		public async Task UpdateProofOfPurchaseAsync(string claimId, string filePath, string fileName)
		{
			var claim = await _unitOfWork.WarrantyClaimRepository.GetByIdAsync(claimId);
			if (claim == null)
				throw new KeyNotFoundException($"Warranty claim with ID {claimId} not found");

			claim.ProofOfPurchasePath = filePath;
			claim.ProofOfPurchaseFileName = fileName;
			claim.UpdatedAt = DateTime.UtcNow;

			await _unitOfWork.WarrantyClaimRepository.UpdateAsync(claim);
			await _unitOfWork.CompleteAsync();
		}

		#endregion
	}
}