using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Business.DTOs
{
	public class UpdateWarrantyClaimDto
	{
		// Distribution Partner Information
		public string ClaimType { get; set; } = string.Empty;

		[StringLength(20)]
		public string ProofMethod { get; set; } = string.Empty;

		[StringLength(100)]
		public string? InvoiceNumber { get; set; } = string.Empty;

		// End User Information (admin can fix user mistakes)
		public string FullName { get; set; } = string.Empty;
		public string PhoneNumber { get; set; } = string.Empty;
		public string Email { get; set; } = string.Empty;
		public string Address { get; set; } = string.Empty;

		// Asset Information (admin can correct product details)
		public string LegacyModelNumber { get; set; } = string.Empty;
		public string LegacySerialNumber { get; set; } = string.Empty;
		public string LegacyFaultDescription { get; set; } = string.Empty;
		public string CommonFaultDescription { get; set; } = string.Empty;

		// Products (admin can add/remove/edit products)
		public List<UpdateProductClaimDto> Products { get; set; } = new List<UpdateProductClaimDto>();
	}

	public class UpdateProductClaimDto
	{
		public string Id { get; set; } = string.Empty; // For existing products, empty for new
		public string ModelNumber { get; set; } = string.Empty;
		public string SerialNumber { get; set; } = string.Empty;
		public string FaultDescription { get; set; } = string.Empty;
		public bool IsActive { get; set; } = true;
	}
}
