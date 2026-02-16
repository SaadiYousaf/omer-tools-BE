// File: Business/DTOs/WarrantyClaimDto.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductService.Business.DTOs
{
	// Main Warranty Claim DTO
	public class WarrantyClaimDto
	{
		public string Id { get; set; } = string.Empty;
		public string ClaimNumber { get; set; } = string.Empty;

		[StringLength(20)]
		public string ProofMethod { get; set; } = string.Empty;

		[StringLength(100)]
		public string InvoiceNumber { get; set; } = string.Empty;

		// Distribution Partner Information
		public string ClaimType { get; set; } = string.Empty;
		public string ProofOfPurchasePath { get; set; } = string.Empty;
		public string ProofOfPurchaseFileName { get; set; } = string.Empty;

		// End User Information
		public string FullName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;

		// Asset Information
		public string ModelNumber { get; set; } = string.Empty;
		public string SerialNumber { get; set; } = string.Empty;
		public string FaultDescription { get; set; } = string.Empty;

		public string CommonFaultDescription { get; set; } = string.Empty;

		// Claim Status & Tracking
		public string Status { get; set; } = "submitted";
		public string StatusNotes { get; set; } = string.Empty;
		public string AssignedTo { get; set; } = string.Empty;




		// Timestamps
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public DateTime SubmittedAt { get; set; }
		public DateTime? ReviewedAt { get; set; }
		public DateTime? CompletedAt { get; set; }

		public bool IsActive { get; set; } = true;

		// Images
		public IEnumerable<WarrantyClaimImageDto> FaultImages { get; set; } = new List<WarrantyClaimImageDto>();

		public IEnumerable<ProductClaimDto> Products { get; set; } = new List<ProductClaimDto>();
	}

	// Warranty Claim Image DTO (matching BlogImageDto pattern)
	public record WarrantyClaimImageDto(
		string Id,
		string WarrantyClaimId,
		string ImageUrl,
		string FileName,
		string FileType,
		long FileSize,
		int DisplayOrder,
		DateTime CreatedAt,
		DateTime? UpdatedAt,
		bool IsActive
	)
	{
		public WarrantyClaimImageDto() : this("0", "0", "", "", "", 0, 0, DateTime.UtcNow, null, true) { }
	}

	// Create Warranty Claim DTO (for form submission)
	public class CreateWarrantyClaimDto
	{
		// Distribution Partner Information
		public string ClaimType { get; set; } = string.Empty;
		[StringLength(100)]
		public string? InvoiceNumber { get; set; } = string.Empty;

		[StringLength(20)]
		public string ProofMethod { get; set; } = string.Empty;

		// End User Information
		public string FullName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;

		// Asset Information
		public string ModelNumber { get; set; } = string.Empty;
		public string SerialNumber { get; set; } = string.Empty;
		public string FaultDescription { get; set; } = string.Empty;

		// New multi-product support
		public List<CreateProductClaimDto> Products { get; set; } = new List<CreateProductClaimDto>();
		public string CommonFaultDescription { get; set; } = string.Empty;
	}

	// Update Status DTO (for admin)
	public class UpdateWarrantyClaimStatusDto
	{
		public string Status { get; set; } = string.Empty;
		public string StatusNotes { get; set; } = string.Empty;
		public string AssignedTo { get; set; } = string.Empty;
	}

	// Query Parameters (matching BlogQueryParameters pattern)
	public class WarrantyClaimQueryParameters
	{
		public string? Status { get; set; }
		public string? ClaimType { get; set; }
		public string? Search { get; set; }
		public DateTime? FromDate { get; set; }
		public DateTime? ToDate { get; set; }
		public int? Page { get; set; }
		public int? Limit { get; set; }
		public string? SortBy { get; set; } = "SubmittedAt";
		public bool? SortDescending { get; set; } = true;
	}

	// Paginated Result (matching your pattern)
	public class WarrantyPaginatedResult<T>
	{
		public IEnumerable<T> Data { get; set; } = new List<T>();
		public int Total { get; set; }
		public int Page { get; set; }
		public int PageSize { get; set; }
		public int TotalPages { get; set; }
	}

	// Dashboard Statistics DTO
	public class WarrantyClaimDashboardDto
	{
		public int TotalClaims { get; set; }
		public int SubmittedCount { get; set; }
		public int UnderReviewCount { get; set; }
		public int SentCount { get; set; }
		public int RejectedCount { get; set; }
		public int CompletedCount { get; set; }
		public List<MonthlyClaimCount> MonthlyStats { get; set; } = new List<MonthlyClaimCount>();
	}

	public class MonthlyClaimCount
	{
		public int Year { get; set; }
		public int Month { get; set; }
		public int Count { get; set; }
		public string MonthName { get; set; } = string.Empty;
	}
	public class CreateProductClaimDto
	{
		public string ModelNumber { get; set; } = string.Empty;
		public string SerialNumber { get; set; } = string.Empty;
		public string FaultDescription { get; set; } = string.Empty;
	}

	public class ProductClaimDto
	{
		public string Id { get; set; } = string.Empty;
		public string WarrantyClaimId { get; set; } = string.Empty;
		public string ModelNumber { get; set; } = string.Empty;
		public string SerialNumber { get; set; } = string.Empty;
		public string FaultDescription { get; set; } = string.Empty;
		public int DisplayOrder { get; set; }
		public DateTime CreatedAt { get; set; }
		public DateTime? UpdatedAt { get; set; }
		public bool IsActive { get; set; } = true;
	}
}