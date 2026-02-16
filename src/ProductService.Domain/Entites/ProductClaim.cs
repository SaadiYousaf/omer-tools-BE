using ProductService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProductService.Domain.Entites
{
	public class ProductClaim : BaseEntity
	{
		public string WarrantyClaimId { get; set; } = string.Empty;
		public string ModelNumber { get; set; } = string.Empty;
		public string SerialNumber { get; set; } = string.Empty;
		public string FaultDescription { get; set; } = string.Empty;
		public int DisplayOrder { get; set; } = 0;

		// Navigation property
		public virtual WarrantyClaim WarrantyClaim { get; set; } = null!;
	}
}
