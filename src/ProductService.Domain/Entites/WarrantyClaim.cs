// File: Domain/Entities/WarrantyClaim.cs
using ProductService.Domain.Entites;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ProductService.Domain.Entities
{
	public class WarrantyClaim : BaseEntity
	{
		// Claim Identification
		public string ClaimNumber { get; set; } = string.Empty;

		// Distribution Partner Information
		public string ClaimType { get; set; } = string.Empty; // 'warranty-inspection', 'service-repair', 'firstup-failure'

		[StringLength(100)]
		public string InvoiceNumber { get; set; }

		[StringLength(20)]
		public string ProofMethod { get; set; }
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
		public string Status { get; set; } = "submitted"; // submitted, under_review, approved, rejected, completed
		public string StatusNotes { get; set; } = string.Empty;
		public string AssignedTo { get; set; } = string.Empty; // Admin/technician assigned

		// Timestamps
		public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
		public DateTime? ReviewedAt { get; set; }
		public DateTime? CompletedAt { get; set; }

		// Navigation Properties
		public virtual ICollection<WarrantyClaimImage> FaultImages { get; set; } = new List<WarrantyClaimImage>();

		public virtual ICollection<ProductClaim> ProductClaims { get; set; } = new List<ProductClaim>();

		private static string GenerateClaimNumber()
		{
			var timestamp ="500";
			var random = new Random().Next(1000, 9999);
			return $"OTW-{timestamp}-{random}";
		}

		public class WarrantyClaimImage : BaseEntity
		{
			public string WarrantyClaimId { get; set; } = string.Empty;
			public string ImagePath { get; set; } = string.Empty;
			public string ImageUrl { get; set; } = string.Empty;
			public string FileName { get; set; } = string.Empty;
			public string FileType { get; set; } = string.Empty;
			public long FileSize { get; set; }
			public int DisplayOrder { get; set; }

			// Navigation Property
			public virtual WarrantyClaim WarrantyClaim { get; set; } = null!;
		}

	}
}